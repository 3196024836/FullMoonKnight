using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 量产专家：在商店购买 1 张牌后，本次商店其余卡牌价格减半。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class MassProduction : KnightRelic
{
    private bool _discountActive;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        _discountActive = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player == Owner && itemPurchased is MerchantCardEntry && !_discountActive)
        {
            _discountActive = true;
            Flash();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (!_discountActive || player != Owner || entry is not MerchantCardEntry)
        {
            return cost;
        }

        return Math.Floor(cost / 2m);
    }
}
