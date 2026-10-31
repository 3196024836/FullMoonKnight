using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·黑暗乐章：每场战斗开始时不洗牌，抽牌堆按牌组顺序排列（起手牌和牌序固定）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseDarkMovement : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        if (player != Owner || !isInitialShuffle)
        {
            return;
        }
        // 按牌组顺序排列；不是牌组里的牌放到最后
        List<CardModel> deck = Owner.Deck.Cards.ToList();
        List<CardModel> ordered = cards
            .OrderBy(c => c.DeckVersion is { } d && deck.IndexOf(d) is var i and >= 0 ? i : int.MaxValue)
            .ToList();
        cards.Clear();
        cards.AddRange(ordered);
    }
}
