using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Relics;

/// <summary>
/// 鹰之警觉：每场战斗开始时布置 1 个"从容"反制，使敌人的第 1 次行动无效。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class EagleVigilance : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return;
        }
        Flash();
        await ComposurePower.PlaceMany(choiceContext, Owner.Creature, 1);
    }
}
