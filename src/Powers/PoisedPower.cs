using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Powers;

/// <summary>
/// 蓄势：接下来层数张对敌人造成伤害的牌，伤害翻倍（每张牌消耗 1 层）。弱点侦查、筹谋、殉道之剑使用。
/// </summary>
public sealed class PoisedPower : KnightPower
{
    /// <summary>本次出牌中已经享受翻倍的牌。</summary>
    private CardModel? boostedCard;

    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者的牌造成的伤害翻倍。
    /// </summary>
    /// <param name="target">受伤者。</param>
    /// <param name="amount">当前伤害。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>伤害倍率。</returns>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        return dealer == Owner && (target == null || target.Side != Owner.Side)
            && cardSource != null && cardSource.Owner.Creature == Owner ? 2m : 1m;
    }

    /// <summary>
    /// 记录造成伤害的牌，出牌结束时再消耗层数（多段伤害都翻倍）。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && target.Side != Owner.Side && cardSource != null && cardSource.Owner.Creature == Owner)
        {
            AssertMutable();
            boostedCard = cardSource;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 造成过伤害的牌结算完后消耗 1 层。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (boostedCard != null && cardPlay.Card == boostedCard)
        {
            boostedCard = null;
            await PowerCmd.Decrement(this);
        }
    }
}
