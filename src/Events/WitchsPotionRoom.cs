using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Events.Data;
using FullMoonKnight.Events.Panels;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 女巫药剂室（原作 5，CollegePanel）：三行，每行可以买多次——
/// 生命上限 +8/+16/+24（同时回复）、初始法力 +1/+2/+3、经验 +1（第 1/2/3 幕）。
/// 价格：原作第 1 章按付费次数 3,6,9…27 后 30（经验 3,6,9,12,15 后 30），第 2、3 章 ×2、×3，再 ×2.5；
/// 每个药剂室第一次购买免费（三行共用）；付费次数按行分开、整局共享。凋零乐章 +50%。
/// 其他角色没有法力和等级，这两行锁定。原作 CollegePanel、EventCollege.csv。
/// </summary>
public sealed class WitchsPotionRoom : KnightEvent
{
    // ===== 常量 =====

    /// <summary>行：生命、法力、经验（与手账里的付费次数下标一致）。</summary>
    private static readonly string[] Rows = ["Health", "Mana", "Exp"];

    /// <summary>法力一行的下标。</summary>
    private const int ManaRow = 1;

    /// <summary>每行的原作图标。</summary>
    private static readonly string[] Icons = ["icon_shengming", "icon_mofa", "icon_jingyan"];

    /// <summary>价格序列的步长（原作 3）。</summary>
    private const int PriceStep = 3;

    /// <summary>价格序列封顶后的价格（原作 30）。</summary>
    private const int PriceCap = 30;

    /// <summary>生命、法力两行价格序列的长度（3…27）。</summary>
    private const int LongSeries = 9;

    /// <summary>经验一行价格序列的长度（3…15）。</summary>
    private const int ShortSeries = 5;

    /// <summary>每章生命上限（原作 2/4/6）。</summary>
    private static readonly int[] OriginalMaxHp = [2, 4, 6];

    /// <summary>每次经验。</summary>
    private const int XpGain = 1;

    // ===== 状态 =====

    /// <summary>本药剂室用过免费购买。</summary>
    private bool _freeUsed;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new MaxHpVar(0), new DynamicVar("Mana", 0), new DynamicVar("Xp", XpGain)];

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars.MaxHp.BaseValue = FullMoonNumbers.Hp(OriginalMaxHp[Chapter - 1]);
        DynamicVars["Mana"].BaseValue = Chapter;
    }

    // ===== 选项 =====

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return RoomOptions(InitialPage);
    }

    /// <summary>
    /// 三行购买选项（钱不够或这一行不可用时锁定）、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] RoomOptions(string page)
    {
        List<EventOption> options = [];
        for (int row = 0; row < Rows.Length; row++)
        {
            if (row == ManaRow)
            {
                // 法力一行暂不出现：女骑士没有用法力的牌
                continue;
            }
            int index = row;
            int price = PriceOf(row);
            bool usable = row == 0 || IsKnight;
            bool canBuy = usable && Owner!.Gold >= price;
            options.Add(ViewChoice(canBuy ? () => Buy(index) : null, page, Rows[row].ToUpperInvariant(),
                new OptionView { Sprite = Icons[row], Price = price }));
        }
        options.Add(LeaveOption());
        return options.ToArray();
    }

    /// <summary>
    /// 一行的价格。
    /// </summary>
    /// <param name="row">行。</param>
    /// <returns>金币；本药剂室第一次为 0。</returns>
    private int PriceOf(int row)
    {
        if (!_freeUsed)
        {
            return 0;
        }
        int paid = Ledger.CollegePaid(row);
        int series = row == 2 ? ShortSeries : LongSeries;
        int original = paid < series ? PriceStep * (paid + 1) : PriceCap;
        return Price(FullMoonNumbers.Gold(original * Chapter));
    }

    // ===== 购买 =====

    /// <summary>
    /// 买一行：付钱、结算效果、记付费次数。
    /// </summary>
    /// <param name="row">行。</param>
    /// <returns>异步任务。</returns>
    private async Task Buy(int row)
    {
        await Spend(PriceOf(row));
        if (_freeUsed)
        {
            Ledger.AddCollegePaid(row);
        }
        _freeUsed = true;
        switch (row)
        {
            case 0:
                await CreatureCmd.GainMaxHp(Owner!.Creature, DynamicVars.MaxHp.BaseValue);
                break;
            case 1:
                Ledger.FmkBaseMana += Chapter;
                break;
            default:
                if (Owner!.GetRelic<KnightOathBase>() is { } oath)
                {
                    await oath.GainXp(XpGain);
                }
                break;
        }
        ShowPage(InitialPage, RoomOptions(RepeatPage));
    }
}
