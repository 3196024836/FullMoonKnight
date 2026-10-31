using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 发条马：回合结束时，对随机敌人造成伤害并获得同样多的格挡。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ClockworkHorse : KnightEquipmentCard<ClockworkHorsePower>
{
    /// <summary>
    /// 创建发条马。
    /// </summary>
    public ClockworkHorse() : base(CardRarity.Common)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Unpowered), new BlockVar(5m, ValueProp.Unpowered)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 发条马装备：层数为每回合的伤害和格挡。
/// </summary>
public sealed class ClockworkHorsePower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合结束时，对随机敌人造成伤害并获得格挡。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="side">结束回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <returns>异步任务。</returns>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        Flash();
        // 随机目标
        Creature? target = KnightCards.RandomEnemy(Owner.Player!);
        if (target != null)
        {
            await KnightDamage.Deal(choiceContext, target, Amount, ValueProp.Unpowered | KnightDamage.Equipment,
                Owner, null);
        }
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
