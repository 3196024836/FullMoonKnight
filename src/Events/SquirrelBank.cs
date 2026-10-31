using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Events.Panels;

namespace FullMoonKnight.Events;

/// <summary>
/// 松鼠银行（原作 42，SaveGoldPanel）：每次存 25（原作 10），存款上限 125（原作 50），每局最多存 10 次；取出全部免费。
/// 原作存款跨局、通关时 +10；模组存款只在本局，进入下一幕时 +25。第 2、3 幕各可能出现一次。
/// </summary>
public sealed class SquirrelBank : KnightEvent, IPanelInfo
{
    // ===== 常量 =====

    /// <summary>每次存入。</summary>
    private const int Deposit = 25;

    /// <summary>存款上限。</summary>
    private const int Cap = 125;

    /// <summary>每局最多存几次。</summary>
    private const int MaxDeposits = 10;

    /// <summary>每进入一幕的利息。</summary>
    private const int Interest = 25;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new GoldVar(Deposit), new DynamicVar("Savings", 0), new DynamicVar("Cap", Cap), new DynamicVar("Left", MaxDeposits)];

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars["Savings"].BaseValue = Ledger.Savings(Interest, Cap);
        DynamicVars["Left"].BaseValue = MaxDeposits - Ledger.FmkDeposits;
    }

    /// <inheritdoc />
    public string PanelInfo => $"{PanelText.Get("savings")} {Ledger.Savings(Interest, Cap)}/{Cap}";

    // ===== 选项 =====

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return BankOptions(InitialPage);
    }

    /// <summary>
    /// 存（钱不够、存满或次数用完时锁定）、取（没有存款时锁定）、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] BankOptions(string page)
    {
        int savings = Ledger.Savings(Interest, Cap);
        bool canSave = Owner!.Gold >= Deposit && savings + Deposit <= Cap && Ledger.FmkDeposits < MaxDeposits;
        return
        [
            ViewChoice(canSave ? Save : null, page, "SAVE", new OptionView { Sprite = "icon_coin", Price = Deposit }),
            ViewChoice(savings > 0 ? Withdraw : null, page, "GET", new OptionView { Price = 0 }),
            LeaveOption(),
        ];
    }

    /// <summary>
    /// 存入一次。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Save()
    {
        await Spend(Deposit);
        Ledger.FmkSavings = Ledger.Savings(Interest, Cap) + Deposit;
        Ledger.FmkDeposits++;
        CalculateVars();
        ShowPage("SAVED", BankOptions(RepeatPage));
    }

    /// <summary>
    /// 取出全部存款。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Withdraw()
    {
        int savings = Ledger.Savings(Interest, Cap);
        Ledger.FmkSavings = 0;
        await GainGold(savings);
        CalculateVars();
        ShowPage("WITHDRAWN", BankOptions(RepeatPage));
    }
}
