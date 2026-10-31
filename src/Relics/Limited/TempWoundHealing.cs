using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·伤口愈合：下次战斗，每回合开始时回复已损失生命的 10%（向下取整）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempWoundHealing : KnightTemporaryRelic
{
    private const decimal HealRatio = 0.10m;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        decimal missing = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        decimal heal = Math.Floor(missing * HealRatio);
        if (heal <= 0m)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner.Creature, heal);
    }
}
