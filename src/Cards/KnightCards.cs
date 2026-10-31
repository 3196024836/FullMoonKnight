using System.Reflection;

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 卡牌效果的公共辅助：临时牌、随机生成女骑士牌、随机目标。
/// "临时"指本场战斗生成的牌：打出后消耗，战斗结束消失。
/// </summary>
public static class KnightCards
{
    /// <summary>复制牌的新版入口要求明确归属玩家，旧版无参数入口保留原归属。</summary>
    private static readonly MethodInfo DupeMethod = typeof(CardModel).GetMethod("CreateDupe", [typeof(Player)])
        ?? typeof(CardModel).GetMethod("CreateDupe", Type.EmptyTypes)!;

    /// <summary>创建战斗临时复制品，不改变归属。</summary>
    public static CardModel Duplicate(CardModel card)
    {
        return (CardModel)DupeMethod.Invoke(card, DupeMethod.GetParameters().Length == 0 ? null : [card.Owner])!;
    }

    /// <summary>
    /// 把一张战斗中生成的牌标为临时（加消耗）。能力牌打出后本来就离开牌堆，不加。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>同一张牌。</returns>
    public static CardModel MakeTemporary(CardModel card)
    {
        if (card is KnightCard knightCard)
        {
            knightCard.GeneratedByKnight = true;
        }
        if (card.Type != CardType.Power)
        {
            card.AddKeyword(CardKeyword.Exhaust);
        }
        return card;
    }

    /// <summary>
    /// 把临时牌加入战斗牌堆。
    /// </summary>
    /// <param name="card">新生成的牌（还没有牌堆）。</param>
    /// <param name="pile">目标牌堆。</param>
    /// <param name="owner">生成者。</param>
    /// <param name="position">放入位置；洗入抽牌堆时用随机位置。</param>
    /// <returns>异步任务。</returns>
    public static async Task AddTemporary(CardModel card, PileType pile, Player owner,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        MakeTemporary(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, pile, owner, position);
    }

    /// <summary>
    /// 只从抽牌堆抽牌：抽牌堆不够时有几张抽几张，不把弃牌堆洗回来。
    /// 用于 0 费、抽得比自己多的牌（二次打击、过劳、无畏）：正常打牌时和普通抽牌一样，
    /// 只在一回合里想把牌组反复洗着打的时候断开循环（女骑士的牌几乎都是 0 费，没有能量来限速）。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="count">张数。</param>
    /// <param name="owner">抽牌的玩家。</param>
    /// <returns>异步任务。</returns>
    public static Task DrawFromPile(PlayerChoiceContext choiceContext, decimal count, Player owner)
    {
        // 抽牌堆剩余张数
        int remaining = PileType.Draw.GetPile(owner).Cards.Count;
        return CardPileCmd.Draw(choiceContext, Math.Min(count, remaining), owner);
    }

    /// <summary>
    /// 从女骑士卡池随机生成若干张不重复的战斗用牌（不含基础牌，使用同步随机数）。
    /// </summary>
    /// <param name="owner">持有者。</param>
    /// <param name="count">张数。</param>
    /// <param name="filter">筛选条件；为空时不筛选。</param>
    /// <returns>新生成的牌（还没有牌堆）。</returns>
    public static List<CardModel> RandomKnightCards(Player owner, int count, Func<CardModel, bool>? filter = null)
    {
        // 候选牌
        IEnumerable<CardModel> candidates = ModelDb.CardPool<KnightCardPool>()
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => filter == null || filter(c));
        return CardFactory.GetDistinctForCombat(owner, candidates, count, owner.RunState.Rng.CombatCardGeneration)
            .ToList();
    }

    /// <summary>
    /// 牌对自己造成穿刺伤害（原作"受到 N 点穿刺伤害"）。伤害来源是自己，殉道之剑据此判断自伤。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="card">造成伤害的牌。</param>
    /// <param name="amount">伤害。</param>
    /// <returns>异步任务。</returns>
    public static Task SelfDamage(PlayerChoiceContext choiceContext, CardModel card, decimal amount, CardPlay? cardPlay = null)
    {
        return KnightDamage.Deal(choiceContext, card.Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, card.Owner.Creature, card, cardPlay);
    }

    /// <summary>
    /// 按一张牌（可以是牌组里的牌）新建一张同名、同升级状态的战斗用牌。
    /// </summary>
    /// <param name="source">原牌。</param>
    /// <param name="owner">新牌的持有者。</param>
    /// <returns>新生成的牌（还没有牌堆）。</returns>
    public static CardModel CombatCopy(CardModel source, Player owner)
    {
        // 新牌
        CardModel copy = owner.Creature.CombatState!.CreateCard(source.CanonicalInstance, owner);
        if (source.IsUpgraded && copy.IsUpgradable)
        {
            CardCmd.Upgrade(copy);
        }
        return copy;
    }

    /// <summary>
    /// 从若干张候选牌里选 1 张。不超过 3 张用三选一界面，更多时用网格界面（三选一界面最多 3 张）。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="options">候选牌。</param>
    /// <param name="owner">做选择的玩家。</param>
    /// <param name="prompt">网格界面的提示文字。</param>
    /// <returns>选中的牌；没选时为空。</returns>
    public static async Task<CardModel?> ChooseOne(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> options,
        Player owner, LocString prompt)
    {
        if (options.Count <= 3)
        {
            return await CardSelectCmd.FromChooseACardScreen(choiceContext, options, owner);
        }
        // 网格选择结果
        IEnumerable<CardModel> chosen = await CardSelectCmd.FromSimpleGrid(choiceContext, options, owner,
            new CardSelectorPrefs(prompt, 1));
        return chosen.FirstOrDefault();
    }

    /// <summary>
    /// 是否装备牌。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>带装备关键词时为 true。</returns>
    public static bool IsEquipment(CardModel card)
    {
        return card.Keywords.Contains(KnightKeywords.Equip);
    }

    /// <summary>
    /// 随机选一个可攻击的敌人（同步随机数）。
    /// </summary>
    /// <param name="owner">发起者所属的玩家。</param>
    /// <returns>敌人；没有时为空。</returns>
    public static Creature? RandomEnemy(Player owner)
    {
        // 可攻击的敌人
        IReadOnlyList<Creature> enemies = owner.Creature.CombatState?.HittableEnemies ?? [];
        return enemies.Count == 0 ? null : owner.RunState.Rng.CombatTargets.NextItem(enemies);
    }
}
