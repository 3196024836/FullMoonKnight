using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Panels;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 梦魇音乐盒（原作 43，MusicBoxPanel）：播放乐章（第一次必是黑暗乐章，之后随机，可以连着播）、播放所有乐章、关闭音乐盒。
/// 乐章是负面祝福，只增加难度。原作梦魇第 1 章首领后出现；模组第 1 幕低概率。
/// </summary>
public sealed class MusicBox : KnightEvent
{
    /// <summary>第一首乐章：黑暗乐章。</summary>
    private static RelicModel FirstMovement => ModelDb.Relic<CurseDarkMovement>();

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return BoxOptions(InitialPage);
    }

    /// <summary>
    /// 还没有的乐章；黑暗乐章没有时排在最前。
    /// </summary>
    /// <returns>乐章（图鉴实例）。</returns>
    private List<RelicModel> Remaining()
    {
        return KnightEventRelics.Movements.Where(m => Owner!.GetRelicById(m.Id) == null)
            .OrderBy(m => m.Id == FirstMovement.Id ? 0 : 1)
            .ToList();
    }

    /// <summary>
    /// 播放乐章、播放所有乐章（还有乐章时）、关闭。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] BoxOptions(string page)
    {
        List<RelicModel> remaining = Remaining();
        if (remaining.Count == 0)
        {
            return [LeaveOption()];
        }
        RelicModel next = remaining[0];
        return
        [
            ViewChoice(PlayOne, page, "PLAY", new OptionView { Relic = next }),
            ViewChoice(PlayAll, page, "PLAY_ALL", new OptionView { Relic = next }),
            LeaveOption(),
        ];
    }

    /// <summary>
    /// 播放一首：第一首是黑暗乐章，之后随机。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task PlayOne()
    {
        List<RelicModel> remaining = Remaining();
        RelicModel movement = remaining[0].Id == FirstMovement.Id ? remaining[0] : remaining[Rng.NextInt(remaining.Count)];
        await Obtain(movement.ToMutable());
        ShowPage("PLAYED", BoxOptions(RepeatPage));
    }

    /// <summary>
    /// 播放所有乐章。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task PlayAll()
    {
        foreach (RelicModel movement in Remaining())
        {
            await Obtain(movement.ToMutable());
        }
        Finish("ALL");
    }
}
