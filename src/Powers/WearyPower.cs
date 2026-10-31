using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace FullMoonKnight.Powers;

/// <summary>
/// 疲惫：下回合少抽层数张牌（力量透支、猛劈、无畏使用）。
/// 与原生"下回合多抽"相同的时序：本回合施加的层数在下回合开始时生效，生效后移除。
/// </summary>
public sealed class WearyPower : KnightPower
{
    /// <inheritdoc />
    public override PowerType Type => PowerType.Debuff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 下回合的抽牌数减少层数。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="count">当前抽牌数。</param>
    /// <returns>修改后的抽牌数。</returns>
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player || AmountOnTurnStart == 0)
        {
            return count;
        }
        return count - Amount;
    }

    /// <summary>
    /// 生效的回合开始后移除。
    /// </summary>
    /// <param name="side">开始回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner) && AmountOnTurnStart != 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
