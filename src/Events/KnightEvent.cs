using System.Reflection;
using System.Runtime.CompilerServices;

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Events.Data;
using FullMoonKnight.Events.Panels;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 月圆之夜事件基类：只由 <see cref="FullMoonEventDeck" /> 抽出（原版抽取不会抽到），所有角色都能结算。
/// 文字键：标题 ID.title，页面 ID.pages.页面.description，选项 ID.pages.页面.options.选项.title/description。
/// 选项可以附带面板展示用的 <see cref="OptionView" />（卡、祝福、图标、价格、格子），<see cref="EventChoicePanel" /> 按它画。
/// </summary>
public abstract class KnightEvent : CustomEventModel
{
    /// <summary>新版同步器接收规范遭遇，旧版入口直接接收可变遭遇。</summary>
    private static readonly bool UsesCanonicalEncounter = typeof(EventModel)
        .GetField("_combatSynchronizer", BindingFlags.Instance | BindingFlags.NonPublic) != null;

    /// <summary>选项附带的面板展示信息。</summary>
    private static readonly ConditionalWeakTable<EventOption, OptionView> Views = new();

    // ===== 常量 =====

    /// <summary>所有事件共用的文字键前缀（离开、面板按钮）。</summary>
    public const string SharedKey = "FULLMOONKNIGHT-KNIGHT_EVENT";

    /// <summary>初始页面名。</summary>
    protected const string InitialPage = "INITIAL";

    /// <summary>重复出现的选项共用的页面名。</summary>
    protected const string RepeatPage = "ALL";

    /// <summary>离开选项名（面板据此找关闭按钮对应的选项）。</summary>
    public const string LeaveName = "LEAVE";

    // ===== 外观 =====

    /// <summary>插图文件名（assets/generated/events/ 下，不含扩展名）；默认按事件类名。</summary>
    protected virtual string PortraitName => KnightAssets.BaseName(Id.Entry);

    /// <inheritdoc />
    public override string CustomInitialPortraitPath => KnightAssets.Image($"events/{PortraitName}.png");

    /// <summary>
    /// 统一样式的选择面板，盖在原生事件页上。每次进入事件房间新建一个。
    /// </summary>
    /// <returns>面板。</returns>
    public EventChoicePanel CreatePanel()
    {
        return new EventChoicePanel();
    }

    // ===== 出现条件 =====

    /// <inheritdoc />
    public override bool IsAllowed(IRunState runState)
    {
        return FullMoonEventDeck.Drawing && CanAppear(runState);
    }

    /// <summary>
    /// 额外的出现条件（链条、持有物品等）。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>可以出现时为 true。</returns>
    protected virtual bool CanAppear(IRunState runState)
    {
        return true;
    }

    // ===== 常用状态 =====

    /// <summary>当前章（第 1/2/3 幕；更多幕按第 3 章）。</summary>
    protected int Chapter => FullMoonNumbers.Chapter(Owner!.RunState);

    /// <summary>玩家的月圆手账。</summary>
    protected FullMoonLedger Ledger => FullMoonLedger.Of(Owner!);

    /// <summary>玩家是不是女骑士。</summary>
    protected bool IsKnight => Owner!.Character is Knight;

    // ===== 选项 =====

    /// <inheritdoc />
    protected sealed override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return InitialOptions();
    }

    /// <summary>
    /// 初始页面的选项。
    /// </summary>
    /// <returns>选项列表。</returns>
    protected abstract IReadOnlyList<EventOption> InitialOptions();

    /// <summary>
    /// 新建一个选项，文字取 ID.pages.<paramref name="page" />.options.<paramref name="name" />。
    /// </summary>
    /// <param name="onChosen">选中后执行；为空时选项锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="name">选项名。</param>
    /// <param name="tips">悬停提示。</param>
    /// <returns>选项。</returns>
    protected EventOption Choice(Func<Task>? onChosen, string page, string name, params IHoverTip[] tips)
    {
        return new EventOption(this, onChosen, $"{Id.Entry}.pages.{page}.options.{name}", tips);
    }

    /// <summary>
    /// 新建一个选项，悬停提示为一组（例如遗物和它引用的关键词）。
    /// </summary>
    /// <param name="onChosen">选中后执行；为空时选项锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="name">选项名。</param>
    /// <param name="tips">悬停提示。</param>
    /// <returns>选项。</returns>
    protected EventOption Choice(Func<Task>? onChosen, string page, string name, IEnumerable<IHoverTip> tips)
    {
        return new EventOption(this, onChosen, $"{Id.Entry}.pages.{page}.options.{name}", tips);
    }

    /// <summary>
    /// 离开选项（所有事件共用文字）。
    /// </summary>
    /// <returns>选项。</returns>
    protected EventOption LeaveOption()
    {
        return new EventOption(this, Leave, new LocString(LocTable, $"{SharedKey}.leave.title"),
            new LocString(LocTable, $"{SharedKey}.leave.description"), $"{Id.Entry}.pages.ALL.options.{LeaveName}", []);
    }

    /// <summary>
    /// 是否离开选项。
    /// </summary>
    /// <param name="option">选项。</param>
    /// <returns>是离开时为 true。</returns>
    public static bool IsLeave(EventOption option)
    {
        return option.TextKey.EndsWith("." + LeaveName, StringComparison.Ordinal);
    }

    /// <summary>
    /// 离开：事件结束。
    /// </summary>
    /// <returns>异步任务。</returns>
    protected Task Leave()
    {
        SetEventFinished(new LocString(LocTable, $"{SharedKey}.left.description"));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 给选项附上面板展示信息。
    /// </summary>
    /// <param name="option">选项。</param>
    /// <param name="view">展示信息。</param>
    /// <returns>同一个选项。</returns>
    protected static EventOption View(EventOption option, OptionView view)
    {
        Views.AddOrUpdate(option, view);
        return option;
    }

    /// <summary>
    /// 选项附带的面板展示信息。
    /// </summary>
    /// <param name="option">选项。</param>
    /// <returns>展示信息；没有时为空。</returns>
    public static OptionView? ViewOf(EventOption option)
    {
        return Views.TryGetValue(option, out OptionView? view) ? view : null;
    }

    /// <summary>
    /// 祝福选项：标题和说明用祝福自己的，面板按原作底框显示。
    /// </summary>
    /// <param name="originalId">祝福的原作编号。</param>
    /// <param name="onChosen">选中后执行（参数是这个祝福的实例）；为空时锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="slot">面板格子；为空时按顺序。</param>
    /// <returns>选项。</returns>
    protected EventOption BlessingOption(int originalId, Func<RelicModel, Task>? onChosen, string page = InitialPage,
        string? slot = null)
    {
        RelicModel relic = ClassicBlessings.Create(originalId);
        relic.Owner = Owner!;
        EventOption option = EventOption.FromRelic(relic, this, onChosen == null ? null : () => onChosen(relic),
            $"{Id.Entry}.pages.{page}.options.{relic.Id.Entry}");
        return View(option, new OptionView { Relic = relic, Frame = ClassicBlessings.FrameIndex(originalId), Slot = slot });
    }

    /// <summary>
    /// 带面板展示信息的普通选项（价格、格子）。
    /// </summary>
    /// <param name="onChosen">选中后执行；为空时锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="name">选项名。</param>
    /// <param name="view">展示信息。</param>
    /// <returns>选项。</returns>
    protected EventOption ViewChoice(Func<Task>? onChosen, string page, string name, OptionView view)
    {
        return View(Choice(onChosen, page, name), view);
    }

    /// <summary>
    /// 遗物选项（限次祝福、荆棘代价、负面乐章等不在祝福登记表里的遗物）：标题和说明用遗物自己的。
    /// </summary>
    /// <param name="relic">遗物实例（可直接获得）。</param>
    /// <param name="onChosen">选中后执行；为空时锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="frame">原作底框；-1 保持默认。</param>
    /// <returns>选项。</returns>
    protected EventOption RelicOption(RelicModel relic, Func<Task>? onChosen, string page = InitialPage, int frame = -1)
    {
        relic.Owner = Owner!;
        EventOption option = EventOption.FromRelic(relic, this, onChosen,
            $"{Id.Entry}.pages.{page}.options.{relic.Id.Entry}");
        return View(option, new OptionView { Relic = relic, Frame = frame });
    }

    /// <summary>
    /// 卡牌选项：悬停显示卡牌，面板画出卡牌和价格。
    /// </summary>
    /// <param name="card">卡牌（属于玩家、还没有牌堆）。</param>
    /// <param name="onChosen">选中后执行；为空时锁定。</param>
    /// <param name="page">所在页面。</param>
    /// <param name="name">选项名（文字键）。</param>
    /// <param name="price">价格；-1 表示不显示。</param>
    /// <param name="slot">面板格子；为空时按顺序。</param>
    /// <returns>选项。</returns>
    protected EventOption CardOption(CardModel card, Func<Task>? onChosen, string page, string name, int price = -1,
        string? slot = null)
    {
        EventOption option = new(this, onChosen, CardTitle(card, page, name), CardDescription(card, page, name, price),
            $"{Id.Entry}.pages.{page}.options.{name}", [HoverTipFactory.FromCard(card)]);
        return View(option, new OptionView { Card = card, Price = price, Slot = slot });
    }

    /// <summary>
    /// 卡牌选项的标题：选项有自己的文字时用它，否则用卡名。
    /// </summary>
    /// <param name="card">卡牌。</param>
    /// <param name="page">页面。</param>
    /// <param name="name">选项名。</param>
    /// <returns>标题。</returns>
    private LocString CardTitle(CardModel card, string page, string name)
    {
        return GetOptionTitle($"{Id.Entry}.pages.{page}.options.{name}") ?? card.TitleLocString;
    }

    /// <summary>
    /// 卡牌选项的说明：选项有自己的文字时用它（价格填进 {Price}），否则用通用的"获得卡牌"文字。
    /// </summary>
    /// <param name="card">卡牌。</param>
    /// <param name="page">页面。</param>
    /// <param name="name">选项名。</param>
    /// <param name="price">价格。</param>
    /// <returns>说明。</returns>
    private LocString CardDescription(CardModel card, string page, string name, int price)
    {
        LocString text = GetOptionDescription($"{Id.Entry}.pages.{page}.options.{name}")
            ?? new LocString(LocTable, price > 0 ? $"{SharedKey}.buy_card.description" : $"{SharedKey}.take_card.description");
        text.Add("Card", card.TitleLocString);
        text.Add("Price", price);
        return text;
    }

    // ===== 页面 =====

    /// <summary>
    /// 页面描述文字，变量从本事件的动态变量填入。
    /// </summary>
    /// <param name="page">页面名。</param>
    /// <returns>文字。</returns>
    protected LocString Page(string page)
    {
        // 页面文字
        LocString text = L10NLookup($"{Id.Entry}.pages.{page}.description");
        DynamicVars.AddTo(text);
        return text;
    }

    /// <summary>
    /// 结束事件并显示页面文字。
    /// </summary>
    /// <param name="page">页面名。</param>
    protected void Finish(string page)
    {
        SetEventFinished(Page(page));
    }

    /// <summary>
    /// 切换到新页面。
    /// </summary>
    /// <param name="page">页面名。</param>
    /// <param name="options">新页面的选项。</param>
    protected void ShowPage(string page, params EventOption[] options)
    {
        SetEventState(Page(page), options);
    }

    /// <summary>
    /// 选牌界面的提示文字：ID.pages.<paramref name="page" />.selectionScreenPrompt。
    /// </summary>
    /// <param name="page">页面名。</param>
    /// <returns>文字。</returns>
    protected LocString Prompt(string page)
    {
        return L10NLookup($"{Id.Entry}.pages.{page}.selectionScreenPrompt");
    }

    // ===== 战斗 =====

    /// <summary>按当前分支进入事件战斗，保留额外奖励与返回事件的规则。</summary>
    /// <param name="canonicalEncounter">遭遇（图鉴实例）。</param>
    /// <param name="rewards">额外奖励。</param>
    /// <param name="shouldResumeAfterCombat">战斗后是否回到事件。</param>
    protected void EnterKnightCombat(EncounterModel canonicalEncounter, IReadOnlyList<Reward> rewards,
        bool shouldResumeAfterCombat)
    {
        EnterCombatWithoutExitingEvent(UsesCanonicalEncounter ? canonicalEncounter : canonicalEncounter.ToMutable(),
            rewards, shouldResumeAfterCombat);
    }

    // ===== 奖励与代价 =====

    /// <summary>
    /// 生成一张属于玩家、本局有效的牌（还没有牌堆）。
    /// </summary>
    /// <param name="canonical">图鉴实例。</param>
    /// <returns>新牌。</returns>
    protected CardModel NewCard(CardModel canonical)
    {
        return Owner!.RunState.CreateCard(canonical, Owner);
    }

    /// <summary>
    /// 把牌加入牌组并播放加入动画。
    /// </summary>
    /// <param name="card">新牌。</param>
    /// <returns>异步任务。</returns>
    protected async Task AddToDeck(CardModel card)
    {
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }

    /// <summary>
    /// 失去生命（无视格挡与能力）；最多失去到剩 1 点时用 <paramref name="keepOne" />。
    /// </summary>
    /// <param name="amount">数值。</param>
    /// <param name="keepOne">至少保留 1 点生命。</param>
    /// <returns>异步任务。</returns>
    protected Task LoseHp(decimal amount, bool keepOne = false)
    {
        if (keepOne)
        {
            amount = Math.Min(amount, Owner!.Creature.CurrentHp - 1);
        }
        if (amount <= 0)
        {
            return Task.CompletedTask;
        }
        return KnightDamage.Deal(new ThrowingPlayerChoiceContext(), Owner!.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    /// <summary>
    /// 失去生命上限。
    /// </summary>
    /// <param name="amount">数值。</param>
    /// <returns>异步任务。</returns>
    protected Task LoseMaxHp(decimal amount)
    {
        return CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, amount, isFromCard: false);
    }

    /// <summary>
    /// 事件里的花费：带凋零乐章时增加 50%。
    /// </summary>
    /// <param name="baseCost">原价。</param>
    /// <returns>实际价格。</returns>
    protected int Price(int baseCost)
    {
        return CurseWitherMovement.EventPrice(Owner!, baseCost);
    }

    /// <summary>
    /// 获得金币。
    /// </summary>
    /// <param name="amount">金币。</param>
    /// <returns>异步任务。</returns>
    protected Task GainGold(int amount)
    {
        return amount <= 0 ? Task.CompletedTask : PlayerCmd.GainGold(amount, Owner!);
    }

    /// <summary>
    /// 花费金币。
    /// </summary>
    /// <param name="amount">金币。</param>
    /// <returns>异步任务。</returns>
    protected Task Spend(int amount)
    {
        return amount <= 0 ? Task.CompletedTask : PlayerCmd.LoseGold(amount, Owner!, GoldLossType.Spent);
    }

    /// <summary>
    /// 是否持有荒芜乐章（泉水、绷带会降低生命上限）。
    /// </summary>
    protected bool HasBarren => Owner!.GetRelic<CurseBarrenMovement>() != null;

    /// <summary>
    /// 获得祝福（遗物）。
    /// </summary>
    /// <param name="relic">祝福实例。</param>
    /// <returns>异步任务。</returns>
    protected async Task Obtain(RelicModel relic)
    {
        await RelicCmd.Obtain(relic, Owner!);
    }

    /// <summary>
    /// 本幕的卡牌等级范围下限。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>等级范围。</returns>
    protected static (int Min, int Max) Tiers(Player player)
    {
        return OriginalCardDraw.TierRange(player);
    }
}
