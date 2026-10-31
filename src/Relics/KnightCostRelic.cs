using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 代价基类（恶魔交易的代价）：拾取时当场支付，付完从遗物栏移除。
/// </summary>
public abstract class KnightCostRelic : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <summary>是否大代价：恶魔交易里大代价换罕见祝福，小代价换普通祝福。</summary>
    public virtual bool Greater => false;

    /// <summary>
    /// 玩家现在付得起吗：失去生命和生命上限后至少留 1 点，金币要够。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>付得起时为 true。</returns>
    public virtual bool CanPay(Player player)
    {
        return true;
    }

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await Pay();
        await RelicCmd.Remove(this);
    }

    /// <summary>
    /// 支付代价。
    /// </summary>
    /// <returns>异步任务。</returns>
    protected abstract Task Pay();
}
