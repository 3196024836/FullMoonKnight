using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Events.Data;
using FullMoonKnight.Events.Panels;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 时光精灵（原作 29）：花 2 粉尘"拾取"揭开第 1 个祝福，之后每次"许愿"再花 2 粉尘揭开下一个（最多 3 个），
/// 从揭开的祝福里选 1。祝福 6–9 级、非负面，第一次打开时生成。拾取前可以"再想想"离开；揭开后必须选。
/// 粉尘：原作是账号资源，模组改成本局资源，每打败一个首领 +3，上限 6（<see cref="FullMoonLedger.Dust" />）。
/// 原作 LampPanel、GetLostThingId.GetLampId。面板标题下显示剩余粉尘。
/// </summary>
public sealed class TimeGenie : KnightEvent, IPanelInfo
{
    // ===== 常量 =====

    /// <summary>每次揭开花费的粉尘。</summary>
    private const int DustCost = 2;

    /// <summary>祝福数。</summary>
    private const int BlessingCount = 3;

    /// <summary>祝福最低等级。</summary>
    private const int MinLevel = 6;

    /// <summary>祝福最高等级。</summary>
    private const int MaxLevel = 9;

    /// <summary>粉尘贴图。</summary>
    private const string DustIcon = "fengcheng__";

    // ===== 状态 =====

    /// <summary>三个祝福（原作编号；拾取前为空）。</summary>
    private List<int> _blessings = [];

    /// <summary>已揭开的个数。</summary>
    private int _revealed;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Dust", DustCost)];

    /// <inheritdoc />
    public string PanelInfo => $"{PanelText.Get("dust")} {Ledger.Dust()}/{FullMoonLedger.DustCap}";

    /// <inheritdoc />
    protected override bool CanAppear(IRunState runState)
    {
        // 还没有手账的玩家按"每幕 +3"算
        int fresh = Math.Min(FullMoonLedger.DustCap, FullMoonLedger.DustPerBoss * runState.CurrentActIndex);
        return runState.Players.Any(p => (FullMoonLedger.Peek(p)?.Dust() ?? fresh) >= DustCost);
    }

    // ===== 选项 =====

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        bool canPick = Ledger.Dust() >= DustCost;
        return
        [
            ViewChoice(canPick ? Pick : null, InitialPage, "PICK",
                new OptionView { Sprite = DustIcon, Price = DustCost, PriceIcon = DustIcon }),
            LeaveOption(),
        ];
    }

    /// <summary>
    /// 祝福窗口：揭开的格是祝福，下一格是许愿（粉尘不够时锁定）。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] LampOptions(string page)
    {
        List<EventOption> options = [];
        for (int i = 0; i < _revealed; i++)
        {
            options.Add(BlessingOption(_blessings[i], Take, page, i.ToString()));
        }
        if (_revealed < BlessingCount)
        {
            bool canWish = Ledger.Dust() >= DustCost;
            options.Add(ViewChoice(canWish ? Wish : null, page, "WISH",
                new OptionView { Sprite = DustIcon, Slot = _revealed.ToString(), Price = DustCost, PriceIcon = DustIcon }));
        }
        return options.ToArray();
    }

    // ===== 揭开与选择 =====

    /// <summary>
    /// 拾取：花粉尘，生成三个祝福，揭开第一个。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Pick()
    {
        Ledger.FmkDust = Ledger.Dust() - DustCost;
        _blessings = ClassicBlessings.Draw(Owner!, Rng, MinLevel, MaxLevel, false, BlessingCount);
        _revealed = 1;
        ShowPage("WISH", LampOptions(RepeatPage));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 许愿：花粉尘，再揭开一个。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Wish()
    {
        Ledger.FmkDust = Ledger.Dust() - DustCost;
        _revealed++;
        ShowPage("WISH", LampOptions(RepeatPage));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 选定一个祝福。
    /// </summary>
    /// <param name="relic">祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }
}
