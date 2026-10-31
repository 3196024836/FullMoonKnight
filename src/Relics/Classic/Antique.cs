using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 古董：随机获得 1 张装备牌，加入牌组。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Antique : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        List<CardModel> equipment = ModelDb.CardPool<KnightCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(KnightCards.IsEquipment)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche);
        if (equipment.Count == 0)
        {
            return;
        }

        CardModel card = Owner.RunState.CreateCard(equipment[0], Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }
}
