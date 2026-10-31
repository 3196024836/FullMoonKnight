using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 尖刺盾牌：造成伤害；直到下个回合，每次受到的伤害减少。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class SpikedShield : KnightCard
{
    /// <summary>
    /// 创建尖刺盾牌。
    /// </summary>
    public SpikedShield() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move), new PowerVar<GuardedPower>(2m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<GuardedPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.BluntVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<GuardedPower>(choiceContext, Owner.Creature, DynamicVars["GuardedPower"].BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
