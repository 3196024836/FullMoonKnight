using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 亡者契约：当前生命 -15，生命上限 +15。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class DeathPact : KnightRelic
{
    /// <summary>生命上限增加量与当前生命减少量。</summary>
    private const decimal Amount = 15m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        // 拾取前的生命（加生命上限会同时加当前生命）
        decimal hp = Owner.Creature.CurrentHp;
        await CreatureCmd.GainMaxHp(Owner.Creature, Amount);
        await CreatureCmd.SetCurrentHp(Owner.Creature, Math.Max(1m, hp - Amount));
    }
}
