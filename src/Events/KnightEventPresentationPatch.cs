using System.Runtime.CompilerServices;

using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

using FullMoonKnight.Events.Panels;

namespace FullMoonKnight.Events;

/// <summary>
/// 月圆之夜事件的展示：把统一样式的选择面板挂到原生事件布局上，并跟随原生选项刷新。
/// 原生布局照常创建和运行（选项、同步、存档都走原生），只是看不见。其他事件不受影响。
/// </summary>
[HarmonyPatch]
public static class KnightEventPresentationPatch
{
    /// <summary>每个原生布局上挂着的面板。</summary>
    private static readonly ConditionalWeakTable<NEventLayout, EventChoicePanel> Panels = new();

    /// <summary>
    /// 原生布局拿到事件后挂上选择面板。
    /// </summary>
    /// <param name="__instance">原生事件布局。</param>
    /// <param name="eventModel">当前事件。</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
    public static void AttachPanel(NEventLayout __instance, EventModel eventModel)
    {
        if (eventModel is not KnightEvent knightEvent || Panels.TryGetValue(__instance, out _))
        {
            return;
        }
        EventChoicePanel panel = knightEvent.CreatePanel();
        panel.Attach(__instance, eventModel);
        Panels.Add(__instance, panel);
    }

    /// <summary>
    /// 原生布局换了一批选项，通知面板刷新。
    /// </summary>
    /// <param name="__instance">原生事件布局。</param>
    /// <param name="options">新的选项。</param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.AddOptions))]
    public static void RefreshPanel(NEventLayout __instance, IEnumerable<EventOption> options)
    {
        if (Panels.TryGetValue(__instance, out EventChoicePanel? panel))
        {
            panel.OnOptions(options.ToList());
        }
    }
}
