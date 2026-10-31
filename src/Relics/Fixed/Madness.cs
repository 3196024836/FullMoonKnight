using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 疯狂（原作 1090，妖精低语）：复制牌组中 1 张牌，然后将 1 张恶作剧放入牌组。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Madness : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CopyDeckCards(1);
        CardModel prank = Owner.RunState.CreateCard(ModelDb.Card<PrankCurse>(), Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(prank, PileType.Deck));
    }
}
