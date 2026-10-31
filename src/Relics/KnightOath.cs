using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;
using FullMoonKnight.Events;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 满月基类：女骑士的抽牌和保留手牌规则，以及等级成长（数值取自原作配置表）。
/// 原作手牌上限和每回合抽牌是同一个数：初始 2，4 级、7 级各 +1；回合结束不弃手牌，超出上限时由玩家选择保留哪些。
/// 等级：战斗胜利获得经验（普通 1、精英 2、首领 3），满级 11；每次升级从行动、生命、强化、清除里三选一。
/// 女骑士事件通过原生共享事件池出现，不额外替换问号房间的抽取结果。
/// 部分原版遗物按 3 能量、抽 5 张设计，在女骑士身上门槛几乎恒成立，这里按女骑士的节奏改数值（见 AdaptVanillaRelics）。
/// </summary>
public abstract class KnightOathBase : KnightRelic
{
    // ===== 常量 =====

    /// <summary>原生每回合基础抽牌数。</summary>
    private const int VanillaHandDraw = 5;

    /// <summary>1 级的每回合抽牌数（原作 MaxCards = 2）。</summary>
    private const int BaseDraw = 2;

    /// <summary>到这些等级时抽牌数和手牌上限各 +1（原作 CardsGrow）。</summary>
    private static readonly int[] DrawGrowLevels = [4, 7];

    /// <summary>每回合抽牌数变量名。</summary>
    public const string TurnDrawVar = "TurnDraw";

    /// <summary>保留上限变量名。</summary>
    public const string KeepVar = "Keep";

    /// <summary>等级变量名。</summary>
    public const string LevelVar = "Level";

    /// <summary>当前经验变量名。</summary>
    public const string XpVar = "Xp";

    /// <summary>升到下一级所需的累计经验变量名。</summary>
    public const string NextXpVar = "NextXp";

    /// <summary>行动成长最多选几次（能量上限 1 → 2；遗物另算，总上限 EnergyCap）。</summary>
    public const int MaxBonusEnergy = 1;

    /// <summary>
    /// 升到第 2～11 级所需的累计经验；满级后不再升级。
    /// 原作累计 2/6/13/25/45/70/100/135/180/225，约第一章末 4 级、第二章末 7 级；
    /// 按幕分段等比换算到女骑士每幕约 13、15、10 点经验，最终首领前后满级。
    /// </summary>
    private static readonly int[] LevelXp = [2, 5, 10, 15, 20, 26, 30, 32, 35, 38];

    /// <summary>每次升级给出的选项数。</summary>
    private const int OptionCount = 3;

    /// <summary>回合开始抽到诅咒时每回合最多补抽几次。</summary>
    public const int CurseRedrawsPerTurn = 2;

    /// <summary>行动成长只在总能量上限低于这个值时出现（原版遗物给的能量也算在内）。</summary>
    public const int EnergyCap = 3;

    // ===== 成长状态（存档保存） =====

    /// <summary>累计经验。</summary>
    private int xp;

    /// <summary>等级（从 1 开始）。</summary>
    private int level = 1;

    /// <summary>行动成长：额外能量上限。</summary>
    private int bonusEnergy;

    /// <summary>旧版抽牌成长：每回合额外抽牌数（新局不再出现，只为读旧存档）。</summary>
    private int bonusDraw;

    /// <summary>本回合因抽到诅咒补抽的次数（战斗内状态）。</summary>
    private int curseRedraws;

    /// <summary>战斗胜利升了级、还没在奖励里选成长的次数。</summary>
    private int pendingLevelUps;

    // ===== 数值 =====

    /// <summary>手牌上限比抽牌数多出的张数。</summary>
    protected abstract int ExtraKeep { get; }

    /// <summary>当前每回合抽牌数（随等级成长）。</summary>
    public int TurnDraw => BaseDraw + DrawGrowLevels.Count(l => level >= l) + bonusDraw;

    /// <summary>回合结束最多保留的手牌数：等于抽牌数，再加额外上限。</summary>
    public int Keep => TurnDraw + ExtraKeep;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(TurnDrawVar, BaseDraw),
        new DynamicVar(KeepVar, BaseDraw + ExtraKeep),
        new DynamicVar(LevelVar, 1),
        new DynamicVar(XpVar, 0),
        new DynamicVar(NextXpVar, LevelXp[0]),
    ];

    /// <inheritdoc />
    public override bool ShowCounter => true;

    /// <inheritdoc />
    public override int DisplayAmount => level;

    /// <summary>当前等级。</summary>
    public int Level => level;

    /// <summary>行动成长次数。</summary>
    public int BonusEnergy => bonusEnergy;

    /// <summary>旧版抽牌成长次数。</summary>
    public int BonusDraw => bonusDraw;

    /// <summary>
    /// 战斗外估算的总能量上限：角色基础值加上所有遗物（含本誓言的行动成长）的加成。
    /// </summary>
    public int TotalMaxEnergy
    {
        get
        {
            // 每件遗物在 0 上加的能量（帕埃尔之肉要读战斗回合数，战斗外直接取它的能量值）
            decimal bonus = Owner.Relics.Sum(r => r is PaelsFlesh ? r.DynamicVars.Energy.BaseValue : r.ModifyMaxEnergy(Owner, 0m));
            return Owner.MaxEnergy + (int)bonus;
        }
    }

    /// <summary>累计经验（存档保存）。</summary>
    [SavedProperty]
    public int FmkKnightXp
    {
        get
        {
            return xp;
        }
        set
        {
            AssertMutable();
            xp = value;
            RefreshVars();
        }
    }

    /// <summary>等级（存档保存）。</summary>
    [SavedProperty]
    public int FmkKnightLevel
    {
        get
        {
            return level;
        }
        set
        {
            AssertMutable();
            level = Math.Max(1, value);
            RefreshVars();
        }
    }

    /// <summary>行动成长次数（存档保存）。</summary>
    [SavedProperty]
    public int FmkKnightBonusEnergy
    {
        get
        {
            return bonusEnergy;
        }
        set
        {
            AssertMutable();
            bonusEnergy = value;
        }
    }

    /// <summary>旧版抽牌成长次数（存档保存）。</summary>
    [SavedProperty]
    public int FmkKnightBonusDraw
    {
        get
        {
            return bonusDraw;
        }
        set
        {
            AssertMutable();
            bonusDraw = value;
            RefreshVars();
        }
    }

    /// <summary>待选成长次数（存档保存：在奖励界面读档时奖励按它重新生成）。</summary>
    [SavedProperty]
    public int FmkKnightPendingLevelUps
    {
        get
        {
            return pendingLevelUps;
        }
        set
        {
            AssertMutable();
            pendingLevelUps = Math.Max(0, value);
        }
    }

    // ===== 成长 =====

    /// <summary>
    /// 行动成长：能量上限 +1。
    /// </summary>
    public void AddBonusEnergy()
    {
        FmkKnightBonusEnergy = bonusEnergy + 1;
    }

    /// <summary>
    /// 把成长状态复制给另一份满月（Orobas 把满月换成皓月时用）。
    /// </summary>
    /// <param name="target">新的满月。</param>
    public void CopyProgressTo(KnightOathBase target)
    {
        target.FmkKnightXp = xp;
        target.FmkKnightLevel = level;
        target.FmkKnightBonusEnergy = bonusEnergy;
        target.FmkKnightBonusDraw = bonusDraw;
    }

    /// <summary>
    /// 获得经验；够了就升级（一次可能连升几级）。
    /// </summary>
    /// <param name="amount">经验。</param>
    /// <param name="viaRewards">为 true 时成长不当场选，而是作为战斗奖励出现（见 <see cref="LevelUpReward" />）。</param>
    /// <returns>异步任务。</returns>
    public async Task GainXp(int amount, bool viaRewards = false)
    {
        FmkKnightXp = xp + amount;
        while (level - 1 < LevelXp.Length && xp >= LevelXp[level - 1])
        {
            FmkKnightLevel = level + 1;
            Flash();
            InvokeDisplayAmountChanged();
            if (viaRewards)
            {
                FmkKnightPendingLevelUps = pendingLevelUps + 1;
            }
            else
            {
                await ResolveLevelUp(RollLevelUpOptions());
            }
        }
    }

    /// <summary>
    /// 失去经验（腐烂苹果）：最多降到当前等级的起点，不降级。
    /// </summary>
    /// <param name="amount">经验。</param>
    public void LoseXp(int amount)
    {
        int floor = level >= 2 ? LevelXp[Math.Min(level, LevelXp.Length + 1) - 2] : 0;
        FmkKnightXp = Math.Max(floor, xp - amount);
    }

    /// <summary>
    /// 按权重抽出三个可用的成长选项。
    /// </summary>
    /// <returns>选项牌。</returns>
    public List<CardModel> RollLevelUpOptions()
    {
        // 可用选项（规范模型）
        List<LevelUpOption> pool = ModelDb.AllCards.OfType<LevelUpOption>().Where(o => o.IsAvailable(this)).ToList();
        // 抽选用的随机数（每名玩家各自一份，联机同步）
        Rng rng = Owner.PlayerRng.Rewards;
        // 抽中的选项牌
        List<CardModel> options = [];
        while (options.Count < OptionCount && pool.Count > 0)
        {
            // 权重掷点
            int roll = rng.NextInt(pool.Sum(o => o.Weight));
            // 掷中的选项
            LevelUpOption picked = pool.First(o => (roll -= o.Weight) < 0);
            pool.Remove(picked);
            options.Add(Owner.RunState.CreateCard(picked, Owner));
        }
        return options;
    }

    /// <summary>
    /// 升级：让玩家从选项里选 1 个成长并结算；有待选次数时扣掉一次。
    /// </summary>
    /// <param name="options">选项牌。</param>
    /// <returns>选完（或没有选项）时为 true。</returns>
    public async Task<bool> ResolveLevelUp(IReadOnlyList<CardModel> options)
    {
        if (options.Count > 0)
        {
            if (await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), options, Owner)
                is not LevelUpOption chosen)
            {
                return false;
            }
            await chosen.Apply(this);
        }
        DropPendingLevelUp();
        return true;
    }

    /// <summary>
    /// 扣掉一次待选成长（选完或跳过奖励时）。
    /// </summary>
    public void DropPendingLevelUp()
    {
        FmkKnightPendingLevelUps = pendingLevelUps - 1;
    }

    /// <summary>
    /// 更新描述里的等级、经验、抽牌数和手牌上限。
    /// </summary>
    private void RefreshVars()
    {
        DynamicVars[LevelVar].BaseValue = level;
        DynamicVars[XpVar].BaseValue = xp;
        DynamicVars[NextXpVar].BaseValue = level - 1 < LevelXp.Length ? LevelXp[level - 1] : xp;
        DynamicVars[TurnDrawVar].BaseValue = TurnDraw;
        DynamicVars[KeepVar].BaseValue = Keep;
    }

    // ===== 钩子 =====

    /// <summary>
    /// 回合开始抽手牌之前：先把弃牌堆并回抽牌堆并洗匀（打出的牌、弃掉的牌回到牌库），
    /// 再锁住本回合的自动洗牌，于是这一回合抽牌堆抽空后就抽不到牌。
    /// 第 1 回合弃牌堆是空的，开局洗牌刚做过，不再洗（保留黑暗乐章这类开局排序）。
    /// 并回只有在累计抽过整副牌时才算一次洗牌，见 <see cref="KnightDrawCycle" />。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner)
        {
            return;
        }
        if (player.PlayerCombatState?.TurnNumber == 1)
        {
            KnightDrawCycle.Reset(player);
            // 上一场的成长奖励没领就离开了房间（奖励已经没了）
            FmkKnightPendingLevelUps = 0;
        }
        KnightDrawCycle.Unlock(player);
        curseRedraws = 0;
        if (PileType.Discard.GetPile(player).Cards.Count > 0)
        {
            // 战斗中的牌库张数
            int deckSize = PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count
                + PileType.Hand.GetPile(player).Cards.Count;
            KnightDrawCycle.BeginTurnShuffle(player, deckSize);
            try
            {
                await CardPileCmd.Shuffle(choiceContext, player);
            }
            finally
            {
                KnightDrawCycle.EndTurnShuffle(player);
            }
        }
        KnightDrawCycle.Lock(player);
    }

    /// <summary>
    /// 记录抽到的牌（决定回合开始的并回什么时候算一次洗牌）；
    /// 回合开始抽到诅咒时补抽 1 张（每回合最多 <see cref="CurseRedrawsPerTurn" /> 次）：每回合只抽 2 张，
    /// 一张诅咒就是少一半手牌，比其他角色痛得多。
    /// </summary>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="card">抽到的牌。</param>
    /// <param name="fromHandDraw">是否是回合开始的抽牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner)
        {
            return;
        }
        KnightDrawCycle.CountDraw(Owner);
        if (fromHandDraw && card.Type == CardType.Curse && curseRedraws < CurseRedrawsPerTurn)
        {
            curseRedraws++;
            Flash();
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }
    }

    /// <summary>
    /// 战斗结束：解开洗牌锁、清零抽牌计数（不让锁留到战斗外）。
    /// </summary>
    /// <param name="room">战斗房间。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        KnightDrawCycle.Reset(Owner);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 把原生 5 张抽牌换成本规则的抽牌数；以差值修改，保留其他效果的加减，结果不低于 0。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="count">当前抽牌数。</param>
    /// <returns>修改后的抽牌数。</returns>
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner)
        {
            return count;
        }
        return Math.Max(0m, count - VanillaHandDraw + TurnDraw);
    }

    /// <summary>
    /// 行动成长增加能量上限。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="amount">当前能量上限。</param>
    /// <returns>修改后的能量上限。</returns>
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + bonusEnergy : amount;
    }

    /// <summary>
    /// 战斗胜利后获得经验（普通 1、精英 2、首领 3）；最后一幕的首领战之后整局结束，不再升级。
    /// 升级的成长放进这场战斗的奖励里选（见 <see cref="TryModifyRewards" />），不在这里弹出选择。
    /// </summary>
    /// <param name="room">战斗房间。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead
            || (room.RoomType == RoomType.Boss && Owner.RunState.CurrentActIndex >= Owner.RunState.Acts.Count - 1))
        {
            return;
        }
        // 本场经验
        int amount = room.RoomType switch
        {
            RoomType.Elite => 2,
            RoomType.Boss => 3,
            _ => 1,
        };
        await GainXp(amount, viaRewards: true);
    }

    /// <summary>
    /// 战斗奖励里加上待选的成长，每次升级一项。
    /// </summary>
    /// <param name="player">领奖励的玩家。</param>
    /// <param name="rewards">奖励列表。</param>
    /// <param name="room">房间。</param>
    /// <returns>加了奖励时为 true。</returns>
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom || pendingLevelUps <= 0)
        {
            return false;
        }
        for (int i = 0; i < pendingLevelUps; i++)
        {
            rewards.Add(new LevelUpReward(this, Owner));
        }
        return true;
    }

    /// <summary>
    /// 回合结束清手牌前：手牌不超过上限就全部保留，超过时让玩家选出要保留的牌。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="player">结束回合的玩家。</param>
    /// <returns>异步任务。</returns>
    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || player.Creature.CombatState is not { } combatState
            || !Hook.ShouldFlush(combatState, player))
        {
            return;
        }

        // 还没有保留标记的手牌
        List<CardModel> candidates = PileType.Hand.GetPile(player).Cards.Where(RetainFilter).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        // 要保留的牌
        IEnumerable<CardModel> kept = candidates;
        if (candidates.Count > Keep)
        {
            CardSelectorPrefs prefs = new(SelectionScreenPrompt, Keep, Keep);
            kept = await CardSelectCmd.FromHand(choiceContext, player, prefs, RetainFilter, this);
        }
        foreach (CardModel card in kept)
        {
            card.GiveSingleTurnRetain();
        }
    }

    /// <summary>
    /// 只选择本回合尚未保留的牌；诅咒、状态和不能打出的牌照常弃掉，不占保留名额。
    /// 否则灼伤、悔恨这类“回合结束在手牌中”就生效的牌会被自动留下，每回合都触发。
    /// </summary>
    /// <param name="card">手牌。</param>
    /// <returns>可以选择时为 true。</returns>
    private static bool RetainFilter(CardModel card)
    {
        return !card.ShouldRetainThisTurn && card.Type is not (CardType.Curse or CardType.Status)
            && !card.Keywords.Contains(CardKeyword.Unplayable);
    }

    // ===== 原版遗物适配 =====

    /// <summary>
    /// 进房间时按女骑士的节奏改写部分原版遗物的数值（描述跟着变）：
    /// 钻石冠冕（出牌不超过 2 张减伤）和怀表（出牌不超过 3 张多抽 3 张）的门槛在每回合只抽 2 张时几乎恒成立，
    /// 改为不超过 1 张，怀表多抽 2 张；帕埃尔之泪（剩能量下回合 +2）对 1 能量是 +200%，改为 +1；
    /// 尖叫酒壶（回合结束手牌为空时对所有敌人 20 点）在女骑士身上几乎每回合都触发，改为 10 点。
    /// 大蘑菇（首回合少抽 2 张）按抽 5 张设计，女骑士首回合基础只抽 2 张，改为少抽 1 张。
    /// 其余按机制适配的原版内容见 <see cref="Mechanics.VanillaAdaptations" />。
    /// </summary>
    /// <param name="room">进入的房间。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        AdaptVanillaRelics();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 改写持有的原版遗物数值（重复调用结果相同）。
    /// </summary>
    public void AdaptVanillaRelics()
    {
        foreach (RelicModel relic in Owner.Relics)
        {
            switch (relic)
            {
                // 测试版的钻石冠冕已重做为首回合格挡，不再有出牌门槛。
                case DiamondDiadem when relic.DynamicVars.ContainsKey("CardThreshold"):
                    relic.DynamicVars["CardThreshold"].BaseValue = 1m;
                    break;
                case Pocketwatch:
                    relic.DynamicVars["CardThreshold"].BaseValue = 1m;
                    relic.DynamicVars.Cards.BaseValue = 2m;
                    break;
                case PaelsTears:
                    relic.DynamicVars.Energy.BaseValue = 1m;
                    break;
                case ScreamingFlagon:
                    relic.DynamicVars.Damage.BaseValue = 10m;
                    break;
                case BigMushroom:
                    relic.DynamicVars.Cards.BaseValue = 1m;
                    break;
                // 女骑士 1 能量、攻击多为 0 费，后期一回合十几张攻击；原版 10 张给 1 能量等于每回合能量翻倍
                case Nunchaku:
                    relic.DynamicVars.Cards.BaseValue = 20m;
                    break;
                // 女骑士每回合只抽 2 张、牌多为 0 费，原版每 4 张抽 1 张会一路连抽下去
                case IronClub:
                    relic.DynamicVars.Cards.BaseValue = 6m;
                    break;
            }
        }
    }
}

/// <summary>
/// 满月：手牌上限等于每回合抽牌数（2，4 级、7 级各 +1）。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class KnightOath : KnightOathBase
{
    /// <inheritdoc />
    protected override int ExtraKeep => 0;

    /// <inheritdoc />
    public override RelicModel GetUpgradeReplacement()
    {
        return ModelDb.Relic<KnightOathPlus>();
    }
}

/// <summary>
/// 皓月（Orobas 升级）：手牌上限比抽牌数多 1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class KnightOathPlus : KnightOathBase
{
    /// <inheritdoc />
    protected override int ExtraKeep => 1;
}
