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
/// 撞击：按当前生命比例造成伤害，低生命时有保底。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Ram : KnightCard
{
    /// <summary>比例变量名。</summary>
    private const string PercentVar = "Percent";

    /// <summary>最低伤害变量名。</summary>
    private const string MinDamageVar = "MinDamage";

    /// <summary>
    /// 创建撞击。
    /// </summary>
    public Ram() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(PercentVar, 25m),
        new DynamicVar(MinDamageVar, 12m),
        .. MakeCalculatedDamage(0, (card, _) => Math.Max(card.DynamicVars[MinDamageVar].BaseValue,
            card.Owner.Creature.CurrentHp * card.DynamicVars[PercentVar].BaseValue / 100m), 1, ValueProp.Move),
    ];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.BluntVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars[PercentVar].UpgradeValueBy(5m);
        DynamicVars[MinDamageVar].UpgradeValueBy(4m);
    }
}
