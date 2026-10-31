using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次祝福基类：只对下一场战斗生效，战斗结束后自动从遗物栏移除。
/// </summary>
public abstract class KnightTemporaryRelic : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await RelicCmd.Remove(this);
    }
}
