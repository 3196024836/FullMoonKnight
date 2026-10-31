using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 天使法典：拾取时生命上限 +10，并随机获得 3 张女骑士牌（按普通战斗奖励的稀有度）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class AngelCodex : KnightRelic
{
    /// <summary>生命上限加成。</summary>
    private const decimal MaxHpBonus = 10m;

    /// <summary>获得的牌数。</summary>
    private const int CardCount = 3;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpBonus);
        // 随机牌
        CardCreationOptions options = new([ModelDb.CardPool<KnightCardPool>()], CardCreationSource.Other,
            CardRarityOddsType.RegularEncounter);
        foreach (CardCreationResult result in CardFactory.CreateForReward(Owner, CardCount, options).ToList())
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(result.Card, PileType.Deck));
        }
    }
}
