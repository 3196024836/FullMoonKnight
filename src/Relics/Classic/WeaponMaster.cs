using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 武器大师（原作"装备槽 +1，战斗开始时把装备槽中所有装备的复制洗入牌组"）：
/// 每场战斗开始时自动装备 1 件随机装备，并把它的临时复制洗入抽牌堆。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class WeaponMaster : KnightRelic
{
    /// <summary>装备件数。</summary>
    private const int EquipCount = 1;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return;
        }
        Flash();
        foreach (CardModel equipment in await AutoEquip(choiceContext, EquipCount))
        {
            await KnightCards.AddTemporary(KnightCards.CombatCopy(equipment, Owner), PileType.Draw, Owner,
                CardPilePosition.Random);
        }
    }
}
