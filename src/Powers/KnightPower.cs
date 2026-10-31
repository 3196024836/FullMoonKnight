using BaseLib.Abstracts;

namespace FullMoonKnight.Powers;

/// <summary>
/// 女骑士能力基类：图标从模组包读取。
/// 小图标 images/powers/{基名}.png（64 像素），大图标 images/powers/big/{基名}.png（256 像素）。
/// </summary>
public abstract class KnightPower : CustomPowerModel
{
    /// <summary>图标文件基名。</summary>
    private string IconName => KnightAssets.BaseName(Id.Entry);

    /// <inheritdoc />
    public override string CustomPackedIconPath => KnightAssets.Image($"powers/{IconName}.png");

    /// <inheritdoc />
    public override string CustomBigIconPath => KnightAssets.Image($"powers/big/{IconName}.png");
}
