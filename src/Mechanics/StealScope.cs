using System.Reflection;

using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 夺取范围：反制让敌人的行动"无效并被夺取"时，在这个范围内执行敌人原本的行动。
/// 范围内敌人给自己的格挡、治疗和白名单增益改给反制者（不夺取时取消）；给反制者的伤害、减益、状态牌和给其他敌人的效果取消。
/// 反制只保护布置者自己：联机时落在其他玩家身上的伤害、减益和状态牌照常结算。开启反弹时，打向反制者的伤害改为打在敌人自己身上。
/// 敌人行动按顺序结算，范围用静态字段记录；执行转交效果时临时关闭范围，避免连锁反应被误拦。
/// </summary>
public static class StealScope
{
    // ===== 状态 =====

    /// <summary>被夺取行动的敌人；为空时范围未开启。</summary>
    private static Creature? victim;

    /// <summary>获得效果的反制者。</summary>
    private static Creature? thief;

    /// <summary>敌人给自己的效果是否转给反制者（否则取消）。</summary>
    private static bool steal;

    /// <summary>是否把敌人造成的伤害反弹给它自己。</summary>
    private static bool reflect;

    /// <summary>可以转给玩家的增益：这些能力在玩家身上也能正常工作。</summary>
    private static readonly HashSet<Type> StealableBuffs =
    [
        typeof(StrengthPower), typeof(DexterityPower), typeof(ArtifactPower), typeof(PlatingPower),
        typeof(ThornsPower), typeof(RitualPower), typeof(VigorPower), typeof(IntangiblePower),
        typeof(RegenPower), typeof(BufferPower),
    ];

    /// <summary>范围是否开启。</summary>
    public static bool IsActive => victim != null;

    /// <summary>
    /// 是否通用增益（夺取白名单）：这些增益在玩家和敌人身上都能正常工作，不属于首领机制。破坏也用这个名单。
    /// </summary>
    /// <param name="power">能力。</param>
    /// <returns>属于白名单时为 true。</returns>
    public static bool IsCommonBuff(PowerModel power)
    {
        return StealableBuffs.Contains(power.GetType());
    }

    // ===== 执行 =====

    /// <summary>
    /// 在夺取范围内执行敌人的行动。行动里抛出的异常只记录，不中断战斗。
    /// </summary>
    /// <param name="victimCreature">被夺取的敌人。</param>
    /// <param name="thiefCreature">获得效果的反制者。</param>
    /// <param name="stealEffects">敌人给自己的效果是否转给反制者。</param>
    /// <param name="reflectDamage">是否反弹伤害。</param>
    /// <param name="action">敌人原本的行动。</param>
    /// <returns>异步任务。</returns>
    public static async Task Run(Creature victimCreature, Creature thiefCreature, bool stealEffects, bool reflectDamage,
        Func<Task> action)
    {
        victim = victimCreature;
        thief = thiefCreature;
        steal = stealEffects;
        reflect = reflectDamage;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log.Warn($"[FullMoonKnight] stolen move of {victimCreature} failed: {ex}");
        }
        finally
        {
            victim = null;
            thief = null;
            steal = false;
            reflect = false;
        }
    }

    /// <summary>
    /// 临时关闭范围执行一个效果（转交给反制者的效果），结束后恢复。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="action">要执行的效果。</param>
    /// <returns>效果的返回值。</returns>
    private static async Task<T> Suspended<T>(Func<Task<T>> action)
    {
        // 保存的范围
        (Creature? savedVictim, Creature? savedThief, bool savedSteal, bool savedReflect) =
            (victim, thief, steal, reflect);
        victim = null;
        thief = null;
        steal = false;
        reflect = false;
        try
        {
            return await action();
        }
        finally
        {
            (victim, thief, steal, reflect) = (savedVictim, savedThief, savedSteal, savedReflect);
        }
    }

    /// <summary>
    /// 临时关闭范围执行一个无返回值的效果。
    /// </summary>
    /// <param name="action">要执行的效果。</param>
    /// <returns>异步任务。</returns>
    private static Task Suspended(Func<Task> action)
    {
        return Suspended(async () =>
        {
            await action();
            return true;
        });
    }

    /// <summary>
    /// 能力能否从敌人转给反制者。
    /// </summary>
    /// <param name="power">能力。</param>
    /// <param name="amount">变化量。</param>
    /// <returns>白名单增益且为正数时为 true。</returns>
    private static bool CanSteal(PowerModel power, decimal amount)
    {
        return steal && amount > 0m && StealableBuffs.Contains(power.GetType());
    }

    /// <summary>
    /// 是否反制者以外的玩家一方生物：反制不保护他们，效果照常结算。
    /// </summary>
    /// <param name="creature">生物。</param>
    /// <returns>是队友时为 true。</returns>
    private static bool IsTeammate(Creature creature)
    {
        return creature.Side == CombatSide.Player && creature != thief;
    }

    // ===== 拦截 =====

    /// <summary>
    /// 格挡：敌人自己的格挡转给反制者，其他敌人的格挡取消。
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock),
        [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)])]
    private static class GainBlockPatch
    {
        /// <summary>
        /// 拦截获得格挡。
        /// </summary>
        /// <param name="creature">获得格挡的生物。</param>
        /// <param name="amount">格挡量。</param>
        /// <param name="props">格挡属性。</param>
        /// <param name="fast">是否快速结算。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(Creature creature, decimal amount, ValueProp props, bool fast,
            ref Task<decimal> __result)
        {
            if (!IsActive || creature.Side != CombatSide.Enemy)
            {
                return true;
            }

            // 反制者
            Creature receiver = thief!;
            __result = creature == victim && steal
                ? Suspended(() => CreatureCmd.GainBlock(receiver, amount, props, null, fast))
                : Task.FromResult(0m);
            return false;
        }
    }

    /// <summary>
    /// 治疗：敌人自己的治疗转给反制者，其他敌人的治疗取消。
    /// </summary>
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    private static class HealPatch
    {
        /// <summary>
        /// 拦截治疗。
        /// </summary>
        /// <param name="creature">被治疗的生物。</param>
        /// <param name="amount">治疗量。</param>
        /// <param name="playAnim">是否播放动画。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(Creature creature, decimal amount, bool playAnim, ref Task __result)
        {
            if (!IsActive || creature.Side != CombatSide.Enemy)
            {
                return true;
            }

            // 0 血的敌人被治疗只会是复活：夺取会把它留在 0 血的无血条状态，这类治疗不能被抢
            if (creature == victim && creature.CurrentHp <= 0)
            {
                Log.Warn($"[FullMoonKnight] revive heal of {creature} is not stealable");
                return true;
            }

            // 反制者
            Creature receiver = thief!;
            __result = creature == victim && steal
                ? Suspended(() => CreatureCmd.Heal(receiver, amount, playAnim))
                : Task.CompletedTask;
            return false;
        }
    }

    /// <summary>
    /// 伤害：敌人打向反制者的伤害取消，开启反弹时改为打在它自己身上；打向队友的伤害照常结算。
    /// </summary>
    [HarmonyPatch]
    private static class DamagePatch
    {
        /// <summary>正式版六参数入口。</summary>
        [HarmonyPrepare]
        private static bool Prepare() => KnightDamage.DamageTarget.GetParameters().Length == 6;

        /// <summary>测试版显式注入 CardPlay，避免 __args 回写覆盖其他补丁改过的伤害属性。</summary>
        [HarmonyPatch]
        private static class WithCardPlay
        {
            [HarmonyPrepare]
            private static bool Prepare() => KnightDamage.DamageTarget.GetParameters().Length == 7;

            [HarmonyTargetMethod]
            private static MethodBase TargetMethod() => KnightDamage.DamageTarget;

            [HarmonyPrefix]
            private static bool Prefix(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount,
                ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay,
                ref Task<IEnumerable<DamageResult>> __result)
            {
                return Intercept(choiceContext, targets, amount, props, dealer, cardSource, cardPlay, ref __result);
            }
        }
        /// <summary>补丁和伤害调用使用同一分支入口。</summary>
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => KnightDamage.DamageTarget;

        /// <summary>
        /// 拦截伤害。
        /// </summary>
        /// <param name="choiceContext">玩家选择上下文。</param>
        /// <param name="targets">原目标。</param>
        /// <param name="amount">每个目标的伤害。</param>
        /// <param name="props">伤害属性。</param>
        /// <param name="dealer">伤害来源。</param>
        /// <param name="cardSource">来源牌。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount,
            ValueProp props, Creature? dealer, CardModel? cardSource,
            ref Task<IEnumerable<DamageResult>> __result)
        {
            return Intercept(choiceContext, targets, amount, props, dealer, cardSource, null, ref __result);
        }

        /// <summary>两分支共享的反制归属判断。</summary>
        private static bool Intercept(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount,
            ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay,
            ref Task<IEnumerable<DamageResult>> __result)
        {
            if (!IsActive || dealer == null || dealer.Side != CombatSide.Enemy)
            {
                return true;
            }

            // 被夺取的敌人、照常受伤的队友、是否反弹打向反制者的那一份
            Creature self = victim!;
            List<Creature> teammates = targets.Where(IsTeammate).ToList();
            bool reflectHit = reflect && dealer == self && self.IsAlive && targets.Contains(thief!);
            __result = teammates.Count == 0 && !reflectHit
                ? Task.FromResult<IEnumerable<DamageResult>>([])
                : Suspended(() => DamageOthers(choiceContext, teammates, amount, props, dealer, cardSource, self,
                    reflectHit, cardPlay));
            return false;
        }

        /// <summary>
        /// 队友照常受伤；需要时把打向反制者的那一份反弹给敌人自己。
        /// </summary>
        /// <param name="choiceContext">玩家选择上下文。</param>
        /// <param name="teammates">照常受伤的队友。</param>
        /// <param name="amount">每个目标的伤害。</param>
        /// <param name="props">伤害属性。</param>
        /// <param name="dealer">伤害来源。</param>
        /// <param name="cardSource">来源牌。</param>
        /// <param name="self">被夺取的敌人。</param>
        /// <param name="reflectHit">是否反弹。</param>
        /// <returns>全部伤害结果。</returns>
        private static async Task<IEnumerable<DamageResult>> DamageOthers(PlayerChoiceContext choiceContext,
            List<Creature> teammates, decimal amount, ValueProp props, Creature dealer, CardModel? cardSource,
            Creature self, bool reflectHit, CardPlay? cardPlay)
        {
            // 伤害结果
            List<DamageResult> results = [];
            if (teammates.Count > 0)
            {
                results.AddRange(await KnightDamage.Deal(choiceContext, teammates, amount, props, dealer, cardSource, cardPlay));
            }
            if (reflectHit)
            {
                results.AddRange(await ReflectDamage(choiceContext, self, amount, props, 1));
            }
            return results;
        }

        /// <summary>
        /// 把伤害反弹给敌人自己。
        /// </summary>
        /// <param name="choiceContext">玩家选择上下文。</param>
        /// <param name="self">敌人。</param>
        /// <param name="amount">每次伤害。</param>
        /// <param name="props">伤害属性。</param>
        /// <param name="count">反弹次数。</param>
        /// <returns>全部伤害结果。</returns>
        private static async Task<IEnumerable<DamageResult>> ReflectDamage(PlayerChoiceContext choiceContext,
            Creature self, decimal amount, ValueProp props, int count)
        {
            // 伤害结果
            List<DamageResult> results = [];
            for (int i = 0; i < count && self.IsAlive; i++)
            {
                results.AddRange(await KnightDamage.Deal(choiceContext, [self], amount, props, self, null));
            }
            return results;
        }
    }

    /// <summary>
    /// 施加新能力：敌人给自己的白名单增益转给反制者（夺取时）；给队友的照常施加；其余全部取消。
    /// </summary>
    [HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply),
        [typeof(PlayerChoiceContext), typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature),
            typeof(CardModel), typeof(bool)])]
    private static class ApplyPatch
    {
        /// <summary>
        /// 拦截施加能力。
        /// </summary>
        /// <param name="choiceContext">玩家选择上下文。</param>
        /// <param name="power">要施加的能力（新实例）。</param>
        /// <param name="target">目标。</param>
        /// <param name="amount">层数。</param>
        /// <param name="applier">施加者。</param>
        /// <param name="cardSource">来源牌。</param>
        /// <param name="silent">是否静默。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(PlayerChoiceContext choiceContext, PowerModel power, Creature target,
            decimal amount, Creature? applier, CardModel? cardSource, bool silent, ref Task __result)
        {
            if (!IsActive)
            {
                return true;
            }
            if (IsTeammate(target))
            {
                __result = Suspended(() =>
                    PowerCmd.Apply(choiceContext, power, target, amount, applier, cardSource, silent));
                return false;
            }

            // 反制者
            Creature receiver = thief!;
            __result = target == victim && CanSteal(power, amount)
                ? Suspended(() => PowerCmd.Apply(choiceContext, power, receiver, amount, receiver, null, silent))
                : Task.CompletedTask;
            return false;
        }
    }

    /// <summary>
    /// 叠加已有能力：敌人已有的白名单增益再叠加时，改为给反制者施加同样层数（夺取时）；队友的照常叠加；其余全部取消。
    /// </summary>
    [HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]
    private static class ModifyAmountPatch
    {
        /// <summary>
        /// 拦截修改能力层数。
        /// </summary>
        /// <param name="choiceContext">玩家选择上下文。</param>
        /// <param name="power">要修改的能力。</param>
        /// <param name="offset">变化量。</param>
        /// <param name="applier">施加者。</param>
        /// <param name="cardSource">来源牌。</param>
        /// <param name="silent">是否静默。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(PlayerChoiceContext choiceContext, PowerModel power, decimal offset,
            Creature? applier, CardModel? cardSource, bool silent, ref Task<int> __result)
        {
            if (!IsActive)
            {
                return true;
            }
            if (power.Owner != null && IsTeammate(power.Owner))
            {
                __result = Suspended(() =>
                    PowerCmd.ModifyAmount(choiceContext, power, offset, applier, cardSource, silent));
                return false;
            }

            // 反制者
            Creature receiver = thief!;
            if (power.Owner == victim && CanSteal(power, offset))
            {
                __result = Suspended(async () =>
                {
                    // 给反制者的新能力实例
                    PowerModel copy = power.CanonicalInstance.ToMutable();
                    await PowerCmd.Apply(choiceContext, copy, receiver, offset, receiver, null, silent);
                    return (int)offset;
                });
            }
            else
            {
                __result = Task.FromResult(power.Amount);
            }
            return false;
        }
    }

    /// <summary>
    /// 生成牌：敌人塞进反制者牌堆的状态牌取消；塞给队友的照常加入。
    /// </summary>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
    private static class GeneratedCardsPatch
    {
        /// <summary>
        /// 拦截生成牌。
        /// </summary>
        /// <param name="cards">要加入的牌。</param>
        /// <param name="newPileType">目标牌堆。</param>
        /// <param name="creator">生成牌的玩家；敌人生成时为空。</param>
        /// <param name="position">加入位置。</param>
        /// <param name="__result">替换后的任务。</param>
        /// <returns>继续执行原方法时为 true。</returns>
        [HarmonyPrefix]
        private static bool Prefix(IEnumerable<CardModel> cards, PileType newPileType, Player? creator,
            CardPilePosition position, ref Task<IReadOnlyList<CardPileAddResult>> __result)
        {
            if (!IsActive || creator != null)
            {
                return true;
            }

            // 塞给队友的牌照常加入，塞给反制者的标为未加入
            List<CardModel> all = cards.ToList();
            List<CardModel> teammates = all.Where(c => c.Owner?.Creature is { } owner && IsTeammate(owner)).ToList();
            List<CardPileAddResult> blocked = all.Except(teammates)
                .Select(c => new CardPileAddResult { success = false, cardAdded = c })
                .ToList();
            __result = teammates.Count == 0
                ? Task.FromResult<IReadOnlyList<CardPileAddResult>>(blocked)
                : Suspended(async () =>
                {
                    // 全部结果
                    List<CardPileAddResult> results =
                        [.. await CardPileCmd.AddGeneratedCardsToCombat(teammates, newPileType, creator, position)];
                    results.AddRange(blocked);
                    return (IReadOnlyList<CardPileAddResult>)results;
                });
            return false;
        }
    }
}
