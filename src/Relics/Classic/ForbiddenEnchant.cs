using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 禁忌附魔（原作"获得 2 个装备槽，战斗开始时少抽 1 张牌"）：每场战斗开始时自动装备 2 件随机装备；
/// 每场战斗第 1 回合少抽 1 张牌。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class ForbiddenEnchant : KnightRelic
{
    /// <summary>装备件数。</summary>
    private const int EquipCount = 2;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <inheritdoc />
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return IsFirstTurnOf(player) ? count - 1m : count;
    }

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return;
        }
        Flash();
        await AutoEquip(choiceContext, EquipCount);
    }
}
