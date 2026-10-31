using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 女骑士的自定义关键词。值由 BaseLib 在 ModelDb 初始化前生成，本地化键为 FULLMOONKNIGHT-名称。
/// 穿刺、火焰、雷电、夺取、临时只作悬停说明，不加进牌的关键词列表。
/// </summary>
public static class KnightKeywords
{
    /// <summary>装备牌类型栏文字的本地化键（card_keywords 表）。</summary>
    public const string EquipTitleKey = "FULLMOONKNIGHT-EQUIP.title";

    /// <summary>反制牌类型栏文字的本地化键（card_keywords 表）。</summary>
    public const string CounterTitleKey = "FULLMOONKNIGHT-COUNTER.title";

    /// <summary>装备：打出后整场战斗生效的能力牌，同名可叠加。</summary>
    [CustomEnum("EQUIP")]
    public static CardKeyword Equip;

    /// <summary>反制：布置后在敌人执行符合条件的行动时使其无效。</summary>
    [CustomEnum("COUNTER")]
    public static CardKeyword Counter;

    /// <summary>穿刺：无视格挡的伤害。</summary>
    [CustomEnum("PIERCE")]
    public static CardKeyword Pierce;

    /// <summary>火焰：带火焰属性的伤害。</summary>
    [CustomEnum("FIRE")]
    public static CardKeyword Fire;

    /// <summary>雷电：带雷电属性的伤害。</summary>
    [CustomEnum("THUNDER")]
    public static CardKeyword Thunder;

    /// <summary>夺取：敌人行动的效果改为作用于自己。</summary>
    [CustomEnum("STEAL")]
    public static CardKeyword Steal;

    /// <summary>临时：本场战斗生成的牌，打出后消耗，战斗结束消失。</summary>
    [CustomEnum("TEMPORARY")]
    public static CardKeyword Temporary;
}
