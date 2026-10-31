namespace FullMoonKnight;

/// <summary>
/// 模组资源路径。所有资源打包在 res://FullMoonKnight/ 下。
/// </summary>
public static class KnightAssets
{
    /// <summary>
    /// 图片路径。
    /// </summary>
    /// <param name="relativePath">相对 images/ 的路径，例如 relics/knight_oath.png。</param>
    /// <returns>res:// 完整路径。</returns>
    public static string Image(string relativePath)
    {
        return KnightMod.ResRoot + "images/" + relativePath;
    }

    /// <summary>
    /// 内容的资源基名：ID 去掉模组前缀后的小写形式，例如 FULLMOONKNIGHT-KNIGHT_OATH 对应 knight_oath。
    /// </summary>
    /// <param name="entry">内容 ID 的 Entry。</param>
    /// <returns>资源基名。</returns>
    public static string BaseName(string entry)
    {
        return entry[(entry.IndexOf('-') + 1)..].ToLowerInvariant();
    }

    /// <summary>
    /// 场景路径。
    /// </summary>
    /// <param name="relativePath">相对 scenes/ 的路径，例如 knight_visuals.tscn。</param>
    /// <returns>res:// 完整路径。</returns>
    public static string Scene(string relativePath)
    {
        return KnightMod.ResRoot + "scenes/" + relativePath;
    }
}
