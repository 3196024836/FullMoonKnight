using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Powers;

/// <summary>
/// 流血（水晶头骨用）：持有者每受到一次攻击伤害，额外受到穿刺伤害并失去 1 层。
/// </summary>
public sealed class BleedPower : KnightPower
{
    /// <summary>每次触发的伤害变量名。</summary>
    private const string DamageVar = "Damage";

    /// <summary>每次触发的伤害。</summary>
    private const decimal DamagePerHit = 1m;

    /// <inheritdoc />
    public override PowerType Type => PowerType.Debuff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(DamageVar, DamagePerHit)];

    /// <summary>
    /// 受到攻击伤害后流血。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || Owner.IsDead)
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, Owner, DamagePerHit, ValueProp.Unblockable | ValueProp.Unpowered,
            null, null);
        await PowerCmd.Decrement(this);
    }
}
