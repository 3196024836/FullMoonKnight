using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 骷髅护符：生命上限 +5；每次战斗抵抗一次大于 7 点的伤害。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SkullAmulet : KnightRelic
{
    private const decimal DamageThreshold = 7m;

    private bool _used;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, 5);
    }

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _used = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_used || target != Owner.Creature || amount <= DamageThreshold || props.HasFlag(ValueProp.Unblockable))
        {
            return amount;
        }

        _used = true;
        return 0m;
    }
}
