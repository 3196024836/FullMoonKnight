using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·凋零乐章：商人、忘忧酒馆、女巫药剂室、生命之泉的价格增加 50%（原作为铁匠铺、酒馆、女巫药剂室）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseWitherMovement : KnightRelic
{
    private const decimal PriceMultiplier = 1.5m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        return player == Owner ? Math.Ceiling(cost * PriceMultiplier) : cost;
    }

    /// <summary>
    /// 事件里的花费（忘忧酒馆、女巫药剂室、生命之泉）：带凋零乐章时同样增加 50%。
    /// </summary>
    /// <param name="player">付钱的玩家。</param>
    /// <param name="cost">原价。</param>
    /// <returns>实际价格。</returns>
    public static int EventPrice(Player player, int cost)
    {
        return player.GetRelic<CurseWitherMovement>() == null ? cost : (int)Math.Ceiling(cost * PriceMultiplier);
    }
}
