using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 奇怪的符文：敌人回合中，敌人受到的伤害提高。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class StrangeRune : KnightEquipmentCard<StrangeRunePower>
{
    /// <summary>
    /// 创建奇怪的符文。
    /// </summary>
    public StrangeRune() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Bonus", 3m)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars["Bonus"].BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["Bonus"].UpgradeValueBy(1m);
    }
}

/// <summary>
/// 奇怪的符文装备：层数为伤害增量。
/// </summary>
public sealed class StrangeRunePower : KnightEquipmentPower
{
    /// <summary>
    /// 敌人回合中，敌人受到的每次伤害增加层数。
    /// </summary>
    /// <param name="target">受伤者。</param>
    /// <param name="amount">当前伤害。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>伤害增量。</returns>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        return target != null && target.Side != Owner.Side && amount > 0m
            && CombatState.CurrentSide == CombatSide.Enemy ? Amount : 0m;
    }
}
