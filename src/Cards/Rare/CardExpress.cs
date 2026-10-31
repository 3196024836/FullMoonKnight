using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 卡片速递：消耗，抽牌直到手牌达到指定张数。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class CardExpress : KnightCard
{
    /// <summary>
    /// 创建卡片速递。
    /// </summary>
    public CardExpress() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 还差的张数
        int missing = DynamicVars.Cards.IntValue - PileType.Hand.GetPile(Owner).Cards.Count;
        if (missing > 0)
        {
            await CardPileCmd.Draw(choiceContext, missing, Owner);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
