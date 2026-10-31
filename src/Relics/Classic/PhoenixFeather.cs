using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 凤凰羽毛：下次死亡时重生，并恢复所有生命。只生效 1 次。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class PhoenixFeather : KnightRelic
{
    /// <summary>重生时回复生命上限的比例（同原版蜥蜴尾巴）。</summary>
    private const decimal RevivePercent = 0.5m;

    /// <summary>是否已用过。</summary>
    private bool used;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool IsUsedUp => used;

    /// <summary>是否已用过（存档保存）。</summary>
    [SavedProperty]
    public bool FmkPhoenixUsed
    {
        get
        {
            return used;
        }
        set
        {
            AssertMutable();
            used = value;
            if (used)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    /// <inheritdoc />
    public override bool ShouldDieLate(Creature creature)
    {
        return creature != Owner.Creature || used;
    }

    /// <inheritdoc />
    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        FmkPhoenixUsed = true;
        await CreatureCmd.Heal(creature, Math.Floor(creature.MaxHp * RevivePercent));
    }
}
