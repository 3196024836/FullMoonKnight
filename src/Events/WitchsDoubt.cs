using MegaCrit.Sts2.Core.Events;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 女巫的疑惑（原作 14，地图事件卡）：寻找苹果 → 本幕加入 3 个遗失的苹果（种类从 8 种里不重复抽，吃之前不知道），
/// 奖励（1 个 9 级祝福）生成时就定好；无视 → 离开。原作 DealWithWitchsDoubt。
/// </summary>
public sealed class WitchsDoubt : KnightEvent
{
    /// <summary>苹果数。</summary>
    private const int AppleCount = 3;

    /// <summary>苹果种类（原作编号 1001～1008；法力类 1005、1006 暂不出现：女骑士没有用法力的牌）。</summary>
    private static readonly int[] AppleKinds = [1001, 1002, 1003, 1004, 1007, 1008];

    /// <summary>奖励祝福等级。</summary>
    private const int RewardLevel = 9;

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Search, InitialPage, "SEARCH"), LeaveOption()];
    }

    /// <summary>
    /// 寻找苹果：本幕放入苹果，定好奖励。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Search()
    {
        List<int> kinds = AppleKinds.ToList();
        List<int> picked = [];
        while (picked.Count < AppleCount)
        {
            int index = Rng.NextInt(kinds.Count);
            picked.Add(kinds[index]);
            kinds.RemoveAt(index);
        }
        int reward = ClassicBlessings.Draw(Owner!, Rng, RewardLevel, RewardLevel, false, 1)[0];
        Ledger.StartApples(Owner!.RunState.CurrentActIndex, picked.ToArray(), reward);
        Finish("SEARCH");
        return Task.CompletedTask;
    }
}
