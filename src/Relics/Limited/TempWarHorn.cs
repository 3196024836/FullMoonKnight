using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·激进号角：下次战斗开始时额外抽 3 张牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempWarHorn : KnightTemporaryRelic
{
    private const decimal ExtraDraw = 3m;

    /// <inheritdoc />
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1)
        {
            return count;
        }

        return count + ExtraDraw;
    }
}
