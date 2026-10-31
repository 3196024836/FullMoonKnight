using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 捕兽夹：敌人的攻击每命中你一次，对它造成伤害。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BearTrap : KnightEquipmentCard<BearTrapPower>
{
    /// <summary>
    /// 创建捕兽夹。
    /// </summary>
    public BearTrap() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Unpowered)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

/// <summary>
/// 捕兽夹装备：层数为每次反击伤害。
/// </summary>
public sealed class BearTrapPower : KnightEquipmentPower
{
    /// <summary>
    /// 敌人的攻击每命中持有者一次，对攻击者造成伤害。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer.Side == Owner.Side || !dealer.IsAlive
            || !props.IsPoweredAttack())
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, dealer, Amount, ValueProp.Unpowered | KnightDamage.Equipment,
            Owner, null);
    }
}
