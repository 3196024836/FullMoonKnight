using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 野性之火：每场战斗开始时，起手牌的能量消耗在本场战斗中 -1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class WildFire : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return Task.CompletedTask;
        }
        Flash();
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
        {
            card.EnergyCost.AddThisCombat(-1, reduceOnly: true);
        }
        return Task.CompletedTask;
    }
}
