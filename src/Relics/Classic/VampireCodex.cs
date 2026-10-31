using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 吸血魔典：每造成 15 点伤害回复 1 点生命；生命上限 -5。
/// 原来每 5 点回 1 点，一场战斗回 20～60 点，远超原版燃烧之血（6）和黑暗之血（12）；改为每 15 点，普通战斗约 5 点、首领约 20 点。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class VampireCodex : KnightRelic
{
    /// <summary>每回复 1 点生命需要累计的伤害。</summary>
    private const decimal HealThreshold = 15m;

    private decimal _damageAccumulator;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await LoseMaxHp(5m);
    }

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _damageAccumulator = 0m;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature || result.UnblockedDamage <= 0)
        {
            return;
        }

        _damageAccumulator += result.UnblockedDamage;

        if (_damageAccumulator >= HealThreshold)
        {
            int heals = (int)(_damageAccumulator / HealThreshold);
            _damageAccumulator -= heals * HealThreshold;
            await CreatureCmd.Heal(Owner.Creature, heals);
        }
    }
}
