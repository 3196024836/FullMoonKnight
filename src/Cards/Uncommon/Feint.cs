using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
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
/// 虚晃：消耗，选择手牌中 1 张攻击牌，把它的若干张临时复制洗入抽牌堆。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Feint : KnightCard
{
    /// <summary>
    /// 创建虚晃。
    /// </summary>
    public Feint() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 选中的攻击牌
        CardModel? chosen = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), c => c.Type == CardType.Attack, this)).FirstOrDefault();
        if (chosen == null)
        {
            return;
        }
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await KnightCards.AddTemporary(chosen.CreateClone(), PileType.Draw, Owner, CardPilePosition.Random);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
