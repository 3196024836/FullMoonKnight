using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 猫之优雅：每次战斗开始时获得 2 点行动力。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CatGrace : KnightRelic
{
    private const decimal EnergyGain = 2m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1)
        {
            return;
        }

        await PlayerCmd.GainEnergy(EnergyGain, Owner);
    }
}
