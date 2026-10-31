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
/// 致命武器：你每有 1 件装备，造成 1 次伤害。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class DeadlyArsenal : KnightCard
{
    /// <summary>
    /// 创建致命武器。
    /// </summary>
    public DeadlyArsenal() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        // 段数：装备件数
        int hits = Powers.KnightEquipmentPower.CountEquipment(Owner.Creature);
        if (hits <= 0)
        {
            return;
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitCount(hits)
            .WithHitFx(KnightDamage.SlashVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
