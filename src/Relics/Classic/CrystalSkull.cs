using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Relics;

/// <summary>
/// 水晶头骨：每回合开始时，所有敌人获得 4 层流血。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CrystalSkull : KnightRelic
{
    /// <summary>每回合施加的流血层数。</summary>
    private const decimal BleedAmount = 4m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature.CombatState is not { } combat || combat.HittableEnemies.Count == 0)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<BleedPower>(choiceContext, combat.HittableEnemies, BleedAmount, Owner.Creature, null);
    }
}
