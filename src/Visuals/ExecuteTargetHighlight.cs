using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Visuals;

/// <summary>
/// 处决瞄准提示：拿处决瞄准斩杀线以下的敌人时，敌人身体一闪一闪地泛红，提示这一下会造成大量伤害。
/// 记下开始瞄准的牌，悬停到敌人时判断；移开、瞄准结束时恢复颜色。只影响本地显示。
/// </summary>
[HarmonyPatch]
public static class ExecuteTargetHighlight
{
    /// <summary>泛红时的颜色。</summary>
    private static readonly Color PulseColor = new(1f, 0.35f, 0.35f);

    /// <summary>一次闪烁的半周期（秒）。</summary>
    private const float PulseSeconds = 0.35f;

    /// <summary>正在瞄准的牌；不是从牌开始的瞄准时为空。</summary>
    private static CardModel? _targetingCard;

    /// <summary>正在泛红的敌人和它的闪烁动画。</summary>
    private static (NCreature Creature, Tween Tween)? _pulse;

    /// <summary>
    /// 从牌开始瞄准时记下这张牌。
    /// </summary>
    /// <param name="control">瞄准起点（出牌时是卡牌节点）。</param>
    [HarmonyPatch(typeof(NTargetManager), nameof(NTargetManager.StartTargeting),
        [typeof(TargetType), typeof(Control), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>)])]
    [HarmonyPostfix]
    public static void RememberCard(Control control)
    {
        _targetingCard = (control as NCard)?.Model;
    }

    /// <summary>
    /// 悬停到敌人：处决且目标在斩杀线以下时开始泛红。
    /// </summary>
    /// <param name="creature">悬停的生物节点。</param>
    [HarmonyPatch(typeof(NTargetManager), "OnCreatureHovered")]
    [HarmonyPostfix]
    public static void OnHovered(NCreature creature)
    {
        StopPulse();
        if (_targetingCard is not Execute || creature.Entity == null || !Execute.IsBelowThreshold(creature.Entity))
        {
            return;
        }
        // 闪烁动画：泛红、恢复，循环
        Tween tween = creature.CreateTween().SetLoops();
        tween.TweenProperty(creature.Visuals, "modulate", PulseColor, PulseSeconds);
        tween.TweenProperty(creature.Visuals, "modulate", Colors.White, PulseSeconds);
        _pulse = (creature, tween);
    }

    /// <summary>
    /// 移开敌人时恢复。
    /// </summary>
    [HarmonyPatch(typeof(NTargetManager), "OnCreatureUnhovered")]
    [HarmonyPostfix]
    public static void OnUnhovered()
    {
        StopPulse();
    }

    /// <summary>
    /// 瞄准结束时恢复并清掉记录的牌。
    /// </summary>
    [HarmonyPatch(typeof(NTargetManager), "FinishTargeting")]
    [HarmonyPostfix]
    public static void OnFinished()
    {
        StopPulse();
        _targetingCard = null;
    }

    /// <summary>
    /// 停止闪烁，恢复敌人原色。
    /// </summary>
    private static void StopPulse()
    {
        if (_pulse is not { } pulse)
        {
            return;
        }
        _pulse = null;
        pulse.Tween.Kill();
        if (GodotObject.IsInstanceValid(pulse.Creature))
        {
            pulse.Creature.Visuals.Modulate = Colors.White;
        }
    }
}
