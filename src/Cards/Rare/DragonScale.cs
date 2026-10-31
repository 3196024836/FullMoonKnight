using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 龙鳞铠甲：你受到的每次伤害减少。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class DragonScale : KnightEquipmentCard<DragonScalePower>
{
    /// <summary>
    /// 创建龙鳞铠甲。
    /// </summary>
    public DragonScale() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 2m)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars["Reduction"].BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["Reduction"].UpgradeValueBy(1m);
    }
}

/// <summary>
/// 龙鳞铠甲装备：层数为每次伤害的减少量（自伤不减）。
/// </summary>
public sealed class DragonScalePower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者受到的每次伤害减少层数。
    /// </summary>
    /// <param name="target">受伤者。</param>
    /// <param name="amount">当前伤害。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>伤害增量（负数）。</returns>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        return target == Owner && dealer != Owner && amount > 0m ? -Math.Min(Amount, amount) : 0m;
    }
}
