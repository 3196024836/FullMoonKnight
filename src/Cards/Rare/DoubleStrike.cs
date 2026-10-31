using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 二次打击：造成伤害，从抽牌堆抽 2 张牌（不洗牌，见 <see cref="KnightCards.DrawFromPile" />）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class DoubleStrike : KnightCard
{
    /// <summary>
    /// 创建二次打击。
    /// </summary>
    public DoubleStrike() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move), new CardsVar(2)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.SlashVfx)
            .Execute(choiceContext);
        await KnightCards.DrawFromPile(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        // 原作是"2 伤害，抽 2 张"（第六批还原抽 2）；升级只加伤害
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
