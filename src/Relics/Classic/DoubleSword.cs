using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 双头剑：造成伤害 +1，受到伤害 +1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class DoubleSword : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return dealer == Owner.Creature && props.IsPoweredAttack() ? 1m : 0m;
    }

    /// <inheritdoc />
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 格挡完全挡住时原版也会以 0 调用，这时不加
        return target == Owner.Creature && amount > 0m ? amount + 1m : amount;
    }
}
