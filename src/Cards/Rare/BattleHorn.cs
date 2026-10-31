using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 战斗号角：消耗，本回合你每打出 1 张攻击牌，抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BattleHorn : KnightCard
{
    /// <summary>
    /// 创建战斗号角。
    /// </summary>
    public BattleHorn() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BattleHornPower>(choiceContext, Owner.Creature, DynamicVars.Cards.BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 战斗号角效果：本回合每打出 1 张攻击牌，抽层数张牌。
/// </summary>
public sealed class BattleHornPower : KnightPower
{
    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者打出攻击牌后抽牌。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, cardPlay.Card.Owner);
    }

    /// <summary>
    /// 持有者一方回合结束时移除。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="side">结束回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
