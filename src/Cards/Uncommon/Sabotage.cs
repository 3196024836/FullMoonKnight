using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 破坏：移除目标 1 个通用增益（有的话），造成伤害。
/// 原作没有增益时无效果；女骑士只有 1 点能量，打空等于浪费一回合（单卡模拟 -3.9 层），改为总是造成伤害。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Sabotage : KnightCard
{
    /// <summary>
    /// 创建破坏。
    /// </summary>
    public Sabotage() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        // 可移除的增益
        List<PowerModel> buffs = cardPlay.Target.Powers
            .Where(p => p.TypeForCurrentAmount == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Buff
                && StealScope.IsCommonBuff(p))
            .ToList();
        if (buffs.Count > 0)
        {
            await PowerCmd.Remove(Owner.RunState.Rng.CombatTargets.NextItem(buffs));
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.BluntVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
