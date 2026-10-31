using MegaCrit.Sts2.Core.Localization;

namespace FullMoonKnight.Events.Panels;

/// <summary>
/// 原作面板上的通用按钮文字（确认、离开、免费等），取 events 表的 FULLMOONKNIGHT-KNIGHT_EVENT.ui.&lt;键&gt;。
/// 例：PanelText.Get("confirm") 得到"确认"。
/// </summary>
public static class PanelText
{
    /// <summary>文字键前缀。</summary>
    private const string Prefix = "FULLMOONKNIGHT-KNIGHT_EVENT.ui.";

    /// <summary>
    /// 取一条面板文字。
    /// </summary>
    /// <param name="key">键名，例如 confirm。</param>
    /// <returns>格式化后的文字。</returns>
    public static string Get(string key)
    {
        return new LocString("events", Prefix + key).GetFormattedText();
    }
}
