using Godot;

using BaseLib.Abstracts;

namespace FullMoonKnight.Character;

/// <summary>
/// 女骑士卡池。卡牌通过 [Pool(typeof(KnightCardPool))] 自动加入。
/// </summary>
public sealed class KnightCardPool : CustomCardPoolModel
{
    /// <inheritdoc />
    public override string Title => "knight";

    /// <inheritdoc />
    public override Color DeckEntryCardColor => Knight.ThemeColor;

    /// <inheritdoc />
    public override Color EnergyOutlineColor => Knight.CostOutlineColor;

    /// <inheritdoc />
    public override bool IsColorless => false;

    /// <inheritdoc />
    public override Color ShaderColor => new("5A7FB8");

    /// <summary>能量图标（费用圈等处），用原作行动点沙漏。</summary>
    public override string BigEnergyIconPath => Knight.EnergyIconPath;

    /// <summary>描述文字里的能量图标。</summary>
    public override string TextEnergyIconPath => Knight.TextEnergyIconPath;
}

/// <summary>
/// 女骑士遗物池（经典祝福）。只有女骑士玩家会刷到。
/// </summary>
public sealed class KnightRelicPool : CustomRelicPoolModel
{
    /// <inheritdoc />
    public override Color LabOutlineColor => Knight.ThemeColor;

    /// <summary>能量图标。</summary>
    public override string BigEnergyIconPath => Knight.EnergyIconPath;

    /// <summary>描述文字里的能量图标。</summary>
    public override string TextEnergyIconPath => Knight.TextEnergyIconPath;
}

/// <summary>
/// 女骑士事件遗物池：事件祝福、限次祝福、负面乐章、皓月。
/// 作为共享池登记以便图鉴收录，但不参与随机奖励（奖励只从通用池和角色池抽取）。
/// </summary>
public sealed class KnightEventRelicPool : CustomRelicPoolModel
{
    /// <inheritdoc />
    public override bool IsShared => true;

    /// <inheritdoc />
    public override Color LabOutlineColor => Knight.ThemeColor;
}

/// <summary>
/// 女骑士药水池：为空，只用通用药水（D39）。
/// </summary>
public sealed class KnightPotionPool : CustomPotionPoolModel
{
    /// <inheritdoc />
    public override Color LabOutlineColor => Knight.ThemeColor;
}
