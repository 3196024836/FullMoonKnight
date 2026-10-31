using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 有备而来（原作"获得 1 个装备槽"）：每场战斗开始时自动装备 1 件随机的女骑士装备。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class Prepared : KnightRelic
{
    /// <summary>装备件数。</summary>
    private const int EquipCount = 1;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Uncommon;

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
