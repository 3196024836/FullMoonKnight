using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Powers;

/// <summary>
/// 恶作剧（D25）：每层使持有者下一次攻击的伤害降低 25%，最多 3 层，攻击结算后清空。
/// </summary>
public sealed class MischiefPower : KnightPower
{
    // ===== 常量 =====

    /// <summary>最大层数。</summary>
    public const int MaxStacks = 3;

    /// <summary>每层减伤比例变量名。</summary>
    private const string PercentVar = "Percent";

    /// <summary>每层减伤比例（百分数）。</summary>
    private const int PercentPerStack = 25;

    // ===== 属性 =====

    /// <inheritdoc />
    public override PowerType Type => PowerType.Debuff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(PercentVar, PercentPerStack)];

    // ===== 钩子 =====

    /// <summary>
    /// 层数超过上限时压回上限。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="power">变化的能力。</param>
    /// <param name="amount">变化量。</param>
    /// <param name="applier">施加者。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power == this && Amount > MaxStacks)
        {
            SetAmount(MaxStacks);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 持有者的攻击伤害按层数降低。
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
        if (dealer != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }
        return 1m - Math.Min(Amount, MaxStacks) * PercentPerStack / 100m;
    }

    /// <summary>
    /// 持有者的一次攻击结算后清空恶作剧。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="command">攻击命令。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker == Owner && command.DamageProps.IsPoweredAttack())
        {
            await PowerCmd.Remove(this);
        }
    }
}
