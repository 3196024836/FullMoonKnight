using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 礼品卡：商店角色卡牌一栏最右边的那张牌打对折。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class GiftCard : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != Owner || entry is not MerchantCardEntry card
            || card._inventory?.CharacterCardEntries.LastOrDefault() != card)
        {
            return cost;
        }
        return Math.Floor(cost / 2m);
    }
}
