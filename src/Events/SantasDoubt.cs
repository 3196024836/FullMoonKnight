using MegaCrit.Sts2.Core.Events;

namespace FullMoonKnight.Events;

/// <summary>
/// 圣诞老人的疑惑（原作 31，地图事件卡）：观察 → 本幕加入 3 个搞错的礼物（<see cref="LostGift" />）。只在第 2 幕。
/// 原作 EventDealWith.DealWithSantaClaus → StageInfo.GenerateWrongGift。
/// </summary>
public sealed class SantasDoubt : KnightEvent
{
    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Observe, InitialPage, "OBSERVE")];
    }

    /// <summary>
    /// 观察：本幕之后会出现搞错的礼物（牌库按本幕是否遇到过本事件判断）。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Observe()
    {
        Finish("OBSERVE");
        return Task.CompletedTask;
    }
}
