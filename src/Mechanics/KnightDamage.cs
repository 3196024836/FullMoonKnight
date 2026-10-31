using System.Reflection;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 伤害属性：穿刺用原生无视格挡；火焰、雷电用未被原生占用的高位标志，数值同普通伤害。
/// </summary>
public static class KnightDamage
{
    // ===== 属性标志 =====

    /// <summary>火焰伤害标志。</summary>
    public const ValueProp Fire = (ValueProp)0x100;

    /// <summary>雷电伤害标志。</summary>
    public const ValueProp Thunder = (ValueProp)0x200;

    /// <summary>装备伤害标志：装备能力造成的伤害（奶酪陷阱据此加成）。</summary>
    public const ValueProp Equipment = (ValueProp)0x400;

    /// <summary>穿刺攻击的属性：卡牌攻击且无视格挡（仍吃力量等加成）。</summary>
    public const ValueProp PierceMove = ValueProp.Move | ValueProp.Unblockable;

    /// <summary>火焰攻击的属性。</summary>
    public const ValueProp FireMove = ValueProp.Move | Fire;

    /// <summary>雷电攻击的属性。</summary>
    public const ValueProp ThunderMove = ValueProp.Move | Thunder;

    // ===== 跨分支伤害入口 =====

    /// <summary>正式版六参数入口或测试版新增 CardPlay 的七参数入口，补丁与调用共享同一目标。</summary>
    public static MethodInfo DamageTarget { get; } = ResolveDamageTarget();

    /// <summary>解析完整的多目标伤害结算入口，不能误选无来源的便捷重载。</summary>
    private static MethodInfo ResolveDamageTarget()
    {
        Type[] original = [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel)];
        return typeof(CreatureCmd).GetMethod(nameof(CreatureCmd.Damage), [.. original, typeof(CardPlay)])
            ?? typeof(CreatureCmd).GetMethod(nameof(CreatureCmd.Damage), original)
            ?? throw new MissingMethodException("Unsupported CreatureCmd.Damage signature");
    }

    /// <summary>对单个目标结算伤害，保留来源生物、来源卡和实际出牌上下文。</summary>
    public static Task<IEnumerable<DamageResult>> Deal(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay = null)
    {
        return Deal(choiceContext, [target], amount, props, dealer, cardSource, cardPlay);
    }

    /// <summary>对多个目标结算伤害；仅省略旧分支不存在的 CardPlay 参数。</summary>
    public static Task<IEnumerable<DamageResult>> Deal(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay = null)
    {
        object?[] arguments = [choiceContext, targets, amount, props, dealer, cardSource];
        if (DamageTarget.GetParameters().Length == 7)
        {
            arguments = [.. arguments, cardPlay];
        }
        return (Task<IEnumerable<DamageResult>>)DamageTarget.Invoke(null, arguments)!;
    }

    // ===== 特效 =====

    /// <summary>普通斩击命中特效。</summary>
    public const string SlashVfx = "vfx/vfx_attack_slash";

    /// <summary>钝击命中特效。</summary>
    public const string BluntVfx = "vfx/vfx_attack_blunt";

    /// <summary>火焰命中特效。</summary>
    public const string FireVfx = "vfx/vfx_fire_burst";

    /// <summary>雷电命中特效。</summary>
    public const string ThunderVfx = "vfx/vfx_attack_lightning";

    // ===== 判断 =====

    /// <summary>
    /// 是否火焰伤害。
    /// </summary>
    /// <param name="props">伤害属性。</param>
    /// <returns>带火焰标志时为 true。</returns>
    public static bool IsFire(this ValueProp props)
    {
        return (props & Fire) != 0;
    }

    /// <summary>
    /// 是否雷电伤害。
    /// </summary>
    /// <param name="props">伤害属性。</param>
    /// <returns>带雷电标志时为 true。</returns>
    public static bool IsThunder(this ValueProp props)
    {
        return (props & Thunder) != 0;
    }

    /// <summary>
    /// 是否装备造成的伤害。
    /// </summary>
    /// <param name="props">伤害属性。</param>
    /// <returns>带装备标志时为 true。</returns>
    public static bool IsEquipment(this ValueProp props)
    {
        return (props & Equipment) != 0;
    }

    // ===== 攻击命令扩展 =====

    /// <summary>
    /// 把攻击改为穿刺（无视格挡）。
    /// </summary>
    /// <param name="command">攻击命令。</param>
    /// <returns>同一命令，便于链式调用。</returns>
    public static AttackCommand AsPierce(this AttackCommand command)
    {
        command.DamageProps |= ValueProp.Unblockable;
        return command;
    }

    /// <summary>
    /// 把攻击标记为火焰伤害。
    /// </summary>
    /// <param name="command">攻击命令。</param>
    /// <returns>同一命令，便于链式调用。</returns>
    public static AttackCommand AsFire(this AttackCommand command)
    {
        command.DamageProps |= Fire;
        return command;
    }

    /// <summary>
    /// 把攻击标记为雷电伤害。
    /// </summary>
    /// <param name="command">攻击命令。</param>
    /// <returns>同一命令，便于链式调用。</returns>
    public static AttackCommand AsThunder(this AttackCommand command)
    {
        command.DamageProps |= Thunder;
        return command;
    }
}
