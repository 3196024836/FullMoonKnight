using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Cards;
using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 可疑的痕迹（原作 41/44，地图事件卡）：原作是进入第 4 章的入口。模组：持有外婆的拐杖时第 3 幕出现，
/// 调查 → 交出拐杖，和本幕一个精英战斗，胜利后额外拿 1 个 9 级祝福；或者离开。联机时全员投票、一起战斗。
/// </summary>
public sealed class SuspiciousTraces : KnightEvent
{
    /// <summary>奖励祝福等级。</summary>
    private const int RewardLevel = 9;

    /// <inheritdoc />
    public override bool IsShared => true;

    /// <inheritdoc />
    protected override bool CanAppear(IRunState runState)
    {
        return runState.Players.Any(HasCrutch);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Investigate, InitialPage, "INVESTIGATE"), LeaveOption()];
    }

    /// <summary>
    /// 玩家牌组里有没有外婆的拐杖。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>有时为 true。</returns>
    private static bool HasCrutch(Player player)
    {
        return player.Deck.Cards.Any(c => c is GrandmasCrutch);
    }

    /// <summary>
    /// 调查：交出拐杖，进入精英战斗，战斗结束后回到地图。拿着拐杖的玩家额外获得 9 级祝福。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Investigate()
    {
        // 每名拿着拐杖的玩家的祝福
        List<Reward> rewards = [];
        foreach (Player player in Owner!.RunState.Players.Where(HasCrutch))
        {
            int id = ClassicBlessings.Draw(player, Rng, RewardLevel, RewardLevel, false, 1)[0];
            RelicModel relic = ClassicBlessings.Create(id);
            rewards.Add(new RelicReward(relic, player));
        }
        if (Owner.Deck.Cards.FirstOrDefault(c => c is GrandmasCrutch) is { } crutch)
        {
            await CardPileCmd.RemoveFromDeck(crutch);
        }
        // 本幕精英
        List<EncounterModel> elites = Owner.RunState.Act.AllEliteEncounters.ToList();
        EnterKnightCombat(elites[Rng.NextInt(elites.Count)], rewards, shouldResumeAfterCombat: false);
    }
}
