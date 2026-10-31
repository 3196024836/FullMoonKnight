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
/// 蓄力一击：消耗，造成伤害并眩晕目标。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ChargedBlow : KnightCard
{
    /// <summary>
    /// 创建蓄力一击。
    /// </summary>
    public ChargedBlow() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.BluntVfx)
            .Execute(choiceContext);
        if (cardPlay.Target.IsAlive)
        {
            await CreatureCmd.Stun(cardPlay.Target);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
