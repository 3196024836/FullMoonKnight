using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Panels;

namespace FullMoonKnight.Events;

/// <summary>
/// 铁匠铺（原作 9，PigPanel）：升级牌，次数不限。每个铁匠铺第一次免费，之后 25 + 25 × 整局付费升级次数
/// （原作 10 + 10 × 次数，×2.5）。凋零乐章 +50%。
/// </summary>
public sealed class Smithy : KnightEvent
{
    /// <summary>基础价格和每次涨价（原作 10）。</summary>
    private const int PriceStep = 25;

    /// <summary>本铁匠铺用过免费升级。</summary>
    private bool _freeUsed;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(0)];

    /// <summary>下一次的价格。</summary>
    private int UpgradePrice => _freeUsed ? Price(PriceStep + PriceStep * Ledger.FmkSmithyPaid) : 0;

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = UpgradePrice;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return SmithyOptions(InitialPage);
    }

    /// <summary>
    /// 打造（钱不够或没有可升级的牌时锁定）、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] SmithyOptions(string page)
    {
        int price = UpgradePrice;
        bool canUpgrade = Owner!.Gold >= price && Owner.Deck.Cards.Any(c => c.IsUpgradable);
        return
        [
            ViewChoice(canUpgrade ? Upgrade : null, page, _freeUsed ? "UPGRADE" : "UPGRADE_FREE",
                new OptionView { Price = price }),
            LeaveOption(),
        ];
    }

    /// <summary>
    /// 打造：选 1 张牌升级并付钱；不选时不花钱。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Upgrade()
    {
        // 选中的牌
        CardModel? card = (await CardSelectCmd.FromDeckForUpgrade(Owner!,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            await Spend(UpgradePrice);
            CardCmd.Upgrade(card);
            if (_freeUsed)
            {
                Ledger.FmkSmithyPaid++;
            }
            _freeUsed = true;
            CalculateVars();
        }
        ShowPage(InitialPage, SmithyOptions(RepeatPage));
    }
}
