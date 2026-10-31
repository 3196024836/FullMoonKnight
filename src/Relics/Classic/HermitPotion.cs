using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 隐者秘药：生命上限 -6；本局游戏每获得 1 张牌，生命上限 +3。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class HermitPotion : KnightRelic
{
    private const decimal MaxHpPenalty = 6m;
    private const decimal MaxHpPerCard = 3m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await LoseMaxHp(MaxHpPenalty);
    }

    /// <inheritdoc />
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        // Only cards that newly enter the run deck count; combat-time clones go to Draw/Hand, not Deck.
        if (card.Pile?.Type != PileType.Deck || card.Owner != Owner || oldPileType == PileType.Deck)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpPerCard);
    }
}
