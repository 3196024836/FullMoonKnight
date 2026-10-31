using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 女巫的奖赏（原作 17，AppleRewardPanel）：吃完本幕最后一个苹果后必出，领取女巫的疑惑时定好的 9 级祝福。必须领取。
/// 联机时没有苹果任务的玩家领一个小钱堆。
/// </summary>
public sealed class WitchsRewards : KnightEvent
{

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        int reward = Ledger.AppleRewardDue(Owner!.RunState.CurrentActIndex) ? Ledger.ClaimAppleReward() : 0;
        return [BlessingOption(reward == 0 ? ClassicBlessings.FallbackId : reward, Take)];
    }

    /// <summary>
    /// 领取祝福。
    /// </summary>
    /// <param name="relic">祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }
}
