using Godot;

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

using FullMoonKnight.Cards;
using FullMoonKnight.Relics;
using FullMoonKnight.Visuals;

namespace FullMoonKnight.Character;

/// <summary>
/// 女骑士角色：80 生命，每回合 1 点能量（月圆行动点），初始遗物满月。
/// </summary>
public sealed class Knight : PlaceholderCharacterModel
{
    // ===== 基础属性 =====

    /// <summary>主题色：月光银蓝。</summary>
    public static readonly Color ThemeColor = new("8FB3E0");

    /// <summary>描边色：深夜蓝。</summary>
    public static readonly Color OutlineColor = new("1E2A4AFF");

    /// <summary>费用和能量数字的描边色：深棕，压在原作沙漏（黑框黄沙）上更清楚。</summary>
    public static readonly Color CostOutlineColor = new("2B1A0CFF");

    /// <summary>能量大图标：原作行动点沙漏。</summary>
    public static readonly string EnergyIconPath = KnightAssets.Image("ui/energy.png");

    /// <summary>描述文字里的能量小图标。</summary>
    public static readonly string TextEnergyIconPath = KnightAssets.Image("ui/energy_text.png");

    // ===== 外观 =====

    /// <inheritdoc />
    public override string CustomVisualPath => KnightAssets.Scene("creature_visuals/knight.tscn");

    /// <inheritdoc />
    public override string CustomRestSiteAnimPath => KnightAssets.Scene("rest_site/knight_rest_site.tscn");

    /// <inheritdoc />
    public override string CustomMerchantAnimPath => KnightAssets.Scene("merchant/knight_merchant.tscn");

    /// <inheritdoc />
    public override string CustomTrailPath => KnightAssets.Scene("vfx/card_trail_knight.tscn");

    /// <inheritdoc />
    public override string CustomEnergyCounterPath => KnightAssets.Scene("energy/knight_energy_counter.tscn");

    /// <inheritdoc />
    public override string CustomIconPath => KnightAssets.Scene("ui/knight_icon.tscn");

    /// <inheritdoc />
    public override string CustomIconTexturePath => KnightAssets.Image("ui/top_icon.png");

    /// <inheritdoc />
    public override string CustomIconOutlineTexturePath => KnightAssets.Image("ui/top_icon_outline.png");

    /// <inheritdoc />
    public override string CustomMapMarkerPath => KnightAssets.Image("ui/map_marker.png");

    /// <inheritdoc />
    public override string CustomCharacterSelectBg => KnightAssets.Scene("char_select/knight_char_select_bg.tscn");

    /// <inheritdoc />
    public override string CustomCharacterSelectIconPath => KnightAssets.Image("ui/char_select.png");

    /// <inheritdoc />
    public override string CustomCharacterSelectLockedIconPath => KnightAssets.Image("ui/char_select_locked.png");

    /// <inheritdoc />
    public override string CustomArmPointingTexturePath => KnightAssets.Image("ui/hands/point.png");

    /// <inheritdoc />
    public override string CustomArmRockTexturePath => KnightAssets.Image("ui/hands/rock.png");

    /// <inheritdoc />
    public override string CustomArmPaperTexturePath => KnightAssets.Image("ui/hands/paper.png");

    /// <inheritdoc />
    public override string CustomArmScissorsTexturePath => KnightAssets.Image("ui/hands/scissors.png");

    /// <inheritdoc />
    public override string CustomAttackSfx => KnightAudio.Attack;

    /// <inheritdoc />
    public override string CustomCastSfx => KnightAudio.Cast;

    /// <inheritdoc />
    public override string CustomDeathSfx => KnightAudio.Death;

    /// <inheritdoc />
    public override Color NameColor => ThemeColor;

    /// <inheritdoc />
    public override CharacterGender Gender => CharacterGender.Feminine;

    /// <inheritdoc />
    public override int StartingHp => 80;

    /// <inheritdoc />
    public override int MaxEnergy => 1;

    /// <inheritdoc />
    public override Color EnergyLabelOutlineColor => CostOutlineColor;

    /// <inheritdoc />
    public override Color DialogueColor => new("1E2A4A");

    /// <inheritdoc />
    public override VfxColor SpeechBubbleColor => VfxColor.Blue;

    /// <inheritdoc />
    public override Color MapDrawingColor => new("3B5B92");

    /// <inheritdoc />
    public override Color RemoteTargetingLineColor => new("8FB3E0FF");

    /// <inheritdoc />
    public override Color RemoteTargetingLineOutline => OutlineColor;

    // ===== 卡池与初始物品 =====

    /// <inheritdoc />
    public override CardPoolModel CardPool => ModelDb.CardPool<KnightCardPool>();

    /// <inheritdoc />
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<KnightRelicPool>();

    /// <inheritdoc />
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<KnightPotionPool>();

    /// <inheritdoc />
    public override IEnumerable<CardModel> StartingDeck => StartingDeckSpec.Select(entry => entry.Card);

    /// <summary>初始牌组明细：牌和是否预先升级。由 StartingDeckPatch 按此生成牌组。</summary>
    public static IReadOnlyList<(CardModel Card, bool Upgraded)> StartingDeckSpec => BuildStartingDeck();

    /// <inheritdoc />
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<KnightOath>()];

    // ===== 方法 =====

    /// <summary>
    /// 组装初始牌组明细：打击×3、打击+×1、防御×3、穿刺攻击、短剑、处决。
    /// 原作没有防御牌。满月规则下 1 级只能保留 2 张手牌，前期更吃防守，按用户要求带 3 张防御。
    /// </summary>
    /// <returns>规范牌模型与是否升级。</returns>
    private static List<(CardModel Card, bool Upgraded)> BuildStartingDeck()
    {
        // 初始牌组
        List<(CardModel Card, bool Upgraded)> deck = [];
        for (int i = 0; i < 3; i++)
        {
            deck.Add((ModelDb.Card<Strike>(), false));
        }
        deck.Add((ModelDb.Card<Strike>(), true));
        for (int i = 0; i < 3; i++)
        {
            deck.Add((ModelDb.Card<Defend>(), false));
        }
        deck.Add((ModelDb.Card<PiercingStrike>(), false));
        deck.Add((ModelDb.Card<Shortsword>(), false));
        deck.Add((ModelDb.Card<Execute>(), false));
        return deck;
    }
}
