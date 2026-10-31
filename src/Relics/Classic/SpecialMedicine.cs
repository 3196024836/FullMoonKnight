using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 特效药：首领战开始时，把 1 张"特效药"加入手牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SpecialMedicine : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player) || Owner.RunState.CurrentRoom?.RoomType != RoomType.Boss)
        {
            return;
        }
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(
            Owner.Creature.CombatState!.CreateCard<SpecialMedicineCard>(Owner), PileType.Hand, Owner);
    }
}
