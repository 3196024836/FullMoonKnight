using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 贪婪：抽牌，然后弃掉手牌中所有非攻击牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Greed : KnightCard
{
    /// <summary>
    /// 创建贪婪。
    /// </summary>
    public Greed() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        // 手牌中的非攻击牌
        List<CardModel> discards = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type != CardType.Attack).ToList();
        if (discards.Count > 0)
        {
            await CardCmd.Discard(choiceContext, discards);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
