using Godot;

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 女骑士卡牌基类：卡图从模组包读取（images/cards/{基名}.png）；可替换牌面类型栏文字（装备、反制）；
/// 牌面用月圆风格牌框（见 <see cref="Visuals.KnightCardFace" />）。
/// </summary>
public abstract class KnightCard : CustomCardModel, ICustomTypeTextCard
{
    /// <summary>
    /// 创建卡牌。
    /// </summary>
    /// <param name="cost">能量费用。</param>
    /// <param name="type">类型。</param>
    /// <param name="rarity">稀有度。</param>
    /// <param name="target">目标类型。</param>
    protected KnightCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }

    /// <inheritdoc />
    /// <remarks>先祖牌走原生先祖牌面，用铺满整张牌的卡图（make_card_art.py 生成）。</remarks>
    public override string CustomPortraitPath => KnightAssets.Image(Rarity == CardRarity.Ancient
        ? $"cards/ancient/{KnightAssets.BaseName(Id.Entry)}.png"
        : $"cards/{KnightAssets.BaseName(Id.Entry)}.png");

    /// <summary>
    /// 由女骑士自己的效果在战斗中生成（比如精品收藏的临时装备）。
    /// 这类装备按卡牌设计免费；原版效果生成的装备费用为 1（见 <see cref="GeneratedEquipmentCostPatch" />）。
    /// </summary>
    public bool GeneratedByKnight { get; set; }

    /// <summary>类型栏文字的本地化键（card_keywords 表）；为空时用原生类型名。</summary>
    protected virtual string? TypeTextKey => null;

    /// <summary>牌框不做色相变换（牌框图片自带底色）。所有女骑士牌共用。</summary>
    private static readonly Material PlainFrameMaterial = new CanvasItemMaterial();

    /// <summary>牌面种类，决定牌框底色：诅咒按稀有度，反制、装备按类型栏文字，其余按卡牌类型。</summary>
    public string FaceKind => Rarity == CardRarity.Curse ? "curse" : TypeTextKey switch
    {
        KnightKeywords.CounterTitleKey => "counter",
        KnightKeywords.EquipTitleKey => "equip",
        _ => Type switch
        {
            CardType.Attack => "attack",
            CardType.Power => "equip",
            CardType.Curse => "curse",
            CardType.Status => "status",
            _ => "skill",
        },
    };

    /// <inheritdoc />
    public override Texture2D? CustomFrame => ResourceLoader.Load<Texture2D>(
        KnightAssets.Image($"card_face/frame_{FaceKind}.png"), null, ResourceLoader.CacheMode.Reuse);

    /// <inheritdoc />
    public override Material? CreateCustomFrameMaterial => PlainFrameMaterial;

    /// <summary>
    /// 类型栏文字替换。
    /// </summary>
    /// <returns>替换文字；不替换时为空序列。</returns>
    public IEnumerable<LocString> GetTypeModifiers()
    {
        return TypeTextKey == null ? [] : [new LocString("card_keywords", TypeTextKey)];
    }
}
