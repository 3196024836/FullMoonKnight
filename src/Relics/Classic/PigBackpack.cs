using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 猪皮背包（原作"获得 2 个装备槽，基础行动力 -1"）：每场战斗开始时自动装备 2 件随机装备；每回合能量 -1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class PigBackpack : KnightRelic
{
    /// <summary>装备件数。</summary>
    private const int EquipCount = 2;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? Math.Max(0m, amount - 1m) : amount;
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
