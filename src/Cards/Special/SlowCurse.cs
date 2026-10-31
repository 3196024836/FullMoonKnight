using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace FullMoonKnight.Cards;

/// <summary>
/// 减速（诅咒）：1 费，消耗，抽 1 张牌。复制幻象、恶魔交易的代价。不进随机诅咒池。
/// </summary>
[Pool(typeof(CurseCardPool))]
public sealed class SlowCurse : KnightCard
{
    /// <summary>
    /// 创建减速。
    /// </summary>
    public SlowCurse() : base(1, CardType.Curse, CardRarity.Curse, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}
