using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 行动迟缓（原作 1087，恶魔交易）：获得 66 金币，将 2 张减速放入牌组。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SluggishTrade : KnightRelic
{
    /// <summary>金币。</summary>
    private const int Gold = 66;

    /// <summary>减速张数。</summary>
    private const int SlowCount = 2;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(Gold, Owner);
        for (int i = 0; i < SlowCount; i++)
        {
            CardModel slow = Owner.RunState.CreateCard(ModelDb.Card<SlowCurse>(), Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(slow, PileType.Deck));
        }
    }
}
