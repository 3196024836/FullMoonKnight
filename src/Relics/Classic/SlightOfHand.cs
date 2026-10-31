using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Relics;

/// <summary>
/// 娴熟戏法（原作"战斗开始时将 2 张恶作剧洗入敌方牌组"）：每场战斗开始时，所有敌人获得 2 层恶作剧。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SlightOfHand : KnightRelic
{
    /// <summary>恶作剧层数。</summary>
    private const decimal MischiefAmount = 2m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player) || Owner.Creature.CombatState is not { } combat
            || combat.HittableEnemies.Count == 0)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<MischiefPower>(choiceContext, combat.HittableEnemies, MischiefAmount, Owner.Creature,
            null);
    }
}
