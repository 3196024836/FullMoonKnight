using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 法力（D35）：每场战斗从 0 开始，由祝福等效果提供初始法力；不自动回复，跨回合保留，战斗结束清零，没有上限。
/// 显示同储君的辉星：能量球左上角，原作法力图标上写数字，为 0 时隐藏。女骑士目前没有消耗法力的牌（D23）。
/// </summary>
public sealed class KnightMana : BasicCustomResource
{
    /// <summary>资源 ID，也是悬停说明的本地化键。</summary>
    public const string ResourceId = "FULLMOONKNIGHT-MANA";

    /// <summary>
    /// 创建法力资源；每回合不重置。
    /// </summary>
    public KnightMana() : base(ResourceId)
    {
    }

    /// <inheritdoc />
    public override string TexturePath => KnightAssets.Image("ui/mana.png");

    /// <inheritdoc />
    public override string IconPath => KnightAssets.Image("ui/mana_icon.png");

    /// <summary>数字描边颜色（原作法力的蓝色）。</summary>
    public override Color MainColor => new("1f4f9e");

    /// <summary>战斗界面里法力显示的大小（像素）。</summary>
    private static readonly Vector2 DisplaySize = new(64f, 64f);

    /// <summary>
    /// 战斗界面里的法力显示：用 BaseLib 的资源显示，挂到能量球下（辉星的父节点），放在能量球左上角，
    /// 跟着能量球一起入场。卡牌费用处不显示（女骑士的牌不花法力）。
    /// </summary>
    /// <typeparam name="T">资源类型。</typeparam>
    public override void RegisterResourceVisuals<T>()
    {
        ExtraCombatUi.RegisterCombatUiElement((ui, player, _) =>
        {
            if (player.PlayerCombatState == null
                || CustomResources<T>.Get(player.PlayerCombatState) is not KnightMana resource)
            {
                return;
            }
            Texture2D texture = PreloadManager.Cache.GetTexture2D(resource.TexturePath);
            NAdditionalResourceDisplay display = NAdditionalResourceDisplay.Create<T>(player, resource, texture);
            Control star = ui._starCounter;
            ui.AddChild(display);
            // 辉星这时还没挂到能量球下；等它挂过去后，法力显示也挂到能量球下，放在左上角
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(display) || !GodotObject.IsInstanceValid(star)
                    || star.GetParent() is not Control parent)
                {
                    return;
                }
                display.Reparent(parent, false);
                display.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                display.Size = DisplaySize;
                // 中心放在能量球外框的左上角，不挡住能量数字
                display.Position = new Vector2(parent.Size.X * 0.12f, parent.Size.Y * 0.12f) - DisplaySize * 0.5f;
            }).CallDeferred();
        });
    }

    /// <summary>
    /// 读取玩家本场战斗的法力；不在战斗中时为空。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>法力资源。</returns>
    public static KnightMana? Of(Player player)
    {
        return CustomResources<KnightMana>.TryGet(player.PlayerCombatState, out KnightMana? mana) ? mana : null;
    }

    /// <summary>
    /// 给玩家增加法力（负数为减少，不低于 0）。不在战斗中时忽略。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="amount">变化量。</param>
    public static void Gain(Player player, int amount)
    {
        if (Of(player) is { } mana)
        {
            mana.ModifyAmount(Math.Max(amount, -mana.Amount));
        }
    }
}
