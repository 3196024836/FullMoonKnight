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
/// 尖刺壁垒：回合结束时，你每有 3 点格挡，对随机敌人造成伤害。
/// 原作每 4 点；女骑士格挡来源少（单卡模拟 -2.0 层），改为每 3 点。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class SpikedRampart : KnightEquipmentCard<SpikedRampartPower>
{
    /// <summary>
    /// 创建尖刺壁垒。
    /// </summary>
    public SpikedRampart() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(1m, ValueProp.Unpowered), new DynamicVar("BlockStep", SpikedRampartPower.BlockPerStep)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 尖刺壁垒装备：层数为每 3 点格挡对应的伤害。
/// </summary>
public sealed class SpikedRampartPower : KnightEquipmentPower
{
    /// <summary>每多少格挡计 1 次。</summary>
    public const int BlockPerStep = 3;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockStep", BlockPerStep)];

    /// <summary>
    /// 持有者回合结束时，按格挡对随机敌人造成伤害。
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
        // 伤害
        decimal damage = Owner.Block / BlockPerStep * Amount;
        // 随机目标
        Creature? target = KnightCards.RandomEnemy(Owner.Player!);
        if (damage > 0m && target != null)
        {
            Flash();
            await KnightDamage.Deal(choiceContext, target, damage, ValueProp.Unpowered | KnightDamage.Equipment,
                Owner, null);
        }
    }
}
