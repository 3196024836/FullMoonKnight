using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Visuals;

/// <summary>
/// 女骑士牌面：月圆经典牌框（牌框图由 <see cref="KnightCard.CustomFrame" /> 提供），卡图放进圆窗，
/// 隐藏原生卡图边框和标题横幅，类型文字移到牌框底部的椭圆里（颜色表示稀有度），标题和描述用原作字体。
/// 费用图标是原作行动牌的小沙漏，0 费牌不显示。
/// 卡牌节点会在不同卡牌间复用，所以每次换牌都按是否女骑士牌重新设置，非女骑士牌恢复原生布局。
/// </summary>
[HarmonyPatch]
public static class KnightCardFace
{
    // ===== 布局（相对卡牌中心，牌框显示为 300x422） =====

    /// <summary>原生卡图位置（左、上、右、下），来自 card.tscn。</summary>
    private static readonly Vector4 VanillaPortrait = new(-125, -168, 125, 22);

    /// <summary>女骑士卡图位置：牌框圆窗中心 (0, -53.5)，边长 223（光晕直径略大于圆窗）。</summary>
    private static readonly Vector4 KnightPortrait = new(-111.5f, -165, 111.5f, 58);

    /// <summary>原生描述位置。</summary>
    private static readonly Vector4 VanillaDescription = new(-122, 37, 121, 173);

    /// <summary>女骑士描述位置：牌框下方文字框内（原来底部给稀有度星留的位置也给描述用）。</summary>
    private static readonly Vector4 KnightDescription = new(-109, 52, 109, 162);

    /// <summary>原生类型牌位置的 Y。</summary>
    private const float VanillaPlaqueY = 1;

    /// <summary>女骑士类型牌位置的 Y：牌框底部椭圆中心 184.5，牌高 37。</summary>
    private const float KnightPlaqueY = 166;

    /// <summary>类型文字颜色（原作标题的米白）。</summary>
    private static readonly Color TypeTextColor = new(0.9f, 0.88f, 0.8f);

    /// <summary>原生类型文字颜色，来自 card.tscn。</summary>
    private static readonly Color VanillaTypeTextColor = new(0, 0, 0, 0.752941f);

    /// <summary>罕见牌类型文字颜色（月光蓝）。</summary>
    private static readonly Color UncommonTypeColor = new(0.6f, 0.82f, 1f);

    /// <summary>稀有牌类型文字颜色（金）。</summary>
    private static readonly Color RareTypeColor = new(1f, 0.8f, 0.35f);

    /// <summary>卡牌节点的费用图标字段。</summary>
    private static readonly AccessTools.FieldRef<NCard, TextureRect> EnergyIconRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_energyIcon");

    /// <summary>描述文字要替换的字体项。</summary>
    private static readonly string[] DescriptionFonts = ["normal_font", "bold_font"];

    /// <summary>描述行距：原生（card.tscn）和女骑士牌。</summary>
    private const int VanillaLineSeparation = -3, KnightLineSeparation = 1;

    /// <summary>节点元数据键前缀：保存原生字体，换回原生牌时恢复。</summary>
    private const string NativeFontMeta = "fmk_native_font_";

    /// <summary>节点复用时保存原生远古卡图的拉伸模式。</summary>
    private const string NativeAncientStretchMeta = "fmk_native_ancient_stretch";

    // ===== 字体 =====

    /// <summary>原作正文字体（缺字时回退到原生字体）；首次使用时加载。</summary>
    private static FontFile? _classicFont;

    // ===== 补丁 =====

    /// <summary>
    /// 换牌后按牌的归属设置牌面。
    /// </summary>
    /// <param name="__instance">卡牌节点。</param>
    [HarmonyPatch(typeof(NCard), "Reload")]
    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (!__instance.IsNodeReady() || __instance.Model == null)
        {
            return;
        }
        // 是否用女骑士牌面（远古稀有度走原生的远古牌面）
        KnightCard? card = __instance.Model is KnightCard knight && knight.Rarity != CardRarity.Ancient ? knight : null;
        bool active = card != null;
        // 卡面节点
        TextureRect portrait = __instance.GetNode<TextureRect>("%Portrait");
        TextureRect ancientPortrait = __instance.GetNode<TextureRect>("%AncientPortrait");
        if (__instance.Model is KnightCard { Rarity: CardRarity.Ancient })
        {
            if (!ancientPortrait.HasMeta(NativeAncientStretchMeta))
            {
                ancientPortrait.SetMeta(NativeAncientStretchMeta, (int)ancientPortrait.StretchMode);
            }
            ancientPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        }
        else if (ancientPortrait.HasMeta(NativeAncientStretchMeta))
        {
            ancientPortrait.StretchMode = (TextureRect.StretchModeEnum)ancientPortrait.GetMeta(NativeAncientStretchMeta).AsInt32();
            ancientPortrait.RemoveMeta(NativeAncientStretchMeta);
        }
        Control description = __instance.GetNode<Control>("%DescriptionLabel");
        Control title = __instance.GetNode<Control>("%TitleLabel");
        NinePatchRect plaque = __instance.GetNode<NinePatchRect>("%TypePlaque");

        SetOffsets(portrait, active ? KnightPortrait : VanillaPortrait);
        SetOffsets(description, active ? KnightDescription : VanillaDescription);
        plaque.Position = new Vector2(plaque.Position.X, active ? KnightPlaqueY : VanillaPlaqueY);
        // 类型牌只留文字（浅色），底色由牌框椭圆提供
        plaque.SelfModulate = active ? Colors.Transparent : Colors.White;
        Label typeLabel = plaque.GetNode<Label>("TypeLabel");
        typeLabel.AddThemeColorOverride("font_color", card != null ? RarityColor(card) : VanillaTypeTextColor);
        SetFonts(typeLabel, ["font"], active);
        if (active)
        {
            __instance.GetNode<CanvasItem>("%PortraitBorder").Visible = false;
            __instance.GetNode<CanvasItem>("%TitleBanner").Visible = false;
        }
        SetFonts(title, ["font"], active);
        SetFonts(description, DescriptionFonts, active);
        // 原作字体行高较大，原生的负行距会让行重叠
        description.AddThemeConstantOverride("line_separation", active ? KnightLineSeparation : VanillaLineSeparation);
    }

    // ===== 辅助 =====

    /// <summary>
    /// 设置控件四边偏移。
    /// </summary>
    /// <param name="control">控件。</param>
    /// <param name="offsets">左、上、右、下。</param>
    private static void SetOffsets(Control control, Vector4 offsets)
    {
        control.OffsetLeft = offsets.X;
        control.OffsetTop = offsets.Y;
        control.OffsetRight = offsets.Z;
        control.OffsetBottom = offsets.W;
    }

    /// <summary>
    /// 换成原作字体，或换回原生字体（第一次替换前记在节点元数据里）。
    /// </summary>
    /// <param name="control">文字控件。</param>
    /// <param name="names">字体主题项。</param>
    /// <param name="active">是否换成原作字体。</param>
    private static void SetFonts(Control control, string[] names, bool active)
    {
        foreach (string name in names)
        {
            // 元数据键
            string key = NativeFontMeta + name;
            if (!control.HasMeta(key))
            {
                if (!active)
                {
                    continue;
                }
                control.SetMeta(key, control.GetThemeFont(name));
            }
            // 原生字体
            Font native = control.GetMeta(key).As<Font>();
            control.AddThemeFontOverride(name, active ? ClassicFont(native) : native);
        }
    }

    /// <summary>
    /// 原作正文字体，从模组包按字节加载。
    /// </summary>
    /// <param name="fallback">缺字时的回退字体（原生字体）。</param>
    /// <returns>字体。</returns>
    private static FontFile ClassicFont(Font fallback)
    {
        if (_classicFont == null)
        {
            _classicFont = new FontFile
            {
                Data = Godot.FileAccess.GetFileAsBytes(KnightMod.ResRoot + "fonts/classic_text.ttf"),
                Fallbacks = [fallback],
            };
        }
        return _classicFont;
    }

    /// <summary>
    /// 类型文字颜色：按稀有度区分（原版用标题横幅颜色区分，女骑士牌面隐藏了横幅）。
    /// </summary>
    /// <param name="card">女骑士牌。</param>
    /// <returns>颜色。</returns>
    private static Color RarityColor(KnightCard card)
    {
        return card.Rarity switch
        {
            CardRarity.Uncommon => UncommonTypeColor,
            CardRarity.Rare => RareTypeColor,
            _ => TypeTextColor,
        };
    }

    // ===== 费用 =====

    /// <summary>
    /// 女骑士的 0 费牌不显示费用图标（原作行动牌只有花行动点的才画沙漏）。
    /// 费用被临时改成非 0 时图标会重新出现。
    /// </summary>
    /// <param name="__instance">卡牌节点。</param>
    [HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
    [HarmonyPostfix]
    public static void HideZeroCost(NCard __instance)
    {
        if (__instance.Model is not KnightCard card || card.Rarity == CardRarity.Ancient || card.EnergyCost.CostsX
            || card.EnergyCost.GetWithModifiers(CostModifiers.All) != 0)
        {
            return;
        }
        EnergyIconRef(__instance).Visible = false;
    }
}
