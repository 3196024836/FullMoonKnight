using System.Reflection;

using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

using FullMoonKnight.Powers;

using FullMoonKnight.Visuals;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 反制触发时的信息，以及结算附带效果用的辅助方法。
/// </summary>
/// <param name="monster">执行行动的敌人。</param>
/// <param name="move">被无效的行动。</param>
/// <param name="targets">行动原本的目标。</param>
/// <param name="owner">反制的布置者。</param>
public sealed class CounterContext(Creature monster, MoveState move, IReadOnlyList<Creature> targets, Creature owner)
{
    /// <summary>执行行动的敌人。</summary>
    public Creature Monster { get; } = monster;

    /// <summary>被无效的行动。</summary>
    public MoveState Move { get; } = move;

    /// <summary>行动原本的目标。</summary>
    public IReadOnlyList<Creature> Targets { get; } = targets;

    /// <summary>反制的布置者。</summary>
    public Creature Owner { get; } = owner;

    /// <summary>敌方回合使用的选择上下文：需要选择时阻塞敌方回合。</summary>
    public PlayerChoiceContext ChoiceContext { get; } = new BlockingPlayerChoiceContext();

    /// <summary>行动是否包含攻击。</summary>
    public bool IsAttack => Move.Intents.Any(i => i is AttackIntent);

    /// <summary>
    /// 夺取：在夺取范围内执行敌人原本的行动，效果转给布置者。召唤和逃跑无法夺取，只无效。
    /// </summary>
    /// <param name="reflect">是否把行动造成的伤害反弹给敌人自己。</param>
    /// <returns>异步任务。</returns>
    public async Task Steal(bool reflect)
    {
        if (Move.Intents.Any(i => i.IntentType is IntentType.Summon or IntentType.Escape))
        {
            return;
        }
        await StealScope.Run(Monster, Owner, true, reflect, () => Move._onPerform(Targets.ToArray()));
    }

    /// <summary>
    /// 只保护布置者：联机时在范围内执行原行动，队友照常承受，布置者和敌人自己的效果取消。
    /// 单人时没有队友，行动整个不执行（不播攻击动作）。召唤和逃跑整个无效。
    /// </summary>
    /// <returns>异步任务。</returns>
    public async Task ShieldOwnerOnly()
    {
        if (Move.Intents.Any(i => i.IntentType is IntentType.Summon or IntentType.Escape)
            || !Targets.Any(t => t.IsAlive && t != Owner))
        {
            return;
        }
        await StealScope.Run(Monster, Owner, false, false, () => Move._onPerform(Targets.ToArray()));
    }

    /// <summary>
    /// 布置者抽牌：反制在敌方回合结算，这时抽牌堆不洗牌、保留结算也已经做过，
    /// 所以改成下回合开始多抽这么多张（原版的"下回合抽牌"能力）。
    /// </summary>
    /// <param name="count">抽牌数。</param>
    /// <returns>异步任务。</returns>
    public async Task Draw(decimal count)
    {
        if (count > 0m && Owner.Player != null)
        {
            await PowerCmd.Apply<DrawCardsNextTurnPower>(ChoiceContext, Owner, count, Owner, null);
        }
    }
}

/// <summary>
/// 反制引擎：在敌人执行行动前检查反制，触发时用反制结算替换原行动。
/// </summary>
[HarmonyPatch(typeof(MoveState), nameof(MoveState.PerformMove))]
public static class CounterEngine
{
    /// <summary>两分支的战斗所有者字段：旧版直接持有战斗，新版持有 CombatTurnState。</summary>
    private static readonly FieldInfo StateField = AccessTools.Field(typeof(CombatManager), "_state")
        ?? AccessTools.Field(typeof(CombatManager), "_turnState")
        ?? throw new MissingFieldException("Unsupported CombatManager state owner");

    /// <summary>读取当前战斗，不引用测试版删除的字段符号。</summary>
    private static CombatState? CurrentCombat
    {
        get
        {
            object? state = StateField.GetValue(CombatManager.Instance);
            if (state == null)
            {
                return null;
            }
            return state as CombatState
                ?? (CombatState?)AccessTools.Property(state.GetType(), "State").GetValue(state);
        }
    }

    /// <summary>
    /// 拦截敌人的行动。
    /// </summary>
    /// <param name="__instance">将要执行的行动。</param>
    /// <param name="targets">行动目标。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <param name="__state">执行行动的敌人，传给后置补丁。</param>
    /// <returns>继续执行原行动时为 true。</returns>
    [HarmonyPrefix]
    private static bool Prefix(MoveState __instance, IEnumerable<Creature> targets, ref Task __result,
        out Creature? __state)
    {
        __state = null;
        if (StealScope.IsActive || CurrentCombat is not { } combatState)
        {
            return true;
        }

        // 正在执行这个行动的敌人
        Creature? monster = combatState.Enemies.FirstOrDefault(c =>
            c.Monster is { IsPerformingMove: true } m && m.NextMove == __instance);
        if (monster == null)
        {
            return true;
        }
        __state = monster;

        // 死亡状态下的行动是复活（实验体的 RESPAWN_MOVE、亡灵类的复苏）：抢走治疗或整个无效会让它停在 0 血，
        // 既没有血条也不能被攻击，战斗因为复活机制不能结束而卡住。这类行动必须原样执行。
        if (!monster.IsAlive)
        {
            Log.Info($"[FullMoonKnight] counter skipped, {monster.Monster!.Id.Entry} is dead and performing {__instance.Id}");
            return true;
        }

        // 最早布置、条件满足的反制（原版敌人的行动都指向全体玩家，不存在只打队友的攻击）
        KnightCounterPower? counter = combatState.PlayerCreatures
            .Where(c => c.IsAlive)
            .SelectMany(c => c.Powers.OfType<KnightCounterPower>())
            .Where(p => p.Matches(__instance))
            .MinBy(p => p.Serial);
        if (counter == null)
        {
            return true;
        }

        __instance._performedAtLeastOnce = true;
        // 敌人的人工制品抵挡反制：消耗一层人工制品和这个反制，行动照常执行（从容不迫的祝福不受人工制品影响）
        if (counter is not ComposurePower && monster.Powers.OfType<ArtifactPower>().FirstOrDefault() is { } artifact)
        {
            __result = BlockedByArtifact(counter, artifact, __instance, targets.ToArray());
            return false;
        }

        __result = Trigger(counter, new CounterContext(monster, __instance, targets.ToList(), counter.Owner));
        return false;
    }

    /// <summary>
    /// 敌人的行动（执行或被无效）结束后，通知玩家的敌人行动监听者。
    /// </summary>
    /// <param name="__result">行动任务。</param>
    /// <param name="__state">执行行动的敌人。</param>
    [HarmonyPostfix]
    private static void Postfix(ref Task __result, Creature? __state)
    {
        if (__state != null)
        {
            __result = NotifyAfterMove(__result, __state);
        }
    }

    /// <summary>
    /// 等行动结束后通知监听者。
    /// </summary>
    /// <param name="move">行动任务。</param>
    /// <param name="monster">执行行动的敌人。</param>
    /// <returns>异步任务。</returns>
    private static async Task NotifyAfterMove(Task move, Creature monster)
    {
        await move;
        if (CurrentCombat is not { } combatState)
        {
            return;
        }
        // 监听者（玩家的遗物）
        List<IEnemyMoveListener> listeners = combatState.PlayerCreatures
            .Where(c => c.IsAlive && c.Player != null)
            .SelectMany(c => c.Player!.Relics.OfType<IEnemyMoveListener>())
            .ToList();
        foreach (IEnemyMoveListener listener in listeners)
        {
            await listener.AfterEnemyMove(new BlockingPlayerChoiceContext(), monster);
        }
    }

    /// <summary>
    /// 反制被敌人的人工制品抵挡：反制和一层人工制品都消耗掉，然后行动照常执行。
    /// </summary>
    /// <param name="counter">被抵挡的反制。</param>
    /// <param name="artifact">敌人的人工制品。</param>
    /// <param name="move">敌人的行动。</param>
    /// <param name="targets">行动目标。</param>
    /// <returns>异步任务。</returns>
    private static async Task BlockedByArtifact(KnightCounterPower counter, ArtifactPower artifact, MoveState move,
        Creature[] targets)
    {
        Log.Info($"[FullMoonKnight] {counter.Id.Entry} blocked by artifact");
        await PowerCmd.Remove(counter);
        await PowerCmd.Decrement(artifact);
        await move._onPerform(targets);
    }

    /// <summary>
    /// 触发反制：移除反制，原行动不执行，结算附带效果。
    /// </summary>
    /// <param name="counter">触发的反制。</param>
    /// <param name="context">触发信息。</param>
    /// <returns>异步任务。</returns>
    private static async Task Trigger(KnightCounterPower counter, CounterContext context)
    {
        Log.Info($"[FullMoonKnight] {counter.Id.Entry} negates {context.Monster.Monster!.Id.Entry} {context.Move.Id}");
        SfxCmd.Play(KnightAudio.CounterTrigger);
        await PowerCmd.Remove(counter);
        if (!counter.Steals)
        {
            await context.ShieldOwnerOnly();
        }
        await counter.Resolve(context);
    }
}

/// <summary>
/// 敌人行动监听者：敌人每执行一次行动（包括被反制无效的行动）后触发。由玩家的遗物实现。
/// </summary>
public interface IEnemyMoveListener
{
    /// <summary>
    /// 敌人的一次行动结束后。
    /// </summary>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="monster">执行行动的敌人。</param>
    /// <returns>异步任务。</returns>
    Task AfterEnemyMove(PlayerChoiceContext choiceContext, Creature monster);
}
