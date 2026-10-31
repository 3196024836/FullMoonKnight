using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 鹿之轻盈：战斗开始时额外抽 1 张牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class DeerSwiftness : KnightRelic
{
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

        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
