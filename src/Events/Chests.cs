using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

using FullMoonKnight.Cards;
using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 一张卡的宝箱（原作 ChestPanel）：打开看到 1 张卡，拾取或离开。子类决定卡从哪来。
/// </summary>
public abstract class CardChestBase : KnightEvent
{
    /// <summary>宝箱里的原作卡（固定卡时为空）。</summary>
    protected OriginalCard? Entry { get; set; }

    /// <summary>宝箱里的牌。</summary>
    protected CardModel? Card { get; set; }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        Fill();
        return ChestOptions(InitialPage);
    }

    /// <summary>
    /// 装宝箱：设置 <see cref="Card" />。
    /// </summary>
    protected abstract void Fill();

    /// <summary>
    /// 从宝箱池抽一张本幕等级的卡装进宝箱（原作 GetChestCardId.GetChestCard）。
    /// </summary>
    /// <param name="minTier">最低等级。</param>
    /// <param name="maxTier">最高等级。</param>
    /// <param name="exclude">不要的卡。</param>
    protected void FillFromPool(int minTier, int maxTier, IEnumerable<OriginalCard>? exclude = null)
    {
        Entry = OriginalCardDraw.Chest(Owner!, Rng, minTier, maxTier, exclude);
        Card = Entry == null ? null : OriginalCardDraw.Create(Owner!, Entry);
    }

    /// <summary>
    /// 拾取（有卡时）、额外选项、离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    protected EventOption[] ChestOptions(string page)
    {
        List<EventOption> options = [];
        if (Card != null)
        {
            options.Add(CardOption(Card, Take, page, "TAKE"));
        }
        options.AddRange(ExtraOptions(page));
        options.Add(LeaveOption());
        return options.ToArray();
    }

    /// <summary>
    /// 额外选项（谦虚的宝箱的更换）。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    protected virtual IEnumerable<EventOption> ExtraOptions(string page)
    {
        return [];
    }

    /// <summary>
    /// 拾取：把牌加入牌组，事件结束。
    /// </summary>
    /// <returns>异步任务。</returns>
    protected virtual async Task Take()
    {
        await AddToDeck(Card!);
        Finish("TAKE");
    }
}

/// <summary>
/// 害羞的宝箱（原作 7）：1 张本幕等级的卡（第 1/2/3 章 1–5、2–7、3–10），宝箱权重。
/// 每幕第一个害羞的宝箱可能换成贪婪或谦虚的宝箱（<see cref="FullMoonEventDeck" />）。
/// </summary>
public sealed class ShyChest : CardChestBase
{
    /// <inheritdoc />
    protected override void Fill()
    {
        (int min, int max) = Tiers(Owner!);
        FillFromPool(min, max);
    }
}

/// <summary>
/// 谦虚的宝箱（原作 24）：同害羞的宝箱，可以免费更换 1 次（换成不同的卡）。
/// </summary>
public sealed class ModestChest : CardChestBase
{
    /// <summary>更换选项名。</summary>
    public const string RefreshName = "REFRESH";

    /// <summary>已经更换过。</summary>
    private bool _refreshed;

    /// <inheritdoc />
    protected override void Fill()
    {
        (int min, int max) = Tiers(Owner!);
        FillFromPool(min, max);
    }

    /// <inheritdoc />
    protected override IEnumerable<EventOption> ExtraOptions(string page)
    {
        return _refreshed ? [] : [Choice(Refresh, page, RefreshName)];
    }

    /// <summary>
    /// 更换：换成另一张不同的卡。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Refresh()
    {
        _refreshed = true;
        (int min, int max) = Tiers(Owner!);
        OriginalCard? old = Entry;
        FillFromPool(min, max, old == null ? null : [old]);
        ShowPage("REFRESHED", ChestOptions(RepeatPage));
        return Task.CompletedTask;
    }
}

/// <summary>
/// 稀有的宝箱（原作 38，RareChestPanel）：1 张等级 8 以上的卡。原作没有生成代码，模组第 2 幕低概率。
/// </summary>
public sealed class RareChest : CardChestBase
{
    /// <summary>最低等级。</summary>
    private const int MinTier = 8;

    /// <summary>最高等级。</summary>
    private const int MaxTier = 10;

    /// <inheritdoc />
    protected override void Fill()
    {
        FillFromPool(MinTier, MaxTier);
    }
}

/// <summary>
/// 害羞的宝箱（金币，原作 33）：一个红包，(2–9) × 章 金币，×2.5。
/// </summary>
public sealed class ShyGoldChest : CardChestBase
{
    /// <summary>红包金币的原作随机范围（含两端）。</summary>
    private const int MinGold = 2;

    /// <summary>红包金币的原作随机范围上限。</summary>
    private const int MaxGold = 9;

    /// <summary>红包金币。</summary>
    private int _gold;

    /// <inheritdoc />
    protected override string PortraitName => "shy_chest";

    /// <inheritdoc />
    protected override void Fill()
    {
        _gold = FullMoonNumbers.Gold((MinGold + Rng.NextInt(MaxGold - MinGold + 1)) * Chapter);
        Card = NewCard(ModelDb.Card<RedEnvelope>());
        Card.DynamicVars.Gold.BaseValue = _gold;
    }

    /// <inheritdoc />
    protected override async Task Take()
    {
        await GainGold(_gold);
        Finish("TAKE");
    }
}

/// <summary>
/// 害羞的宝箱（鞭炮，原作 52）：固定 1 张鞭炮，只在第 2 章。
/// </summary>
public sealed class ShyFirecrackerChest : CardChestBase
{
    /// <inheritdoc />
    protected override string PortraitName => "shy_chest";

    /// <inheritdoc />
    protected override void Fill()
    {
        Card = NewCard(ModelDb.Card<Firecrackers>());
    }
}

/// <summary>
/// 贪婪的宝箱（原作 25，GreedyChestPanel）：2 张不同的卡，两张都可以拿。只在第 1 章。
/// </summary>
public sealed class GreedyChest : KnightEvent
{
    /// <summary>卡数。</summary>
    private const int CardCount = 2;

    /// <summary>两张牌（拿走后为空）。</summary>
    private readonly CardModel?[] _cards = new CardModel?[CardCount];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        (int min, int max) = Tiers(Owner!);
        List<OriginalCard> picked = [];
        for (int i = 0; i < CardCount; i++)
        {
            if (OriginalCardDraw.Chest(Owner!, Rng, min, max, picked) is { } entry)
            {
                picked.Add(entry);
                _cards[i] = OriginalCardDraw.Create(Owner!, entry);
            }
        }
        return ChestOptions(InitialPage);
    }

    /// <summary>
    /// 每张还在的牌一个领取选项，加离开。
    /// </summary>
    /// <param name="page">页面。</param>
    /// <returns>选项。</returns>
    private EventOption[] ChestOptions(string page)
    {
        List<EventOption> options = [];
        for (int i = 0; i < CardCount; i++)
        {
            if (_cards[i] != null)
            {
                int slot = i;
                options.Add(CardOption(_cards[i]!, () => Take(slot), page, $"TAKE{i}", -1, i.ToString()));
            }
        }
        options.Add(LeaveOption());
        return options.ToArray();
    }

    /// <summary>
    /// 领取一张；两张都拿完时结束。
    /// </summary>
    /// <param name="slot">卡位。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(int slot)
    {
        await AddToDeck(_cards[slot]!);
        _cards[slot] = null;
        if (_cards.All(c => c == null))
        {
            Finish("TAKE");
            return;
        }
        ShowPage(InitialPage, ChestOptions(RepeatPage));
    }
}

/// <summary>
/// 可疑的宝箱（原作 26，地图事件卡）：打开就和宝箱怪战斗。只在第 2、3 章。
/// 模组：和本幕一场普通战斗，金币奖励翻倍（每名玩家多一份战斗金币）。联机时全员投票、一起战斗。
/// </summary>
public sealed class SuspiciousChest : KnightEvent
{
    /// <inheritdoc />
    public override bool IsShared => true;

    /// <inheritdoc />
    protected override string PortraitName => "shy_chest";

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Open, InitialPage, "OPEN"), LeaveOption()];
    }

    /// <summary>
    /// 打开：进入战斗，战斗结束后回到地图。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Open()
    {
        // 本幕普通战斗
        List<EncounterModel> encounters = Owner!.RunState.Act.AllRegularEncounters.ToList();
        EncounterModel encounter = encounters[Rng.NextInt(encounters.Count)];
        // 每名玩家多一份金币
        List<Reward> rewards = Owner.RunState.Players
            .Select(Reward (Player p) => new GoldReward(GoldReward.defaultMinGoldAmount, GoldReward.defaultMaxGoldAmount, p))
            .ToList();
        EnterKnightCombat(encounter, rewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}
