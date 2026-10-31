using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 亢奋之书（原作"可升级的卡牌等级 +1，怪物等级 +1"）：卡牌奖励里的牌升级几率 +50%；
/// 每场战斗开始时，所有敌人获得 1 点力量。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class FrenzyBook : KnightRelic
{
    /// <summary>升级几率加成。</summary>
    private const decimal UpgradeOddsBonus = 0.5m;

    /// <summary>敌人获得的力量。</summary>
    private const decimal EnemyStrength = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
    {
        return player == Owner ? odds + UpgradeOddsBonus : odds;
    }

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player) || Owner.Creature.CombatState is not { } combat
            || combat.HittableEnemies.Count == 0)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, combat.HittableEnemies, EnemyStrength, Owner.Creature,
            null);
    }
}
