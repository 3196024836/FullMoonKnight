using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Relics;

/// <summary>
/// 守卫雕像（原作持续 3 回合）：前 3 个回合，每回合开始时布置 1 个"从容"反制，使敌方的下一次行动无效。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class GuardianStatue : KnightRelic
{
    private const int Turns = 3;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || (Owner.PlayerCombatState?.TurnNumber ?? 0) > Turns)
        {
            return;
        }

        Flash();
        await ComposurePower.PlaceMany(choiceContext, Owner.Creature, 1);
    }
}
