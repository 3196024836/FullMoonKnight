using System.Runtime.CompilerServices;

using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 原版内容对女骑士的专属适配：只改会和女骑士产生质变或出错的那几件物品，只对女骑士生效，其他角色拿到完全是原版。
/// 女骑士 1 能量、每回合抽 2 张、大多数牌 0 费、装备是叠加的 0 费能力牌；原版“打空手牌就抽”“打出就回能”这类效果
/// 按别的角色要花能量来设计，在女骑士身上会变成一回合打不完。适配保留物品的味道，只封住失控的那一环，
/// 并在悬停提示里写明（relics.json / cards.json 的 FULLMOONKNIGHT-ADAPT.* 文本）。
/// 审计记录见 docs/design/mechanics.md。
/// </summary>
[HarmonyPatch]
public static class VanillaAdaptations
{
    /// <summary>女骑士持有不休陀螺时每回合最多触发的次数（正好是每回合的基础抽牌数）。</summary>
    public const int UnceasingTopPerTurn = 2;

    /// <summary>女骑士持有手里剑、苦无、装饰扇时每回合最多触发的次数（每 3 张攻击牌一次）。</summary>
    public const int AttackCounterRelicPerTurn = 2;

    /// <summary>适配说明文本键的前缀（后接原版物品 ID）。</summary>
    private const string AdaptKeyPrefix = "FULLMOONKNIGHT-ADAPT.";

    /// <summary>适配说明的标题键。</summary>
    private const string AdaptTitleKey = "FULLMOONKNIGHT-ADAPT.title";

    /// <summary>遗物的每回合触发计数。</summary>
    private static readonly ConditionalWeakTable<RelicModel, TurnCount> RelicCounts = new();

    /// <summary>玩家每回合打出各种牌的计数（按牌的类型）。</summary>
    private static readonly ConditionalWeakTable<Player, Dictionary<Type, TurnCount>> CardCounts = new();

    /// <summary>
    /// 本回合计数。
    /// </summary>
    private sealed class TurnCount
    {
        /// <summary>计数所在的战斗。</summary>
        public object? Combat;

        /// <summary>计数所在的回合。</summary>
        public int Turn;

        /// <summary>本回合次数。</summary>
        public int Count;
    }

    /// <summary>
    /// 玩家是否是女骑士。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>是女骑士时为 true。</returns>
    public static bool IsKnight(Player? player)
    {
        return player?.Character is Knight;
    }

    /// <summary>
    /// 把计数对到本回合（换了战斗或回合时从 0 开始）。
    /// </summary>
    /// <param name="count">计数。</param>
    /// <param name="player">回合所属的玩家。</param>
    /// <returns>同一个计数。</returns>
    private static TurnCount ThisTurn(TurnCount count, Player player)
    {
        // 当前战斗和回合
        object? combat = player.Creature.CombatState;
        int turn = player.PlayerCombatState?.TurnNumber ?? 0;
        if (!ReferenceEquals(count.Combat, combat) || count.Turn != turn)
        {
            count.Combat = combat;
            count.Turn = turn;
            count.Count = 0;
        }
        return count;
    }

    // ===== 不休陀螺 =====

    /// <summary>
    /// 不休陀螺：女骑士每回合最多触发 <see cref="UnceasingTopPerTurn" /> 次。
    /// 女骑士的牌几乎都是 0 费，打空手牌是常态，原版会每打一张补一张，一回合打完整副牌。
    /// </summary>
    /// <param name="__instance">不休陀螺。</param>
    /// <param name="player">手牌打空的玩家。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(UnceasingTop), nameof(UnceasingTop.AfterHandEmptied))]
    [HarmonyPrefix]
    public static bool UnceasingTopLimit(UnceasingTop __instance, Player player, ref Task __result)
    {
        if (player != __instance.Owner || !IsKnight(player) || player.PlayerCombatState is not { } state
            || state.Phase is not (PlayerTurnPhase.AutoPrePlay or PlayerTurnPhase.Play or PlayerTurnPhase.AutoPostPlay))
        {
            return true;
        }
        // 本回合已触发的次数
        TurnCount count = ThisTurn(RelicCounts.GetOrCreateValue(__instance), player);
        if (count.Count >= UnceasingTopPerTurn)
        {
            __result = Task.CompletedTask;
            return false;
        }
        count.Count++;
        return true;
    }

    // ===== 手里剑、苦无、装饰扇 =====

    /// <summary>
    /// 手里剑、苦无、装饰扇、锁镰（每回合每打出 3 张攻击牌触发一次）的处理方法。
    /// </summary>
    /// <returns>四个遗物的打出牌后方法。</returns>
    private static IEnumerable<System.Reflection.MethodBase> AttackCounterRelicMethods()
    {
        yield return AccessTools.Method(typeof(Shuriken), nameof(Shuriken.AfterCardPlayed));
        yield return AccessTools.Method(typeof(Kunai), nameof(Kunai.AfterCardPlayed));
        yield return AccessTools.Method(typeof(OrnamentalFan), nameof(OrnamentalFan.AfterCardPlayed));
        yield return AccessTools.Method(typeof(Kusarigama), nameof(Kusarigama.AfterCardPlayed));
    }

    /// <summary>
    /// 手里剑、苦无、装饰扇、锁镰：女骑士每回合最多触发 <see cref="AttackCounterRelicPerTurn" /> 次。
    /// 女骑士的攻击牌多为 0 费，后期一回合打十几张，原版会每回合叠 4～6 层力量或敏捷。
    /// </summary>
    [HarmonyPatch]
    public static class AttackCounterRelicLimit
    {
        /// <summary>要打补丁的方法。</summary>
        /// <returns>四个遗物的打出牌后方法。</returns>
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods() => AttackCounterRelicMethods();

        /// <summary>
        /// 本回合已经触发够次数时跳过原方法（计数也停在这里，不再闪烁）。
        /// </summary>
        /// <param name="__instance">遗物。</param>
        /// <param name="cardPlay">出牌信息。</param>
        /// <param name="__result">跳过时返回的任务。</param>
        /// <returns>执行原方法时为 true。</returns>
        public static bool Prefix(RelicModel __instance, CardPlay cardPlay, ref Task __result)
        {
            if (cardPlay.Card.Owner != __instance.Owner || !IsKnight(__instance.Owner)
                || cardPlay.Card.Type != CardType.Attack)
            {
                return true;
            }
            // 本回合已打出的攻击牌数（原版每回合开始清零）
            int played = Traverse.Create(__instance).Field<int>("_attacksPlayedThisTurn").Value;
            int perTrigger = __instance.DynamicVars.Cards.IntValue;
            if (played < AttackCounterRelicPerTurn * perTrigger)
            {
                return true;
            }
            __result = Task.CompletedTask;
            return false;
        }
    }

    // ===== 最亮之火、焦躁 =====

    /// <summary>
    /// 最亮之火（先祖牌，回能并抽牌、不消耗）：女骑士每回合第一次打出照常结算，之后同一回合再打出只抽 1 张。
    /// 对 1 能量的女骑士一次就是三倍能量，配合 0 费抽牌能在一回合里反复洗回来。
    /// </summary>
    /// <param name="__instance">最亮之火。</param>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(BrightestFlame), "OnPlay")]
    [HarmonyPrefix]
    public static bool BrightestFlameOncePerTurn(CardModel __instance, PlayerChoiceContext choiceContext,
        ref Task __result)
    {
        return OncePerTurn(__instance, choiceContext, true, ref __result);
    }

    /// <summary>
    /// 焦躁（手里只剩它时回能并抽牌、不消耗）：同最亮之火，女骑士每回合只有第一次生效时完整结算。
    /// </summary>
    /// <param name="__instance">焦躁。</param>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(Restlessness), "OnPlay")]
    [HarmonyPrefix]
    public static bool RestlessnessOncePerTurn(CardModel __instance, PlayerChoiceContext choiceContext,
        ref Task __result)
    {
        // 手里是否只剩它（原版的生效条件）
        bool fires = !PileType.Hand.GetPile(__instance.Owner).Cards.Any(c => c != __instance);
        return OncePerTurn(__instance, choiceContext, fires, ref __result);
    }

    /// <summary>
    /// 每回合完整结算一次：本回合已经完整结算过同名牌时，这次只抽 1 张。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="fires">这次打出原版效果是否生效。</param>
    /// <param name="result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    private static bool OncePerTurn(CardModel card, PlayerChoiceContext choiceContext, bool fires, ref Task result)
    {
        // 牌的持有者
        Player owner = card.Owner;
        if (!fires || !IsKnight(owner))
        {
            return true;
        }
        // 这名玩家本回合完整结算这种牌的次数
        Dictionary<Type, TurnCount> counts = CardCounts.GetOrCreateValue(owner);
        if (!counts.TryGetValue(card.GetType(), out TurnCount? stored))
        {
            stored = new TurnCount();
            counts[card.GetType()] = stored;
        }
        TurnCount count = ThisTurn(stored, owner);
        if (count.Count == 0)
        {
            count.Count++;
            return true;
        }
        result = CardPileCmd.Draw(choiceContext, 1, owner);
        return false;
    }

    // ===== 灾厄 =====

    /// <summary>战斗中生成的牌（灾厄生成的攻击不再触发灾厄）。</summary>
    public static readonly ConditionalWeakTable<CardModel, object> GeneratedCards = new();

    /// <summary>
    /// 灾厄（打出攻击时生成随机攻击）：女骑士打出战斗中生成的攻击时不触发。
    /// 女骑士的攻击八成是 0 费，生成的攻击打出去又会生成，链条几乎不断。
    /// </summary>
    /// <param name="__instance">灾厄能力。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(CalamityPower), nameof(CalamityPower.BeforeCardPlayed))]
    [HarmonyPrefix]
    public static bool CalamityIgnoresGenerated(CalamityPower __instance, CardPlay cardPlay, ref Task __result)
    {
        if (!IsKnight(__instance.Owner.Player) || !GeneratedCards.TryGetValue(cardPlay.Card, out _))
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }

    // ===== 帕埃尔之芽 =====

    /// <summary>
    /// 帕埃尔之芽的克隆：女骑士的装备复制出来的副本不带克隆附魔，每次休息只多 1 件，不再翻倍。
    /// 原版副本连附魔一起复制，数量 1→2→4→8；装备 0 费、整场生效、同名叠加，翻倍下去一场战斗就失控。
    /// </summary>
    /// <param name="__instance">休息处的克隆选项。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(CloneRestSiteOption), nameof(CloneRestSiteOption.OnSelect))]
    [HarmonyPrefix]
    public static bool CloneEquipmentLinear(CloneRestSiteOption __instance, ref Task<bool> __result)
    {
        // 选项的玩家
        Player owner = Traverse.Create(__instance).Property("Owner").GetValue<Player>();
        if (!IsKnight(owner))
        {
            return true;
        }
        __result = CloneLinear(owner);
        return false;
    }

    /// <summary>
    /// 复制所有带克隆附魔的牌；装备的副本去掉附魔。
    /// </summary>
    /// <param name="owner">玩家。</param>
    /// <returns>是否完成。</returns>
    private static async Task<bool> CloneLinear(Player owner)
    {
        // 带克隆附魔的牌、加入结果
        List<CardModel> sources = owner.Deck.Cards.Where(c => c.Enchantment is Clone).ToList();
        List<CardPileAddResult> results = [];
        foreach (CardModel source in sources)
        {
            // 副本
            CardModel copy = owner.RunState.CloneCard(source);
            if (KnightCards.IsEquipment(copy))
            {
                CardCmd.ClearEnchantment(copy);
            }
            results.Add(await CardPileCmd.Add(copy, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.MessyLayout);
        return true;
    }

    // ===== 涅奥的护符 =====

    /// <summary>
    /// 涅奥的护符（升级最后一张基础打击和防御）：女骑士改为升级最后一张还能升级的。
    /// 女骑士的初始打击里有已升级的打击+，原版会选中它，升级落空。
    /// </summary>
    /// <param name="__instance">护符。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(NeowsTalisman), nameof(NeowsTalisman.AfterObtained))]
    [HarmonyPrefix]
    public static bool TalismanUpgradesUpgradable(NeowsTalisman __instance, ref Task __result)
    {
        if (!IsKnight(__instance.Owner))
        {
            return true;
        }
        // 可升级的基础牌
        List<CardModel> basics = PileType.Deck.GetPile(__instance.Owner).Cards
            .Where(c => c.Rarity == CardRarity.Basic && c.IsUpgradable).ToList();
        foreach (CardTag tag in new[] { CardTag.Strike, CardTag.Defend })
        {
            if (basics.LastOrDefault(c => c.Tags.Contains(tag)) is { } card)
            {
                CardCmd.Upgrade(card);
            }
        }
        __result = Task.CompletedTask;
        return false;
    }

    // ===== 电镀（地球头） =====

    /// <summary>
    /// 地球头的电镀（玩家的能力牌被电镀，打出时受 Amount 点伤害）：女骑士的装备只受一半伤害（向上取整）。
    /// 别的角色一场打 2~4 张能力牌；女骑士的装备是能力牌，一场打 8~10 件，原数值一场要掉 50 点生命。
    /// </summary>
    /// <param name="__instance">电镀能力。</param>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(GalvanicPower), nameof(GalvanicPower.AfterCardPlayed))]
    [HarmonyPrefix]
    public static bool GalvanicHalvedForEquipment(GalvanicPower __instance, PlayerChoiceContext choiceContext,
        CardPlay cardPlay, ref Task __result)
    {
        // 打出的牌
        CardModel card = cardPlay.Card;
        if (card.Affliction is not Galvanized || !IsKnight(card.Owner) || !KnightCards.IsEquipment(card))
        {
            return true;
        }
        __result = GalvanicDamage(choiceContext, card.Owner, Math.Ceiling(__instance.Amount / 2m));
        return false;
    }

    /// <summary>
    /// 电镀伤害（同原版的特效和伤害属性）。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="player">受伤的玩家。</param>
    /// <param name="amount">伤害。</param>
    /// <returns>异步任务。</returns>
    private static async Task GalvanicDamage(PlayerChoiceContext choiceContext, Player player, decimal amount)
    {
        VfxCmd.PlayOnCreature(player.Creature, "vfx/vfx_attack_lightning");
        await KnightDamage.Deal(choiceContext, player.Creature, amount, ValueProp.Unpowered | ValueProp.Move, null,
            null);
    }

    // ===== 洗牌时效果 =====

    /// <summary>
    /// 算盘（洗牌时获得格挡）：女骑士每回合开始都会把弃牌堆并回，只有累计抽过整副牌的那次并回才触发。
    /// </summary>
    /// <param name="shuffler">洗牌的玩家。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(TheAbacus), nameof(TheAbacus.AfterShuffle))]
    [HarmonyPrefix]
    public static bool AbacusCountedShuffle(Player shuffler, ref Task __result)
    {
        return CountedShuffle(shuffler, ref __result);
    }

    /// <summary>
    /// 大大的拥抱（洗牌时加煤灰）：同算盘，只在算作洗牌的那次并回时触发。
    /// </summary>
    /// <param name="shuffler">洗牌的玩家。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(BiiigHug), nameof(BiiigHug.AfterShuffle))]
    [HarmonyPrefix]
    public static bool BiiigHugCountedShuffle(Player shuffler, ref Task __result)
    {
        return CountedShuffle(shuffler, ref __result);
    }

    /// <summary>
    /// 谋略（洗牌时从抽牌堆选牌）：同算盘，只在算作洗牌的那次并回时触发。
    /// </summary>
    /// <param name="player">洗牌的玩家。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(StratagemPower), nameof(StratagemPower.AfterShuffle))]
    [HarmonyPrefix]
    public static bool StratagemCountedShuffle(Player player, ref Task __result)
    {
        return CountedShuffle(player, ref __result);
    }

    /// <summary>
    /// 完美契合（洗牌时放到抽牌堆顶）：同算盘，只在算作洗牌的那次并回时生效，否则每回合必抽到。
    /// </summary>
    /// <param name="player">洗牌的玩家。</param>
    /// <returns>执行原方法时为 true。</returns>
    [HarmonyPatch(typeof(PerfectFit), nameof(PerfectFit.ModifyShuffleOrder))]
    [HarmonyPrefix]
    public static bool PerfectFitCountedShuffle(Player player)
    {
        return !KnightDrawCycle.IsQuietShuffle(player);
    }

    /// <summary>
    /// 洗牌时效果的公共判断：回合开始的并回不算洗牌时跳过。
    /// </summary>
    /// <param name="player">洗牌的玩家。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>执行原方法时为 true。</returns>
    private static bool CountedShuffle(Player player, ref Task __result)
    {
        if (!KnightDrawCycle.IsQuietShuffle(player))
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }

    // ===== 悬停说明 =====

    /// <summary>
    /// 女骑士看到被适配的原版遗物时，悬停提示里多一条适配说明。
    /// </summary>
    /// <param name="__instance">遗物。</param>
    /// <param name="__result">悬停提示列表。</param>
    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.HoverTips), MethodType.Getter)]
    [HarmonyPostfix]
    public static void RelicNote(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (Note("relics", __instance.Id.Entry, __instance.IsMutable ? __instance.Owner : null) is { } tip)
        {
            __result = __result.Append(tip).ToList();
        }
    }

    /// <summary>
    /// 女骑士看到被适配的原版卡牌时，悬停提示里多一条适配说明。
    /// </summary>
    /// <param name="__instance">卡牌。</param>
    /// <param name="__result">悬停提示列表。</param>
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    [HarmonyPostfix]
    public static void CardNote(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (Note("cards", __instance.Id.Entry, __instance.IsMutable ? __instance.Owner : null) is { } tip)
        {
            __result = __result.Append(tip).ToList();
        }
    }

    /// <summary>
    /// 适配说明：有对应文本、且看的人是女骑士时返回提示。
    /// </summary>
    /// <param name="table">本地化表。</param>
    /// <param name="id">物品 ID。</param>
    /// <param name="owner">物品的持有者；还没被拿走（奖励、商店）时为空，按本地玩家判断。</param>
    /// <returns>提示；不需要时为空。</returns>
    private static IHoverTip? Note(string table, string id, Player? owner)
    {
        // 说明文本键
        string key = AdaptKeyPrefix + id;
        if (!LocString.Exists(table, key))
        {
            return null;
        }
        // 看提示的玩家
        Player? viewer = owner ?? (RunManager.Instance.DebugOnlyGetState() is { } run
            ? MegaCrit.Sts2.Core.Context.LocalContext.GetMe(run)
            : null);
        if (!IsKnight(viewer))
        {
            return null;
        }
        return new HoverTip(new LocString(table, AdaptTitleKey), new LocString(table, key));
    }
}
