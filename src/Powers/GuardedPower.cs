using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Powers;

/// <summary>
/// 护身：直到你的下个回合，每次受到他人的伤害减少层数（尖刺盾牌、石化皮肤使用）。
/// </summary>
public sealed class GuardedPower : KnightPower
{
    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者受到的每次伤害减少层数（自己造成的除外）。
    /// </summary>
    /// <param name="target">受伤者。</param>
    /// <param name="amount">当前伤害。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>伤害增量。</returns>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        return target == Owner && dealer != Owner ? -Math.Min(Amount, amount) : 0m;
    }

    /// <summary>
    /// 持有者的下个回合开始时移除。
    /// </summary>
    /// <param name="side">开始回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
