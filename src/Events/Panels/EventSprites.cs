using Godot;

namespace FullMoonKnight.Events.Panels;

/// <summary>
/// 原作事件贴图（images/event_ui/，由 tools/stage_assets.py 从 assets/original/event_ui/ 复制）：
/// 选项图标、价格单位，以及面板用的祝福卡底、品质圆框、标题牌、按钮等。
/// 例：EventSprites.Get("icon_shengming") 得到原作的生命图标；EventSprites.Get("zhufudi") 得到祝福卡底。
/// </summary>
public static class EventSprites
{
    /// <summary>贴图目录。</summary>
    private static readonly string Root = KnightMod.ResRoot + "images/event_ui/";

    /// <summary>
    /// 读取一张原作贴图；没有时为空。
    /// </summary>
    /// <param name="name">贴图文件名（不含扩展名）。</param>
    /// <returns>贴图。</returns>
    public static Texture2D? Get(string name)
    {
        string path = Root + name + ".png";
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }
}
