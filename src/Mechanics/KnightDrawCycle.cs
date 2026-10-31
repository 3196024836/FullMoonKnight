using System.Runtime.CompilerServices;

using MegaCrit.Sts2.Core.Entities.Players;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 满月的回合抽牌循环：回合内抽牌不会把弃牌堆洗回抽牌堆（抽牌堆空了就抽不到），
/// 打出的牌、弃掉的牌在下个回合开始时先并回抽牌堆并洗匀，然后再抽起手牌。
/// 回合开始的并回每回合都会发生；只有累计抽过的牌达到整副牌的张数时才算一次"洗牌"，
/// 原版"洗牌时"的效果（算盘、大大的拥抱、谋略、完美契合）按这个节奏触发，和其他角色抽完牌堆再洗的频率相当。
/// </summary>
public static class KnightDrawCycle
{
    /// <summary>本回合锁住洗牌的玩家。</summary>
    private static readonly HashSet<Player> Locked = [];

    /// <summary>每个玩家自上次算作洗牌以来抽过的牌数。</summary>
    private static readonly ConditionalWeakTable<Player, StrongBox<int>> Drawn = new();

    /// <summary>正在进行回合开始并回、且这次并回不算洗牌的玩家。</summary>
    private static readonly HashSet<Player> QuietShuffle = [];

    /// <summary>该玩家本回合是否禁止把弃牌堆洗回抽牌堆。</summary>
    /// <param name="player">玩家。</param>
    /// <returns>锁住时为 true。</returns>
    public static bool ReshuffleLocked(Player player)
    {
        return Locked.Contains(player);
    }

    /// <summary>锁住本回合的洗牌。</summary>
    /// <param name="player">玩家。</param>
    public static void Lock(Player player)
    {
        Locked.Add(player);
    }

    /// <summary>解除锁（回合开始时允许并回弃牌堆）。</summary>
    /// <param name="player">玩家。</param>
    public static void Unlock(Player player)
    {
        Locked.Remove(player);
    }

    /// <summary>记一张抽到的牌。</summary>
    /// <param name="player">玩家。</param>
    public static void CountDraw(Player player)
    {
        Drawn.GetOrCreateValue(player).Value++;
    }

    /// <summary>战斗开始或结束时清零计数并解锁。</summary>
    /// <param name="player">玩家。</param>
    public static void Reset(Player player)
    {
        Drawn.GetOrCreateValue(player).Value = 0;
        Locked.Remove(player);
        QuietShuffle.Remove(player);
    }

    /// <summary>
    /// 回合开始并回时调用：抽过的牌达到 <paramref name="deckSize" /> 张就算一次洗牌（计数清零），否则这次并回不算洗牌。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="deckSize">战斗中牌库的张数（抽牌堆、弃牌堆、手牌）。</param>
    /// <returns>算作洗牌时为 true。</returns>
    public static bool BeginTurnShuffle(Player player, int deckSize)
    {
        StrongBox<int> drawn = Drawn.GetOrCreateValue(player);
        if (drawn.Value >= Math.Max(1, deckSize))
        {
            drawn.Value = 0;
            return true;
        }
        QuietShuffle.Add(player);
        return false;
    }

    /// <summary>回合开始并回结束。</summary>
    /// <param name="player">玩家。</param>
    public static void EndTurnShuffle(Player player)
    {
        QuietShuffle.Remove(player);
    }

    /// <summary>该玩家当前的洗牌是否只是回合开始的并回（不触发"洗牌时"效果）。</summary>
    /// <param name="player">玩家。</param>
    /// <returns>不算洗牌时为 true。</returns>
    public static bool IsQuietShuffle(Player? player)
    {
        return player != null && QuietShuffle.Contains(player);
    }
}
