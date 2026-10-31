using MegaCrit.Sts2.Core.Runs;

namespace FullMoonKnight.Events.Data;

/// <summary>
/// 原作数值换算到尖塔：生命 ×4、伤害 ×2、金币 ×2.5（四舍五入到 5）。
/// </summary>
public static class FullMoonNumbers
{
    /// <summary>生命、回复、生命上限的倍数。</summary>
    public const int HpScale = 4;

    /// <summary>
    /// 原作章节：第 1/2/3 幕对应第 1/2/3 章，更多幕按第 3 章。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>1～3。</returns>
    public static int Chapter(IRunState runState)
    {
        return Math.Clamp(runState.CurrentActIndex + 1, 1, 3);
    }

    /// <summary>
    /// 原作生命数值换算。
    /// </summary>
    /// <param name="original">原作数值。</param>
    /// <returns>模组数值。</returns>
    public static int Hp(int original)
    {
        return original * HpScale;
    }

    /// <summary>
    /// 原作金币换算：×2.5 后四舍五入到 5 的倍数（0 仍是 0，正数至少 5）。
    /// </summary>
    /// <param name="original">原作金币。</param>
    /// <returns>模组金币。</returns>
    public static int Gold(int original)
    {
        if (original <= 0)
        {
            return 0;
        }
        // 2.5 倍后的值，乘 2 避免小数：original × 5 / 2
        int halves = original * 5;
        // 四舍五入到 5：按 10 个"半金币"一档
        int rounded = (halves + 5) / 10 * 5;
        return Math.Max(5, rounded);
    }

    /// <summary>
    /// 带凋零乐章时加价 50%（原作效果 54：价格 × 150 / 100，向下取整），再换算。
    /// </summary>
    /// <param name="original">原作价格。</param>
    /// <param name="withered">是否有凋零乐章。</param>
    /// <returns>模组价格。</returns>
    public static int Price(int original, bool withered)
    {
        return Gold(withered ? original * 150 / 100 : original);
    }
}
