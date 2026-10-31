using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 女骑士遗物基类：图标从模组包读取。
/// 图标文件名为 ID 去掉前缀后的小写形式，例如 knight_oath.png。
/// 同时提供拾取时常用的牌组/生命操作，供各遗物复用。
/// </summary>
public abstract class KnightRelic : CustomRelicModel
{
    /// <summary>图标文件基名。</summary>
    private string IconName => KnightAssets.BaseName(Id.Entry);

    /// <inheritdoc />
    public override string PackedIconPath => KnightAssets.Image($"relics/{IconName}.png");

    /// <inheritdoc />
    protected override string PackedIconOutlinePath => KnightAssets.Image($"relics/outline/{IconName}.png");

    /// <inheritdoc />
    protected override string BigIconPath => KnightAssets.Image($"relics/{IconName}.png");

    /// <summary>拾取时失去生命上限（战斗外，不触发卡牌来源效果）。</summary>
    protected Task LoseMaxHp(decimal amount)
    {
        return CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, amount, isFromCard: false);
    }

    /// <summary>拾取时失去当前生命（无视格挡与能力）。</summary>
    protected Task LoseHp(decimal amount)
    {
        return KnightDamage.Deal(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    }

    /// <summary>让玩家从牌组中选择 <paramref name="count"/> 张牌并复制到牌组。</summary>
    protected async Task CopyDeckCards(int count)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, count);
        var picked = await CardSelectCmd.FromDeckGeneric(Owner, prefs, c => c.Type != CardType.Quest);
        foreach (CardModel card in picked)
        {
            CardModel copy = Owner.RunState.CloneCard(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck));
        }
    }

    /// <summary>让玩家从牌组中选择最多 <paramref name="maxCount"/> 张牌并删除。</summary>
    protected async Task RemoveDeckCards(int maxCount)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, maxCount);
        var picked = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList();
        if (picked.Count > 0)
        {
            await CardPileCmd.RemoveFromDeck(picked);
        }
    }

    /// <summary>让玩家从牌组中选择 <paramref name="count"/> 张牌升级。</summary>
    protected async Task UpgradeDeckCards(int count)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, count);
        foreach (CardModel card in await CardSelectCmd.FromDeckForUpgrade(Owner, prefs))
        {
            CardCmd.Upgrade(card);
        }
    }

    /// <summary>
    /// 自动装备 <paramref name="count"/> 件随机的女骑士装备（装备槽类遗物）。装备牌直接打出，不进牌堆。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="count">件数。</param>
    /// <returns>打出的装备牌。</returns>
    protected async Task<List<CardModel>> AutoEquip(PlayerChoiceContext choiceContext, int count)
    {
        // 随机装备
        List<CardModel> equipment = KnightCards.RandomKnightCards(Owner, count, KnightCards.IsEquipment);
        foreach (CardModel card in equipment)
        {
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
        return equipment;
    }

    /// <summary>
    /// 是否本场战斗的第 1 个玩家回合（战斗开始时触发的效果用）。
    /// </summary>
    /// <param name="player">回合开始的玩家。</param>
    /// <returns>是持有者的第 1 回合时为 true。</returns>
    protected bool IsFirstTurnOf(Player player)
    {
        return player == Owner && Owner.PlayerCombatState?.TurnNumber == 1;
    }
}
