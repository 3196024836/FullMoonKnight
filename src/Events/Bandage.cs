using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 绷带（原作 3，地图事件卡）：可以用 2 次，每次回复 12/24/36（原作第 1/2/3 章 3/6/9 ×4），满血也算一次。
/// 带荒芜乐章时每次先失去 4 点生命上限（原作 1 点 ×4）。原作 DealWithBandage、EventBandage.txt。
/// </summary>
public sealed class Bandage : KnightEvent
{
    /// <summary>使用次数。</summary>
    private const int Uses = 2;

    /// <summary>每章每次回复（原作数值）。</summary>
    private static readonly int[] OriginalHeal = [3, 6, 9];

    /// <summary>荒芜乐章每次失去的生命上限（原作 1）。</summary>
    private static readonly int BarrenLoss = FullMoonNumbers.Hp(1);

    /// <summary>已经用了几次。</summary>
    private int _used;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(0), new MaxHpVar(BarrenLoss), new DynamicVar("Left", Uses)];

    /// <inheritdoc />
    public override void CalculateVars()
    {
        DynamicVars.Heal.BaseValue = FullMoonNumbers.Hp(OriginalHeal[Chapter - 1]);
        DynamicVars["Left"].BaseValue = Uses - _used;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return BandageOptions(InitialPage);
    }

    /// <summary>
    /// 使用、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] BandageOptions(string page)
    {
        return [Choice(Use, page, HasBarren ? "USE_BARREN" : "USE"), LeaveOption()];
    }

    /// <summary>
    /// 用一次绷带；用完 2 次结束。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Use()
    {
        if (HasBarren)
        {
            await LoseMaxHp(BarrenLoss);
        }
        await CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
        _used++;
        CalculateVars();
        if (_used >= Uses)
        {
            Finish("USED_UP");
            return;
        }
        ShowPage("USED", BandageOptions(RepeatPage));
    }
}
