using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Panels;

namespace FullMoonKnight.Events;

/// <summary>
/// 忘忧酒馆（原作 6，PubPanel）：删牌，次数不限。每个酒馆第一次免费，之后 50 + 25 × 整局付费删牌次数
/// （原作 20 + 10 × 次数，×2.5）。凋零乐章 +50%。原作 CommFunc.GetDeleteCardPrice。
/// </summary>
public sealed class SelflessTavern : KnightEvent
{
    /// <summary>基础价格（原作 20）。</summary>
    private const int BasePrice = 50;

    /// <summary>每次付费后涨价（原作 10）。</summary>
    private const int PriceStep = 25;

    /// <summary>本酒馆用过免费删牌。</summary>
    private bool _freeUsed;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(0)];

    /// <summary>下一次的价格。</summary>
    private int ForgetPrice => _freeUsed ? Price(BasePrice + PriceStep * Ledger.FmkPubPaid) : 0;

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = ForgetPrice;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return TavernOptions(InitialPage);
    }

    /// <summary>
    /// 删除（钱不够或没有可删的牌时锁定）、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] TavernOptions(string page)
    {
        int price = ForgetPrice;
        bool canForget = Owner!.Gold >= price && Owner.Deck.Cards.Any(c => c.IsRemovable);
        return
        [
            ViewChoice(canForget ? Forget : null, page, _freeUsed ? "FORGET" : "FORGET_FREE",
                new OptionView { Price = price }),
            LeaveOption(),
        ];
    }

    /// <summary>
    /// 删除：选 1 张牌删除并付钱；不选时不花钱。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Forget()
    {
        // 选中的牌
        CardModel? card = (await CardSelectCmd.FromDeckForRemoval(Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            await Spend(ForgetPrice);
            await CardPileCmd.RemoveFromDeck(card);
            if (_freeUsed)
            {
                Ledger.FmkPubPaid++;
            }
            _freeUsed = true;
            CalculateVars();
        }
        ShowPage(InitialPage, TavernOptions(RepeatPage));
    }
}
