using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 外婆的祝福（原作 8511，外婆的叮嘱给）：战斗开始时在手牌里；打出后本场移除，从 3 张牌里选 1 张加入手牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class GrandmasBlessing : KnightCard
{
    /// <summary>发现的候选数。</summary>
    private const int OfferCount = 3;

    /// <summary>
    /// 创建外婆的祝福。
    /// </summary>
    public GrandmasBlessing() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(OfferCount)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 候选：自己卡池里的牌
        List<CardModel> cards = CardFactory.GetDistinctForCombat(Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            OfferCount, Owner.RunState.Rng.CombatCardGeneration).ToList();
        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, Owner);
        if (chosen != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
        }
    }
}

/// <summary>
/// 外婆的拐杖（原作 4100，战斗痕迹给）：不能打出。持有它时第 3 幕可能出现"可疑的痕迹"。
/// </summary>
[Pool(typeof(QuestCardPool))]
public sealed class GrandmasCrutch : KnightCard
{
    /// <summary>
    /// 创建外婆的拐杖。
    /// </summary>
    public GrandmasCrutch() : base(-1, CardType.Quest, CardRarity.Quest, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <inheritdoc />
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 沉睡的发条骑士（原作 8703，封存的记忆）：装备。第一次回合结束时装备 1 张随机装备牌，然后醒来变成发条骑士；
/// 之后每回合结束时对随机敌人造成等同于装备件数 × 2 的伤害（原作 8706，伤害 ×2 换算）。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class SleepingClockworkKnight : KnightEquipmentCard<ClockworkKnightPower>
{
    /// <summary>
    /// 创建沉睡的发条骑士。
    /// </summary>
    public SleepingClockworkKnight() : base(CardRarity.Event)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PerEquip", ClockworkKnightPower.DamagePerEquip)];

    /// <inheritdoc />
    protected override decimal EquipAmount => 1;
}

/// <summary>
/// 发条骑士装备：沉睡时回合结束装备 1 张随机装备牌并醒来；醒来后回合结束按装备件数造成伤害。
/// 层数是件数（多张叠加时每件各结算一次）。
/// </summary>
public sealed class ClockworkKnightPower : KnightEquipmentPower
{
    /// <summary>每件装备造成的伤害（原作 1 点，×2）。</summary>
    public const int DamagePerEquip = 2;

    /// <summary>已经醒来。</summary>
    private bool awake;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PerEquip", DamagePerEquip)];

    /// <summary>
    /// 持有者回合结束：沉睡时装备并醒来，醒来后造成伤害。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="side">结束回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <returns>异步任务。</returns>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
        {
            return;
        }
        AssertMutable();
        Flash();
        if (!awake)
        {
            awake = true;
            // 随机装备牌，直接打出
            foreach (CardModel card in KnightCards.RandomKnightCards(Owner.Player, (int)Amount, KnightCards.IsEquipment))
            {
                await CardCmd.AutoPlay(choiceContext, KnightCards.MakeTemporary(card), null);
            }
            return;
        }
        for (int i = 0; i < Amount; i++)
        {
            Creature? target = KnightCards.RandomEnemy(Owner.Player);
            if (target == null)
            {
                return;
            }
            await KnightDamage.Deal(choiceContext, target, DamagePerEquip * CountEquipment(Owner),
                ValueProp.Unpowered | KnightDamage.Equipment, Owner, null);
        }
    }
}

/// <summary>
/// 致命餐叉（原作 8707，封存的记忆）：装备。你打出攻击牌时，丢弃最左边的手牌，这张攻击牌额外触发 1 次。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class DeadlyFork : KnightEquipmentCard<DeadlyForkPower>
{
    /// <summary>
    /// 创建致命餐叉。
    /// </summary>
    public DeadlyFork() : base(CardRarity.Event)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    protected override decimal EquipAmount => 1;
}

/// <summary>
/// 致命餐叉装备：持有者打出攻击牌（不是餐叉触发的复制）时，丢弃最左边的手牌，打出这张攻击牌的复制。
/// 手牌为空时不触发。
/// </summary>
public sealed class DeadlyForkPower : KnightEquipmentPower
{
    /// <summary>餐叉触发打出的复制（不再触发餐叉；被打出多次时也一样，所以不移除）。</summary>
    private readonly HashSet<CardModel> replays = [];

    /// <summary>
    /// 持有者打出攻击牌后：丢弃最左边的手牌，再打出一次。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Owner.Creature != Owner || card.Type != CardType.Attack || replays.Contains(card))
        {
            return;
        }
        // 最左边的手牌
        CardModel? leftmost = PileType.Hand.GetPile(card.Owner).Cards.FirstOrDefault();
        if (leftmost == null)
        {
            return;
        }
        Flash();
        await CardCmd.Discard(choiceContext, leftmost);
        CardModel copy = KnightCards.Duplicate(card);
        replays.Add(copy);
        await CardCmd.AutoPlay(choiceContext, copy, cardPlay.Target);
    }
}

/// <summary>
/// 弹簧拳套（原作 8712，封存的记忆）：装备。你打出技能牌后，本回合下一张攻击牌伤害翻倍，下回合能量 +1（每回合最多 +1）。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class SpringGlove : KnightEquipmentCard<SpringGlovePower>
{
    /// <summary>
    /// 创建弹簧拳套。
    /// </summary>
    public SpringGlove() : base(CardRarity.Event)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    protected override decimal EquipAmount => 1;
}

/// <summary>
/// 弹簧拳套装备：打出技能牌后蓄力，下一张攻击牌伤害 ×2；本回合第一次蓄力时下回合能量 +1。
/// </summary>
public sealed class SpringGlovePower : KnightEquipmentPower
{
    /// <summary>已蓄力（下一张攻击牌翻倍）。</summary>
    private bool charged;

    /// <summary>本回合已经给过下回合能量。</summary>
    private bool energyGiven;

    /// <summary>
    /// 出牌后：技能牌蓄力，攻击牌用掉蓄力。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }
        AssertMutable();
        if (cardPlay.Card.Type == CardType.Attack)
        {
            charged = false;
            return;
        }
        if (cardPlay.Card.Type != CardType.Skill)
        {
            return;
        }
        charged = true;
        if (!energyGiven)
        {
            energyGiven = true;
            Flash();
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner, 1, Owner, null);
        }
    }

    /// <summary>
    /// 蓄力时，持有者攻击牌的伤害翻倍。
    /// </summary>
    /// <param name="target">目标。</param>
    /// <param name="amount">伤害。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>倍数。</returns>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (!charged || dealer != Owner || cardSource?.Type != CardType.Attack || !props.IsPoweredAttack())
        {
            return 1m;
        }
        return 2m;
    }

    /// <summary>
    /// 持有者回合结束：蓄力和能量次数清零。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="side">结束回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            AssertMutable();
            charged = false;
            energyGiven = false;
        }
        return Task.CompletedTask;
    }
}
