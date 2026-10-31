using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 剑术训练：造成的伤害 +1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SwordTraining : KnightRelic
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
}
