using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 武器之书（原作"获得 1 个装备槽，所有怪物装备 1 把短剑"）：每场战斗开始时自动装备 1 件随机装备；
/// 所有敌人获得 1 点力量。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class WeaponBook : KnightRelic
{
    /// <summary>装备件数。</summary>
    private const int EquipCount = 1;

    /// <summary>敌人获得的力量。</summary>
    private const decimal EnemyStrength = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Shop;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return;
        }
        Flash();
        await AutoEquip(choiceContext, EquipCount);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature.CombatState!.HittableEnemies,
            EnemyStrength, Owner.Creature, null);
    }
}
