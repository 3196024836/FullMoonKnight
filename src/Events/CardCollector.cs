using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 卡牌收藏家（原作 4，DealerPanel）：交易 2 次。交出牌组里任意 1 张，换 1 张等级在 [交出卡等级 −1, +4] 的卡
/// （权重 warrior × boxMul，不同名，不出特殊牌）。原作 CommFunc.GetDealerCard。
/// </summary>
public sealed class CardCollector : KnightEvent
{
    /// <summary>交易次数。</summary>
    private const int Trades = 2;

    /// <summary>已交易次数。</summary>
    private int _traded;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Left", Trades)];

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars["Left"].BaseValue = Trades - _traded;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return DealerOptions(InitialPage);
    }

    /// <summary>
    /// 交易（没有能交出的牌时锁定）、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] DealerOptions(string page)
    {
        bool canTrade = Owner!.Deck.Cards.Any(CanTrade);
        return [Choice(canTrade ? Trade : null, page, "TRADE"), LeaveOption()];
    }

    /// <summary>
    /// 能交出的牌：能删除的牌（诅咒、状态、任务牌除外）。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>能交出时为 true。</returns>
    private static bool CanTrade(CardModel card)
    {
        return card.IsRemovable && card.Type is not (CardType.Curse or CardType.Quest or CardType.Status);
    }

    /// <summary>
    /// 交易：选一张交出，换一张新牌；交满 2 次结束。不选时什么也不发生。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Trade()
    {
        // 交出的牌
        CardModel? given = (await CardSelectCmd.FromDeckForRemoval(Owner!,
            new CardSelectorPrefs(Prompt("TRADE"), 1), CanTrade)).FirstOrDefault();
        if (given != null && OriginalCardDraw.Dealer(Owner!, Rng, given) is { } entry)
        {
            await CardPileCmd.RemoveFromDeck(given);
            await AddToDeck(OriginalCardDraw.Create(Owner!, entry));
            _traded++;
            CalculateVars();
        }
        if (_traded >= Trades)
        {
            Finish("DONE");
            return;
        }
        ShowPage(_traded == 0 ? InitialPage : "TRADED", DealerOptions(RepeatPage));
    }
}
