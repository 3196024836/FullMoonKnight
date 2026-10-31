using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 勇敢的心：抽 1 张牌，获得 1 张随机临时攻击牌（升级后该牌已升级）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BraveHeart : KnightCard
{
    /// <summary>
    /// 创建勇敢的心。
    /// </summary>
    public BraveHeart() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        foreach (CardModel card in KnightCards.RandomKnightCards(Owner, 1, c => c.Type == CardType.Attack))
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
            await KnightCards.AddTemporary(card, PileType.Hand, Owner);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
    }
}
