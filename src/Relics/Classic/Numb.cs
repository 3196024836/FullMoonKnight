using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 麻木：受到的所有伤害（穿透除外）减少 1 点。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Numb : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature)
        {
            return amount;
        }

        // Unblockable damage (e.g. Poison) bypasses this reduction.
        if (props.HasFlag(ValueProp.Unblockable))
        {
            return amount;
        }

        return Math.Max(0m, amount - 1m);
    }
}
