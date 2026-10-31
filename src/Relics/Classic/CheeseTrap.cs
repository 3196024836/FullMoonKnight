using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 奶酪陷阱：装备造成的伤害 +1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CheeseTrap : KnightRelic
{
    /// <summary>伤害加成。</summary>
    private const decimal BonusDamage = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        return dealer == Owner.Creature && props.IsEquipment() ? BonusDamage : 0m;
    }
}
