using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 月圆手账：记录一名玩家本局月圆之夜事件的状态（原作存在 roleInfo/UserInfo 上的那些数）。
/// 不在遗物栏显示（<see cref="FullMoonLedgerInventoryPatch" />）；第一次用到时静默加入，联机各端在同一处同步代码里加入。
/// 记录：已出现的祝福（原作 appearLostThing）、卡牌出现次数、药剂室/酒馆/铁匠铺付费次数、基础法力、粉尘、存款、苹果任务。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class FullMoonLedger : KnightRelic
{
    // ===== 常量 =====

    /// <summary>每打败一个首领得到的粉尘（原作 common 1601）。</summary>
    public const int DustPerBoss = 3;

    /// <summary>粉尘上限（原作 common 1602）。</summary>
    public const int DustCap = 6;

    // ===== 字段 =====

    /// <summary>已出现的祝福（原作编号）。</summary>
    private int[] appearedBlessings = [];

    /// <summary>卡牌出现次数：卡牌 ID 条目=次数，逗号分隔。</summary>
    private string cardAppear = "";

    /// <summary>药剂室三行的付费次数：生命、法力、经验。</summary>
    private int[] collegePaid = [0, 0, 0];

    /// <summary>酒馆付费删牌次数。</summary>
    private int pubPaid;

    /// <summary>铁匠铺付费升级次数。</summary>
    private int smithyPaid;

    /// <summary>基础法力（每场战斗开始时获得）。</summary>
    private int baseMana;

    /// <summary>粉尘（时光精灵）。</summary>
    private int dust;

    /// <summary>粉尘已结算到第几幕（0 起）。</summary>
    private int dustAct;

    /// <summary>松鼠银行存款。</summary>
    private int savings;

    /// <summary>存款已结算到第几幕。</summary>
    private int savingsAct;

    /// <summary>本局存钱次数。</summary>
    private int deposits;

    /// <summary>苹果任务所在的幕（-1 表示没有进行中的任务）。</summary>
    private int appleAct = -1;

    /// <summary>还没吃的苹果种类（原作编号 1001～1008）。</summary>
    private int[] apples = [];

    /// <summary>苹果任务的奖励祝福（原作编号；0 表示没有）。</summary>
    private int appleReward;

    // ===== 基础属性 =====

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool ShouldFlashOnPlayer => false;

    /// <summary>
    /// 取玩家的手账；没有时静默加入一个。必须在各端都会执行的同步代码里调用。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>手账。</returns>
    public static FullMoonLedger Of(Player player)
    {
        if (player.GetRelic<FullMoonLedger>() is { } ledger)
        {
            return ledger;
        }
        // 新手账
        FullMoonLedger created = (FullMoonLedger)ModelDb.Relic<FullMoonLedger>().ToMutable();
        player.AddRelicInternal(created, silent: true);
        return created;
    }

    /// <summary>
    /// 取玩家的手账，没有时不新建（只读判断用）。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>手账；没有时为空。</returns>
    public static FullMoonLedger? Peek(Player player)
    {
        return player.GetRelic<FullMoonLedger>();
    }

    // ===== 存档属性 =====

    /// <summary>已出现的祝福（存档）。</summary>
    [SavedProperty]
    public int[] FmkAppearedBlessings
    {
        get
        {
            return appearedBlessings;
        }
        set
        {
            AssertMutable();
            appearedBlessings = value ?? [];
        }
    }

    /// <summary>卡牌出现次数（存档）。</summary>
    [SavedProperty]
    public string FmkCardAppear
    {
        get
        {
            return cardAppear;
        }
        set
        {
            AssertMutable();
            cardAppear = value ?? "";
        }
    }

    /// <summary>药剂室付费次数（存档）。</summary>
    [SavedProperty]
    public int[] FmkCollegePaid
    {
        get
        {
            return collegePaid;
        }
        set
        {
            AssertMutable();
            collegePaid = value is { Length: 3 } ? value : [0, 0, 0];
        }
    }

    /// <summary>酒馆付费次数（存档）。</summary>
    [SavedProperty]
    public int FmkPubPaid
    {
        get
        {
            return pubPaid;
        }
        set
        {
            AssertMutable();
            pubPaid = value;
        }
    }

    /// <summary>铁匠铺付费次数（存档）。</summary>
    [SavedProperty]
    public int FmkSmithyPaid
    {
        get
        {
            return smithyPaid;
        }
        set
        {
            AssertMutable();
            smithyPaid = value;
        }
    }

    /// <summary>基础法力（存档）。</summary>
    [SavedProperty]
    public int FmkBaseMana
    {
        get
        {
            return baseMana;
        }
        set
        {
            AssertMutable();
            baseMana = Math.Max(0, value);
        }
    }

    /// <summary>粉尘（存档）。</summary>
    [SavedProperty]
    public int FmkDust
    {
        get
        {
            return dust;
        }
        set
        {
            AssertMutable();
            dust = Math.Clamp(value, 0, DustCap);
        }
    }

    /// <summary>粉尘结算到的幕（存档）。</summary>
    [SavedProperty]
    public int FmkDustAct
    {
        get
        {
            return dustAct;
        }
        set
        {
            AssertMutable();
            dustAct = value;
        }
    }

    /// <summary>存款（存档）。</summary>
    [SavedProperty]
    public int FmkSavings
    {
        get
        {
            return savings;
        }
        set
        {
            AssertMutable();
            savings = Math.Max(0, value);
        }
    }

    /// <summary>存款结算到的幕（存档）。</summary>
    [SavedProperty]
    public int FmkSavingsAct
    {
        get
        {
            return savingsAct;
        }
        set
        {
            AssertMutable();
            savingsAct = value;
        }
    }

    /// <summary>本局存钱次数（存档）。</summary>
    [SavedProperty]
    public int FmkDeposits
    {
        get
        {
            return deposits;
        }
        set
        {
            AssertMutable();
            deposits = value;
        }
    }

    /// <summary>苹果任务所在的幕（存档）。</summary>
    [SavedProperty]
    public int FmkAppleAct
    {
        get
        {
            return appleAct;
        }
        set
        {
            AssertMutable();
            appleAct = value;
        }
    }

    /// <summary>还没吃的苹果（存档）。</summary>
    [SavedProperty]
    public int[] FmkApples
    {
        get
        {
            return apples;
        }
        set
        {
            AssertMutable();
            apples = value ?? [];
        }
    }

    /// <summary>苹果任务奖励（存档）。</summary>
    [SavedProperty]
    public int FmkAppleReward
    {
        get
        {
            return appleReward;
        }
        set
        {
            AssertMutable();
            appleReward = value;
        }
    }

    // ===== 祝福 =====

    /// <summary>
    /// 祝福本局是否已经出现过。
    /// </summary>
    /// <param name="originalId">原作编号。</param>
    /// <returns>出现过时为 true。</returns>
    public bool HasAppeared(int originalId)
    {
        return appearedBlessings.Contains(originalId);
    }

    /// <summary>
    /// 记下祝福已出现。
    /// </summary>
    /// <param name="originalId">原作编号。</param>
    public void MarkAppeared(int originalId)
    {
        if (!HasAppeared(originalId))
        {
            FmkAppearedBlessings = [.. appearedBlessings, originalId];
        }
    }

    // ===== 卡牌出现次数 =====

    /// <summary>
    /// 卡牌本局在商店、宝箱、收藏家里出现过几次（同名卡合计，原作 GetCardAppearCount）。
    /// </summary>
    /// <param name="card">卡牌（图鉴实例）。</param>
    /// <returns>次数。</returns>
    public int AppearCount(CardModel card)
    {
        return ParseAppear().GetValueOrDefault(card.Id.Entry);
    }

    /// <summary>
    /// 卡牌出现次数 +1（原作 SetCardAppearCount，生成时就计数）。
    /// </summary>
    /// <param name="card">卡牌。</param>
    public void MarkCardAppeared(CardModel card)
    {
        Dictionary<string, int> counts = ParseAppear();
        counts[card.Id.Entry] = counts.GetValueOrDefault(card.Id.Entry) + 1;
        FmkCardAppear = string.Join(",", counts.Select(p => $"{p.Key}={p.Value}"));
    }

    /// <summary>
    /// 解析卡牌出现次数。
    /// </summary>
    /// <returns>卡牌 ID 条目 → 次数。</returns>
    private Dictionary<string, int> ParseAppear()
    {
        // 结果
        Dictionary<string, int> counts = [];
        foreach (string pair in cardAppear.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=');
            if (parts.Length == 2 && int.TryParse(parts[1], out int count))
            {
                counts[parts[0]] = count;
            }
        }
        return counts;
    }

    // ===== 付费次数 =====

    /// <summary>
    /// 药剂室某一行的整局付费次数。
    /// </summary>
    /// <param name="row">0 生命、1 法力、2 经验。</param>
    /// <returns>次数。</returns>
    public int CollegePaid(int row)
    {
        return collegePaid[row];
    }

    /// <summary>
    /// 药剂室某一行付费次数 +1。
    /// </summary>
    /// <param name="row">0 生命、1 法力、2 经验。</param>
    public void AddCollegePaid(int row)
    {
        int[] next = [.. collegePaid];
        next[row]++;
        FmkCollegePaid = next;
    }

    // ===== 粉尘与存款 =====

    /// <summary>
    /// 当前粉尘：每进入新的一幕（打败了上一幕首领）补 3，上限 6。
    /// </summary>
    /// <returns>粉尘。</returns>
    public int Dust()
    {
        int act = Owner.RunState.CurrentActIndex;
        if (act > dustAct)
        {
            FmkDust = dust + DustPerBoss * (act - dustAct);
            FmkDustAct = act;
        }
        return dust;
    }

    /// <summary>
    /// 当前存款：每进入新的一幕加一次利息（原作通关时存款 +10，上限 50）。
    /// </summary>
    /// <param name="interest">每幕利息。</param>
    /// <param name="cap">存款上限。</param>
    /// <returns>存款。</returns>
    public int Savings(int interest, int cap)
    {
        int act = Owner.RunState.CurrentActIndex;
        if (act > savingsAct)
        {
            if (savings > 0)
            {
                FmkSavings = Math.Min(cap, savings + interest * (act - savingsAct));
            }
            FmkSavingsAct = act;
        }
        return savings;
    }

    // ===== 苹果任务 =====

    /// <summary>
    /// 开始苹果任务（女巫的疑惑）：本幕放入若干个苹果，奖励祝福生成时就定好。
    /// </summary>
    /// <param name="act">幕（0 起）。</param>
    /// <param name="kinds">苹果种类（原作编号）。</param>
    /// <param name="reward">奖励祝福（原作编号）。</param>
    public void StartApples(int act, int[] kinds, int reward)
    {
        FmkAppleAct = act;
        FmkApples = kinds;
        FmkAppleReward = reward;
    }

    /// <summary>
    /// 本幕还没吃的苹果数（任务不在本幕时为 0，苹果只在本幕有效）。
    /// </summary>
    /// <param name="act">幕（0 起）。</param>
    /// <returns>个数。</returns>
    public int ApplesLeft(int act)
    {
        return appleAct == act ? apples.Length : 0;
    }

    /// <summary>
    /// 吃掉下一个苹果。
    /// </summary>
    /// <returns>苹果种类（原作编号）；没有时为 0。</returns>
    public int EatApple()
    {
        if (apples.Length == 0)
        {
            return 0;
        }
        int kind = apples[0];
        FmkApples = apples[1..];
        return kind;
    }

    /// <summary>
    /// 本幕苹果吃完了、奖励还没领（女巫的奖赏必出）。
    /// </summary>
    /// <param name="act">幕（0 起）。</param>
    /// <returns>该领奖励时为 true。</returns>
    public bool AppleRewardDue(int act)
    {
        return appleAct == act && apples.Length == 0 && appleReward != 0;
    }

    /// <summary>
    /// 领走苹果任务奖励。
    /// </summary>
    /// <returns>奖励祝福（原作编号）；没有时为 0。</returns>
    public int ClaimAppleReward()
    {
        int reward = appleReward;
        FmkAppleReward = 0;
        FmkAppleAct = -1;
        return reward;
    }

    // ===== 钩子 =====

    /// <summary>
    /// 战斗开始时获得基础法力。
    /// </summary>
    /// <returns>异步任务。</returns>
    public override Task BeforeCombatStart()
    {
        if (baseMana > 0)
        {
            KnightMana.Gain(Owner, baseMana);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// 月圆手账不进遗物栏：读档初始化和获得时都跳过它。
/// 之后获得的遗物按玩家遗物列表里的位置插入遗物栏，要减去排在前面的手账，否则位置超出遗物栏。
/// 例：遗物 [誓言, 手账]，获得第 3 个遗物时列表位置 2，遗物栏只有 1 个节点，应插在 1。
/// </summary>
[HarmonyPatch(typeof(NRelicInventory), "Add")]
public static class FullMoonLedgerInventoryPatch
{
    /// <summary>
    /// 是手账时不加节点；其他遗物的插入位置跳过前面的手账。
    /// </summary>
    /// <param name="relic">遗物。</param>
    /// <param name="index">插入位置（玩家遗物列表里的位置；-1 为加在末尾）。</param>
    /// <returns>继续原方法时为 true。</returns>
    [HarmonyPrefix]
    private static bool SkipLedger(RelicModel relic, ref int index)
    {
        if (relic is FullMoonLedger)
        {
            return false;
        }
        if (index > 0 && relic.Owner != null)
        {
            index -= relic.Owner.Relics.Take(index).Count(r => r is FullMoonLedger);
        }
        return true;
    }
}

/// <summary>
/// 移除手账时遗物栏里本来就没有它的节点，跳过原方法。
/// </summary>
[HarmonyPatch(typeof(NRelicInventory), "Remove")]
public static class FullMoonLedgerInventoryRemovePatch
{
    /// <summary>
    /// 是手账时跳过。
    /// </summary>
    /// <param name="relic">遗物。</param>
    /// <returns>继续原方法时为 true。</returns>
    [HarmonyPrefix]
    private static bool SkipLedger(RelicModel relic)
    {
        return relic is not FullMoonLedger;
    }
}
