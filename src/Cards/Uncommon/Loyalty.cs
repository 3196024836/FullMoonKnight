using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 忠诚：消耗，获得能量，受到 1 点穿刺伤害，抽牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Loyalty : KnightCard
{
    /// <summary>
    /// 创建忠诚。
    /// </summary>
    public Loyalty() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new HpLossVar(1m), new CardsVar(1)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await KnightCards.SelfDamage(choiceContext, this, DynamicVars.HpLoss.BaseValue, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
