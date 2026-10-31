using HarmonyLib;

using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Events.Data;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 月圆之夜事件牌库：开启时接管问号房间的事件抽取（ActModel.PullNextEvent 前缀）。
/// 1. 本幕还没遇到的必出事件优先（按原作遭遇权重抽；离首领越近越优先）。
/// 2. 否则 35% 从模组事件牌库按原作权重抽，65% 交给原版。
/// 牌库状态不另存：本幕遇到过什么从地图历史读，随机数由种子、幕数和本幕事件序号决定，读档后结果一样。
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
public static class FullMoonEventDeck
{
    // ===== 常量 =====

    /// <summary>必出事件之外，从模组牌库抽的概率（%）。</summary>
    private const int ModDeckPercent = 35;

    /// <summary>本幕已遇到同一事件时的权重系数（原作 repeatKey）。</summary>
    private const double RepeatFactor = 0.2;

    /// <summary>每剩几行地图就预期遇到一个问号（安排必出事件用）。</summary>
    private const double RowsPerEvent = 4;

    /// <summary>必出事件的最低优先概率。</summary>
    private const double MustMinChance = 0.6;

    /// <summary>每幕"金币多"的阈值（原作 20/40/80 × 2.5）。</summary>
    private static readonly int[] RichGold = [50, 100, 200];

    /// <summary>每幕第一个害羞的宝箱换成谦虚的宝箱的概率（原作 33%/50%/100%）。</summary>
    private static readonly double[] ModestChance = [0.33, 0.5, 1.0];

    // ===== 牌库 =====

    /// <summary>
    /// 牌库的一项。
    /// </summary>
    /// <param name="Event">事件（图鉴实例）。</param>
    /// <param name="Caps">每幕上限（第 1/2/3 幕）。</param>
    /// <param name="Weights">每幕遭遇权重。</param>
    /// <param name="Unique">整局只出现一次。</param>
    /// <param name="Must">必出的幕（1 起），空为不必出。</param>
    /// <param name="Factor">额外权重系数（生命、金币）；空为 1。</param>
    private sealed record Entry(Func<EventModel> Event, int[] Caps, int[] Weights, bool Unique, int[] Must,
        Func<IRunState, double>? Factor = null);

    /// <summary>全部事件。</summary>
    private static readonly IReadOnlyList<Entry> Deck =
    [
        new(() => ModelDb.Event<OldCatShop>(), [3, 5, 6], [4, 4, 4], false, [], s => RichFactor(s, [2, 4, 5])),
        new(() => ModelDb.Event<HealthSpring>(), [0, 2, 2], [2, 4, 4], false, [], s => HpFactor(s, 5, 0.1)),
        new(() => ModelDb.Event<Bandage>(), [1, 2, 2], [4, 20, 22], false, [], s => HpFactor(s, 2.5, 0.4)),
        new(() => ModelDb.Event<CardCollector>(), [0, 1, 0], [1, 1, 1], true, [2]),
        new(() => ModelDb.Event<WitchsPotionRoom>(), [0, 1, 1], [4, 4, 4], false, [2, 3]),
        new(() => ModelDb.Event<SelflessTavern>(), [1, 1, 1], [4, 4, 4], false, [1, 2, 3]),
        new(() => ModelDb.Event<ShyChest>(), [3, 3, 3], [8, 4, 4], false, []),
        new(() => ModelDb.Event<ShyGoldChest>(), [1, 1, 1], [8, 4, 4], false, []),
        new(() => ModelDb.Event<ShyFirecrackerChest>(), [0, 1, 0], [8, 4, 4], false, []),
        new(() => ModelDb.Event<SuspiciousChest>(), [0, 1, 1], [2, 2, 2], false, []),
        new(() => ModelDb.Event<RareChest>(), [0, 1, 0], [1, 1, 1], true, []),
        new(() => ModelDb.Event<Smithy>(), [1, 1, 1], [6, 80, 6], false, []),
        new(() => ModelDb.Event<FairyBlessing>(), [1, 1, 1], [600, 600, 600], false, [1, 2, 3]),
        new(() => ModelDb.Event<PandorasBox>(), [0, 1, 0], [1, 10, 4], true, [2]),
        new(() => ModelDb.Event<WitchsDoubt>(), [0, 1, 1], [1, 4, 4], true, []),
        new(() => ModelDb.Event<DemonicTrade>(), [0, 1, 0], [1, 1, 1], true, []),
        new(() => ModelDb.Event<ShortBlessing>(), [1, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<PixieWhisper>(), [1, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<LittleRock>(), [1, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<TimeGenie>(), [0, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<SantasDoubt>(), [0, 1, 0], [4, 4, 4], true, [2]),
        new(() => ModelDb.Event<GrandmasAdvice>(), [0, 0, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<SealedMemory>(), [0, 1, 0], [1, 1, 1], true, []),
        new(() => ModelDb.Event<BattleTraces>(), [0, 0, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<SuspiciousTraces>(), [0, 0, 1], [20, 20, 20], true, []),
        new(() => ModelDb.Event<ThornBush>(), [0, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<SquirrelBank>(), [0, 1, 1], [4, 50, 6], false, []),
        new(() => ModelDb.Event<MusicBox>(), [1, 0, 0], [1, 1, 1], true, []),
        new(() => ModelDb.Event<LostItems>(), [0, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<CatsCuriosityShop>(), [0, 1, 1], [1, 1, 1], true, []),
        new(() => ModelDb.Event<LostApple>(), [0, 3, 3], [4, 4, 4], false, []),
        new(() => ModelDb.Event<LostGift>(), [0, 3, 0], [4, 4, 4], false, []),
    ];

    /// <summary>正在从牌库抽（KnightEvent.IsAllowed 只在这时放行，原版抽取不会抽到模组事件）。</summary>
    [ThreadStatic]
    private static bool _drawing;

    /// <summary>模组事件此刻能否被选中。</summary>
    public static bool Drawing => _drawing;

    // ===== 抽取 =====

    /// <summary>
    /// 问号房间抽事件：开启时按月圆之夜牌库抽，交给原版时返回 true。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="__result">抽到的事件。</param>
    /// <returns>是否继续执行原版抽取。</returns>
    [HarmonyPrefix]
    public static bool Prefix(RunState runState, ref EventModel __result)
    {
        if (!FullMoonConfig.EventsEnabled(runState))
        {
            return true;
        }
        EventModel? picked = Draw(runState);
        if (picked == null)
        {
            return true;
        }
        runState.AddVisitedEvent(picked);
        __result = picked;
        return false;
    }

    /// <summary>
    /// 按规则抽一个模组事件；交给原版时为空。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>事件（图鉴实例）。</returns>
    public static EventModel? Draw(IRunState runState)
    {
        int act = runState.CurrentActIndex;
        int chapter = FullMoonNumbers.Chapter(runState);
        // 本幕遇到过的事件
        List<ModelId> seen = SeenThisAct(runState);
        // 用字符串种子派生随机数：Rng.Seed 和 Rng 构造函数在测试版里从 uint 改成了 ulong，直接引用会在测试版找不到方法
        Rng rng = new RunRngSet($"{runState.Rng.StringSeed}_fmk_events_{act}_{seen.Count}").Niche;
        _drawing = true;
        try
        {
            // 链条里必须马上出现的（吃完最后一个苹果后的女巫的奖赏）
            EventModel? forced = Forced(runState);
            if (forced != null)
            {
                return forced;
            }

            // 必出
            List<Entry> must = Deck.Where(e => e.Must.Contains(chapter) && !seen.Contains(e.Event().Id)
                && Available(runState, e, seen, chapter)).ToList();
            if (must.Count > 0 && rng.NextDouble() < MustChance(runState, must.Count))
            {
                return Pick(must, runState, seen, chapter, rng);
            }

            // 模组牌库或原版
            if (rng.NextInt(100) >= ModDeckPercent)
            {
                return null;
            }
            List<Entry> pool = Deck.Where(e => Available(runState, e, seen, chapter)).ToList();
            return pool.Count == 0 ? null : SpecialChest(Pick(pool, runState, seen, chapter, rng), runState, seen, chapter, rng);
        }
        finally
        {
            _drawing = false;
        }
    }

    /// <summary>
    /// 本幕已经进过的事件房间（地图历史）。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>事件编号（可重复）。</returns>
    public static List<ModelId> SeenThisAct(IRunState runState)
    {
        int act = runState.CurrentActIndex;
        if (runState.MapPointHistory.Count <= act)
        {
            return [];
        }
        return runState.MapPointHistory[act]
            .SelectMany(p => p.Rooms)
            .Where(r => r.RoomType == RoomType.Event && r.ModelId != null)
            .Select(r => r.ModelId!)
            .ToList();
    }

    /// <summary>
    /// 每幕第一个害羞的宝箱按原作换成贪婪或谦虚的宝箱（原作 GenerateSpecialChest）：
    /// 第 1 幕先换贪婪的宝箱（整局一次），否则按 33%/50%/100% 换谦虚的宝箱（整局一次）。
    /// </summary>
    /// <param name="picked">抽到的事件。</param>
    /// <param name="runState">本局。</param>
    /// <param name="seen">本幕遇到过的事件。</param>
    /// <param name="chapter">章。</param>
    /// <param name="rng">随机数。</param>
    /// <returns>最终的事件。</returns>
    private static EventModel SpecialChest(EventModel picked, IRunState runState, List<ModelId> seen, int chapter, Rng rng)
    {
        ModelId[] chests = [ModelDb.Event<ShyChest>().Id, ModelDb.Event<ModestChest>().Id, ModelDb.Event<GreedyChest>().Id];
        if (picked is not ShyChest || seen.Any(chests.Contains))
        {
            return picked;
        }
        EventModel greedy = ModelDb.Event<GreedyChest>();
        if (chapter == 1 && !Visited(runState, greedy.Id) && greedy.IsAllowed(runState))
        {
            return greedy;
        }
        EventModel modest = ModelDb.Event<ModestChest>();
        if (!Visited(runState, modest.Id) && rng.NextDouble() < ModestChance[chapter - 1]
            && modest.IsAllowed(runState))
        {
            return modest;
        }
        return picked;
    }

    /// <summary>
    /// 必须马上出现的链条事件。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>事件；没有时为空。</returns>
    private static EventModel? Forced(IRunState runState)
    {
        EventModel reward = ModelDb.Event<WitchsRewards>();
        bool due = runState.Players.Any(p => FullMoonLedger.Peek(p)?.AppleRewardDue(runState.CurrentActIndex) == true);
        return due && reward.IsAllowed(runState) ? reward : null;
    }

    /// <summary>
    /// 这一项现在能不能出现：本幕上限、整局一次、事件自己的条件。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="entry">牌库项。</param>
    /// <param name="seen">本幕遇到过的事件。</param>
    /// <param name="chapter">章。</param>
    /// <returns>能出现时为 true。</returns>
    private static bool Available(IRunState runState, Entry entry, List<ModelId> seen, int chapter)
    {
        EventModel model = entry.Event();
        int count = seen.Count(id => id == model.Id);
        if (count >= entry.Caps[chapter - 1] || entry.Weights[chapter - 1] <= 0)
        {
            return false;
        }
        if (entry.Unique && Visited(runState, model.Id))
        {
            return false;
        }
        return model.IsAllowed(runState);
    }

    /// <summary>
    /// 按权重抽一项。
    /// </summary>
    /// <param name="candidates">候选（非空）。</param>
    /// <param name="runState">本局。</param>
    /// <param name="seen">本幕遇到过的事件。</param>
    /// <param name="chapter">章。</param>
    /// <param name="rng">随机数。</param>
    /// <returns>事件。</returns>
    private static EventModel Pick(List<Entry> candidates, IRunState runState, List<ModelId> seen, int chapter, Rng rng)
    {
        // 每项权重
        List<double> weights = candidates.Select(e =>
        {
            double weight = e.Weights[chapter - 1] * (e.Factor?.Invoke(runState) ?? 1);
            return seen.Contains(e.Event().Id) ? weight * RepeatFactor : weight;
        }).ToList();
        double roll = rng.NextDouble() * weights.Sum();
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return candidates[i].Event();
            }
        }
        return candidates[^1].Event();
    }

    /// <summary>
    /// 必出事件优先的概率：剩余行数少、必出事件多时更高。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="pending">还没出现的必出事件数。</param>
    /// <returns>概率。</returns>
    private static double MustChance(IRunState runState, int pending)
    {
        int row = runState.CurrentMapCoord?.row ?? 0;
        int bossRow = runState.Map is NullActMap ? row + 1 : runState.Map.BossMapPoint.coord.row;
        double expected = Math.Max(1, (bossRow - row) / RowsPerEvent);
        return Math.Clamp(pending / expected, MustMinChance, 1);
    }

    /// <summary>
    /// 生命系数：有玩家生命低于一半乘 <paramref name="low" />，全部玩家生命不低于 75% 乘 <paramref name="high" />。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="low">低生命系数。</param>
    /// <param name="high">高生命系数。</param>
    /// <returns>系数。</returns>
    private static double HpFactor(IRunState runState, double low, double high)
    {
        if (runState.Players.Any(p => p.Creature.CurrentHp * 2 < p.Creature.MaxHp))
        {
            return low;
        }
        return runState.Players.All(p => p.Creature.CurrentHp * 4 >= p.Creature.MaxHp * 3) ? high : 1;
    }

    /// <summary>
    /// 金币系数：有玩家金币达到本幕阈值时乘对应系数。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="factors">每幕系数。</param>
    /// <returns>系数。</returns>
    private static double RichFactor(IRunState runState, int[] factors)
    {
        int chapter = FullMoonNumbers.Chapter(runState);
        return runState.Players.Any(p => p.Gold >= RichGold[chapter - 1]) ? factors[chapter - 1] : 1;
    }

    /// <summary>
    /// 本局是否已经遇到过某个事件。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <param name="id">事件编号。</param>
    /// <returns>遇到过时为 true。</returns>
    private static bool Visited(IRunState runState, ModelId id)
    {
        return runState is RunState state && state.VisitedEventIds.Contains(id);
    }

    /// <summary>
    /// 本局本幕某个事件遇到过几次。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    /// <param name="runState">本局。</param>
    /// <returns>次数。</returns>
    public static int SeenCount<T>(IRunState runState) where T : EventModel
    {
        ModelId id = ModelDb.Event<T>().Id;
        return SeenThisAct(runState).Count(x => x == id);
    }
}
