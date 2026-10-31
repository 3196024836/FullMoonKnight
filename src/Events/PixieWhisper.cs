using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace FullMoonKnight.Events;

/// <summary>
/// 妖精低语（原作 27，ElfWhisperPanel）：着魔（复制 1 张牌，生命上限 −18）、冥想（复制 1 张牌，能量上限 −1）、
/// 疯狂（复制 1 张牌，1 张恶作剧），选 1 或离开。原作只在新手引导出现。祝福数值照原作 1:1。
/// </summary>
public sealed class PixieWhisper : KnightEvent
{
    /// <summary>三个低语（原作编号）。</summary>
    private static readonly int[] Whispers = [1088, 1089, 1090];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        List<EventOption> options = Whispers.Select(id => BlessingOption(id, Take)).ToList();
        options.Add(LeaveOption());
        return options;
    }

    /// <summary>
    /// 获得选中的低语（拾取时复制牌并结算代价）。
    /// </summary>
    /// <param name="relic">祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }
}
