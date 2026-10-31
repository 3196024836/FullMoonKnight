using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 营地篝火（原作"角色升级时的卡牌奖励额外复制 1 张"）：从精英或首领战的卡牌奖励选牌时，额外获得 1 张复制。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CampfireRest : KnightRelic
{
    /// <summary>最近一张加入牌组的牌。</summary>
    private CardModel? _lastAdded;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        _lastAdded = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner == Owner && card.Pile?.Type == PileType.Deck && oldPileType != PileType.Deck
            && clonedBy != this)
        {
            _lastAdded = card;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        // 本次奖励选中的牌
        CardModel? chosen = _lastAdded;
        _lastAdded = null;
        if (player != Owner || reward is not CardReward || chosen == null
            || Owner.RunState.CurrentRoom?.RoomType is not (RoomType.Elite or RoomType.Boss))
        {
            return;
        }
        Flash();
        CardModel copy = Owner.RunState.CloneCard(chosen);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck, clonedBy: this));
    }
}
