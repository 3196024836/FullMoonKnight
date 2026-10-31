using System.Linq;

using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Relics;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 满月的回合抽牌循环：带着誓言的玩家在本回合抽牌时，不把弃牌堆洗回抽牌堆。
/// 抽牌堆抽空后这一回合就抽不到牌；打出的牌在下一个回合开始时由 <see cref="KnightDrawCycle" /> 并回牌堆。
/// </summary>
[HarmonyPatch]
public static class KnightShufflePatch
{
    /// <summary>
    /// 锁住洗牌时跳过原方法，弃牌堆保持不动。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>继续执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.ShuffleIfNecessary))]
    [HarmonyPrefix]
    private static bool Prefix(Player player, ref Task __result)
    {
        if (!player.Relics.OfType<KnightOathBase>().Any() || !KnightDrawCycle.ReshuffleLocked(player))
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }
}
