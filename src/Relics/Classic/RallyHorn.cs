using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 集结号角：每打出 1 张技能牌，从抽牌堆随机打出 1 张攻击牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class RallyHorn : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Skill)
        {
            return;
        }
        // 抽牌堆里随机一张可打出的攻击牌
        CardModel? attack = PileType.Draw.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Attack && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Shuffle)
            .FirstOrDefault();
        if (attack == null)
        {
            return;
        }
        Flash();
        await CardCmd.AutoPlay(choiceContext, attack, null);
    }
}
