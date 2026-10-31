using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

using FullMoonKnight.Mechanics;
using FullMoonKnight.Visuals;

namespace FullMoonKnight.Powers;

/// <summary>
/// 反制触发的条件。
/// </summary>
public enum CounterCondition
{
    /// <summary>敌人的行动包含攻击。</summary>
    Attack,

    /// <summary>敌人的行动不包含攻击。</summary>
    NonAttack,

    /// <summary>敌人的任何行动。</summary>
    Any,
}

/// <summary>
/// 反制能力基类：反制牌打出后布置在自己身上，每张牌一个实例，显示在状态栏。
/// 敌人执行满足条件的行动时，最早布置的那个反制触发：该行动整个无效，然后结算附带效果，反制移除。
/// 没触发的反制留到战斗结束。数值从布置它的牌复制（同名变量），升级版和普通版可以并存。
/// </summary>
public abstract class KnightCounterPower : KnightPower
{
    // ===== 字段 =====

    /// <summary>布置序号计数器；联机各端按相同顺序打牌，序号一致。</summary>
    private static long nextSerial;

    /// <summary>本实例的布置序号，越小越早。</summary>
    private long serial;

    // ===== 属性 =====

    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Single;

    /// <inheritdoc />
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    /// <summary>布置序号。</summary>
    public long Serial => serial;

    /// <summary>触发条件。</summary>
    public abstract CounterCondition Condition { get; }

    /// <summary>是否夺取（结算时自己在夺取范围内执行原行动）；否则只让原行动对布置者无效。</summary>
    public virtual bool Steals => false;

    // ===== 判断与结算 =====

    /// <summary>
    /// 敌人的这次行动是否触发本反制。眩晕、睡眠这类没有实际行动的回合不触发。
    /// </summary>
    /// <param name="move">敌人将要执行的行动。</param>
    /// <returns>触发时为 true。</returns>
    public bool Matches(MoveState move)
    {
        // 行动是否包含攻击
        bool isAttack = move.Intents.Any(i => i is AttackIntent);
        // 是否没有实际行动
        bool isIdle = move.Intents.All(i => i.IntentType is IntentType.Stun or IntentType.Sleep);
        return Condition switch
        {
            CounterCondition.Attack => isAttack,
            CounterCondition.NonAttack => !isAttack && !isIdle,
            _ => !isIdle,
        };
    }

    /// <summary>
    /// 敌人的行动已被无效后，结算本反制的附带效果。
    /// </summary>
    /// <param name="context">触发信息。</param>
    /// <returns>异步任务。</returns>
    public abstract Task Resolve(CounterContext context);

    // ===== 布置 =====

    /// <summary>
    /// 布置一个反制：给牌的持有者施加新实例，并从牌复制同名数值。
    /// </summary>
    /// <typeparam name="T">反制能力类型。</typeparam>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="card">反制牌。</param>
    /// <returns>布置的反制；战斗结束等情况下为空。</returns>
    public static async Task<T?> Place<T>(PlayerChoiceContext choiceContext, CardModel card)
        where T : KnightCounterPower
    {
        // 布置者
        Creature owner = card.Owner.Creature;
        // 新实例
        T? power = await PowerCmd.Apply<T>(choiceContext, owner, 1m, owner, card);
        if (power == null)
        {
            return null;
        }

        power.serial = ++nextSerial;
        SfxCmd.Play(KnightAudio.CounterSet);
        foreach (KeyValuePair<string, DynamicVar> pair in power.DynamicVars)
        {
            if (card.DynamicVars.TryGetValue(pair.Key, out DynamicVar? source))
            {
                pair.Value.BaseValue = source.BaseValue;
            }
        }
        return power;
    }
}
