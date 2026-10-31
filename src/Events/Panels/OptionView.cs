using MegaCrit.Sts2.Core.Models;

namespace FullMoonKnight.Events.Panels;

/// <summary>
/// 选项在事件面板上的展示信息（事件给选项附上，<see cref="EventChoicePanel" /> 按它画选项）。不含 Godot 类型，测试里也能建。
/// 例：商店的购买选项 = { Card = 卡, Price = 38, Slot = "1" }；药剂室 = { Sprite = "icon_shengming", Price = 0 }。
/// </summary>
public sealed class OptionView
{
    /// <summary>默认的价格单位：金币（原作贴图名）。</summary>
    public const string GoldIcon = "icon_coin";

    /// <summary>展示的卡牌（画成尖塔卡牌）。</summary>
    public CardModel? Card { get; init; }

    /// <summary>展示的祝福（取它的图标）。</summary>
    public RelicModel? Relic { get; init; }

    /// <summary>原作贴图名（event_ui 目录下，不含扩展名；没有卡牌和祝福时作为图标；都没有时用事件插图）。</summary>
    public string? Sprite { get; init; }

    /// <summary>显示的名字（BBCode）；为空时用卡名、祝福名或选项标题。</summary>
    public string? Name { get; init; }

    /// <summary>显示的说明（BBCode）；为空时用祝福说明或选项说明。</summary>
    public string? Description { get; init; }

    /// <summary>价格：-1 不显示，0 显示"免费"。</summary>
    public int Price { get; init; } = -1;

    /// <summary>价格单位的原作贴图名：默认 <see cref="GoldIcon" />，粉尘为 fengcheng__。</summary>
    public string PriceIcon { get; init; } = GoldIcon;

    /// <summary>排序用的格子号（"0"/"1"/"2"，买走的位置留空也不影响其余顺序）；不是数字时按选项顺序。</summary>
    public string? Slot { get; init; }

    /// <summary>祝福品质（原作底框序号：0 普通、1 稀有、2 负面）；-1 为普通选项。决定选项牌的边框颜色。</summary>
    public int Frame { get; init; } = -1;
}
