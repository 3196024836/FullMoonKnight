using Godot;

using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace FullMoonKnight.Events.Panels;

/// <summary>
/// 月圆之夜事件的统一选择面板（尖塔"几选一"的形式）：盖在原生事件布局上，所有事件同一套样式。
/// 上方是标题牌和故事文字，中间一排选项：卡牌选项画尖塔卡牌，祝福和普通选项画成原作的祝福卡
/// （卡底、品质圆框里的图标、名字条、说明），价格在下面；底部是离开按钮。贴图全部用原作的（images/event_ui/）。事件结束后显示结果文字和"继续"按钮。
/// 点击最终仍然走原生选项（NEventRoom.OptionButtonClicked），所以联机同步、存档行为不变。
/// 例：仙女祝福 = 三张祝福竖牌；老猫商店 = 三张卡 + 价格 + 离开。
/// </summary>
public sealed class EventChoicePanel
{
    // ===== 常量 =====

    /// <summary>面板节点名（也用来判断布局上是否已经挂了面板）。</summary>
    public const string NodeName = "FullMoonEventPanel";

    /// <summary>设计分辨率。</summary>
    private static readonly Vector2 DesignSize = new(1920, 1080);

    /// <summary>尖塔顶栏（含撕边）占屏幕高度的比例；设计画布放在它下面。</summary>
    private const float TopBarShare = 92f / 1080f;

    /// <summary>设计画布里内容的上沿；上面这段只有插图，可以压在顶栏下面。</summary>
    private const float ContentTop = 90f;

    /// <summary>选项卡的大小（和原作祝福卡底 zhufudi 428×624 同比例）。</summary>
    private static readonly Vector2 TileSize = new(310, 452);

    /// <summary>原作祝福卡（428 宽）到选项卡的缩放，卡内各部分按原作预制体的位置乘它。</summary>
    private const float TileUnit = 310f / 428f;

    /// <summary>选项卡之间的空隙。</summary>
    private const float TileGap = 46;

    /// <summary>一排选项最多占的宽度（超出时整排缩小）。</summary>
    private const float RowWidth = 1840;

    /// <summary>选项卡上沿。</summary>
    private const float RowTop = 414;

    /// <summary>价格底的大小（在选项卡下面）。</summary>
    private static readonly Vector2 PriceSize = new(176, 54);

    /// <summary>底部按钮的大小（原作 396×120 按 2560 宽画布缩到 1920）。</summary>
    private static readonly Vector2 ButtonSize = new(300, 90);

    /// <summary>卡牌在格子里的缩放（卡牌本身平时 0.8、悬停 1.0）。</summary>
    private const float CardScale = 1.28f;

    /// <summary>悬停时选项卡放大的倍数。</summary>
    private const float HoverScale = 1.05f;

    /// <summary>悬停动画秒数。</summary>
    private const double HoverTime = 0.12;

    /// <summary>选中光（faguang）比选项卡四周多出的像素：贴图的亮线在边缘往里 52 像素处，这样亮线正好贴着卡边外侧。</summary>
    private const float GlowPad = 56;

    /// <summary>插图压暗的颜色（压暗后标题、文字和选项更突出）。</summary>
    private static readonly Color IllustrationTint = new(0.55f, 0.55f, 0.58f);

    /// <summary>正文颜色（原作说明文字 #ded8c3）。</summary>
    private static readonly Color TextColor = new("ded8c3");

    /// <summary>标题颜色（原作标题牌上的 #fedd82）。</summary>
    private static readonly Color TitleColor = new("fedd82");

    /// <summary>祝福名字颜色（原作 #cbcab4）。</summary>
    private static readonly Color NameColor = new("cbcab4");

    /// <summary>祝福说明颜色（原作 #928a7f 提亮一些，压在插图上也看得清）。</summary>
    private static readonly Color DescriptionColor = new("b2a995");

    /// <summary>按钮文字颜色（原作 #dfd9c4）。</summary>
    private static readonly Color ButtonColor = new("dfd9c4");

    /// <summary>附加信息（存款、粉尘）颜色。</summary>
    private static readonly Color InfoColor = new(1f, 0.82f, 0.4f);

    /// <summary>价格颜色。</summary>
    private static readonly Color PriceColor = new(1f, 0.85f, 0.35f);

    /// <summary>免费的颜色。</summary>
    private static readonly Color FreeColor = new(0.55f, 0.95f, 0.5f);

    /// <summary>买不起的价格颜色。</summary>
    private static readonly Color PoorColor = new(1f, 0.45f, 0.4f);

    /// <summary>锁定选项的颜色。</summary>
    private static readonly Color LockedColor = new(0.5f, 0.5f, 0.5f);

    /// <summary>选中光的颜色（原作贴图本身是橙金色，这里只控制透明度）。</summary>
    private static readonly Color GlowOn = new(1f, 1f, 1f, 0.95f);

    // ===== 状态 =====

    /// <summary>所属事件。</summary>
    private EventModel _event = null!;

    /// <summary>挂在原生布局上的外层节点。</summary>
    private Control _host = null!;

    /// <summary>设计画布（按窗口缩放）。</summary>
    private Control _design = null!;

    /// <summary>原生描述文字节点（复制它得到尖塔字体和文字特效）。</summary>
    private RichTextLabel? _template;

    /// <summary>标题。</summary>
    private RichTextLabel _title = null!;

    /// <summary>故事文字。</summary>
    private RichTextLabel _story = null!;

    /// <summary>故事文字后面的深色底。</summary>
    private Control _backdrop = null!;

    /// <summary>附加信息（存款、粉尘）。</summary>
    private RichTextLabel _info = null!;

    /// <summary>选项排。</summary>
    private Control _row = null!;

    /// <summary>底部按钮（离开为绿色、继续为红色）。</summary>
    private NinePatchRect _button = null!;

    /// <summary>底部按钮文字。</summary>
    private RichTextLabel _buttonText = null!;

    /// <summary>底部按钮点下去要做的事。</summary>
    private Action? _buttonAction;

    /// <summary>选了一个选项、正在等结果；期间面板不响应点击。</summary>
    private bool _busy;

    // ===== 生命周期 =====

    /// <summary>
    /// 挂到原生事件布局上：隐藏原生内容，铺上插图背景、标题、故事文字、选项排和底部按钮。
    /// </summary>
    /// <param name="layout">原生事件布局。</param>
    /// <param name="eventModel">当前事件。</param>
    public void Attach(NEventLayout layout, EventModel eventModel)
    {
        _event = eventModel;
        _template = layout.GetNodeOrNull<RichTextLabel>("%EventDescription");

        // 原生插图、标题、描述和选项全部隐藏（节点仍在，原生逻辑照常运行）
        foreach (Control child in layout.GetChildren().OfType<Control>())
        {
            child.Visible = false;
        }

        // 外层：全屏插图，整体压暗
        _host = new Control { Name = NodeName, MouseFilter = Control.MouseFilterEnum.Stop };
        layout.AddChild(_host);
        _host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        TextureRect illustration = new()
        {
            Name = "Illustration", Texture = eventModel.CreateInitialPortrait(), MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            SelfModulate = IllustrationTint,
        };
        _host.AddChild(illustration);
        illustration.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // 设计画布：1920×1080，按窗口缩放居中
        _design = new Control { Name = "Design", Size = DesignSize, MouseFilter = Control.MouseFilterEnum.Ignore };
        _host.AddChild(_design);
        _host.Resized += FitDesign;
        BuildHeader();
        _row = new Control { Name = "Choices", MouseFilter = Control.MouseFilterEnum.Ignore };
        _design.AddChild(_row);
        BuildButton();
        FitDesign();
    }

    /// <summary>
    /// 原生布局刷新了选项（进入事件、选完一个选项后）：重画选项排；事件结束时显示结果和"继续"。
    /// </summary>
    /// <param name="options">当前选项。</param>
    public void OnOptions(IReadOnlyList<EventOption> options)
    {
        _busy = false;
        foreach (Node child in _row.GetChildren())
        {
            child.QueueFreeSafely();
        }
        string story = _event.Description?.GetFormattedText() ?? "";

        // 结束：深色底和结果文字放大居中，底部"继续"
        if (_event.IsFinished)
        {
            Place(_backdrop, new Rect2(360, 330, 1200, 320));
            Place(_story, new Rect2(420, 360, 1080, 260));
            SetText(_story, Center(story), 26, 34);
            _info.Visible = false;
            EventOption? proceed = options.FirstOrDefault(o => o.IsProceed);
            ShowButton(proceed == null ? null : PanelText.Get("continue"), "Tsqueren", () => Proceed(proceed!));
            return;
        }

        // 有附加信息时故事文字让出最下一行
        string? info = (_event as IPanelInfo)?.PanelInfo;
        _info.Visible = !string.IsNullOrEmpty(info);
        Place(_backdrop, new Rect2(330, 226, 1260, _info.Visible ? 178 : 160));
        Place(_story, new Rect2(370, 234, 1180, _info.Visible ? 122 : 144));
        SetText(_story, Center(story), 22, 30);
        SetText(_info, Center(info ?? ""), 20, 26);

        // 选项排：离开以外的选项按格子号排好
        List<EventOption> choices = Ordered(options);
        BuildRow(choices);
        EventOption? leave = options.FirstOrDefault(KnightEvent.IsLeave);
        ShowButton(leave == null ? null : leave.Title.GetFormattedText(), "Tsqueren-Green", () => Choose(leave!));
    }

    // ===== 页头与按钮 =====

    /// <summary>
    /// 原作标题牌、深色底上的故事文字和附加信息。
    /// </summary>
    private void BuildHeader()
    {
        // 标题牌 482×111（原作尺寸），画布整体放在尖塔顶栏下面；文字在六边形里面
        Vector2 plaqueSize = new(482, 111);
        Vector2 plaqueAt = new((DesignSize.X - plaqueSize.X) / 2, ContentTop + 8);
        _design.AddChild(NewImage("Plaque", "biaotikuang_MERGE_PUBLIC", new Rect2(plaqueAt, plaqueSize)));
        _title = NewText(_design, "Title", new Rect2(plaqueAt + new Vector2(60, 8), plaqueSize - new Vector2(120, 16)), TitleColor, true);
        SetText(_title, Center($"[b]{_event.Title.GetFormattedText()}[/b]"), 30, 44);
        // 故事文字底：原作的深色圆角底（九宫格 24），半透明
        NinePatchRect backdrop = NewPatch("Backdrop", "di", new Rect2(330, 226, 1260, 160), 24, 24);
        backdrop.Modulate = new Color(1, 1, 1, 0.82f);
        _backdrop = backdrop;
        _design.AddChild(backdrop);
        _story = NewText(_design, "Story", new Rect2(370, 236, 1180, 140), TextColor, true);
        _info = NewText(_design, "Info", new Rect2(560, 358, 800, 38), InfoColor, true);
    }

    /// <summary>
    /// 底部按钮（原作的木板按钮：离开绿色、继续红色），悬停放大变亮。
    /// </summary>
    private void BuildButton()
    {
        _button = NewPatch("Button", "Tsqueren-Green", new Rect2(new Vector2((DesignSize.X - ButtonSize.X) / 2, 960), ButtonSize), 30, 22);
        _button.MouseFilter = Control.MouseFilterEnum.Stop;
        _button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        _button.PivotOffset = ButtonSize / 2;
        _design.AddChild(_button);
        _buttonText = NewText(_button, "Text", new Rect2(20, 4, ButtonSize.X - 40, ButtonSize.Y - 14), ButtonColor, true);
        Hover(_button, () => true);
        _button.GuiInput += input =>
        {
            if (!_busy && IsClick(input))
            {
                _buttonAction?.Invoke();
            }
        };
    }

    /// <summary>
    /// 显示或隐藏底部按钮。
    /// </summary>
    /// <param name="text">按钮文字；为空时隐藏。</param>
    /// <param name="sprite">按钮贴图（Tsqueren 红色、Tsqueren-Green 绿色）。</param>
    /// <param name="onClick">点击后执行。</param>
    private void ShowButton(string? text, string sprite, Action onClick)
    {
        _button.Texture = EventSprites.Get(sprite);
        _button.Visible = text != null;
        _buttonAction = text == null ? null : onClick;
        if (text != null)
        {
            SetText(_buttonText, Center($"[b]{text}[/b]"), 22, 32);
        }
    }

    // ===== 选项排 =====

    /// <summary>
    /// 离开以外的选项，按格子号排序（格子号不是数字时保持原顺序）。
    /// 例：商店卖掉中间那张后剩下格子 0、2，仍然按 0、2 排。
    /// </summary>
    /// <param name="options">当前选项。</param>
    /// <returns>排好的选项。</returns>
    private static List<EventOption> Ordered(IReadOnlyList<EventOption> options)
    {
        List<EventOption> choices = options.Where(o => !KnightEvent.IsLeave(o)).ToList();
        return choices
            .Select((option, index) => (option, key: int.TryParse(KnightEvent.ViewOf(option)?.Slot, out int slot) ? slot : index))
            .OrderBy(pair => pair.key)
            .Select(pair => pair.option)
            .ToList();
    }

    /// <summary>
    /// 一排选项居中摆开；太多时整排缩小。
    /// </summary>
    /// <param name="choices">选项。</param>
    private void BuildRow(List<EventOption> choices)
    {
        int count = choices.Count;
        float width = count * TileSize.X + Math.Max(0, count - 1) * TileGap;
        float scale = Math.Min(1f, RowWidth / Math.Max(width, 1));
        _row.Scale = new Vector2(scale, scale);
        _row.Position = new Vector2((DesignSize.X - width * scale) / 2, RowTop);
        for (int i = 0; i < count; i++)
        {
            Control slot = new()
            {
                Name = $"Choice{i}", MouseFilter = Control.MouseFilterEnum.Ignore, Size = TileSize,
                Position = new Vector2(i * (TileSize.X + TileGap), 0),
            };
            _row.AddChild(slot);
            EventOption option = choices[i];
            OptionView? view = KnightEvent.ViewOf(option);
            if (view?.Card != null)
            {
                BuildCard(slot, option, view.Card);
            }
            else
            {
                BuildTile(slot, option, view);
            }
            BuildPrice(slot, option, view);
        }
    }

    /// <summary>
    /// 卡牌选项：尖塔卡牌（悬停放大、显示说明），点卡选择。
    /// </summary>
    /// <param name="slot">格子。</param>
    /// <param name="option">选项。</param>
    /// <param name="card">卡牌。</param>
    private void BuildCard(Control slot, EventOption option, CardModel card)
    {
        NCard? node = NCard.Create(card);
        NGridCardHolder? holder = node == null ? null : NGridCardHolder.Create(node);
        if (holder == null)
        {
            BuildTile(slot, option, KnightEvent.ViewOf(option));
            return;
        }
        // 卡牌在格子中央；外面套一层放大，卡牌自己的悬停缩放照常
        Control anchor = new()
        {
            Name = "Card", MouseFilter = Control.MouseFilterEnum.Ignore, Position = TileSize / 2,
            Scale = new Vector2(CardScale, CardScale),
        };
        slot.AddChild(anchor);
        anchor.AddChild(holder);
        node!.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        if (option.IsLocked)
        {
            slot.Modulate = LockedColor;
        }
        holder.Pressed += _ => Choose(option);
        // 悬停时卡牌放大，整格和卡牌都提到上层，免得被邻格或自己的价格牌盖住
        holder.MouseEntered += () => slot.ZIndex = anchor.ZIndex = 1;
        holder.MouseExited += () => slot.ZIndex = anchor.ZIndex = 0;
    }

    /// <summary>
    /// 祝福和普通选项画成原作的祝福卡：半透明卡底、品质圆框里的图标、名字条、说明。
    /// 位置按原作预制体（LostThingCardItem_PC：圆框中心在卡中心上方 110、直径 352；名字条在下方 46.6、376×55；说明在下方 186）。
    /// </summary>
    /// <param name="slot">格子。</param>
    /// <param name="option">选项。</param>
    /// <param name="view">展示信息。</param>
    private void BuildTile(Control slot, EventOption option, OptionView? view)
    {
        Control tile = new()
        {
            Name = "Tile", Size = TileSize, PivotOffset = TileSize / 2, MouseFilter = Control.MouseFilterEnum.Stop,
            MouseDefaultCursorShape = option.IsLocked ? Control.CursorShape.Arrow : Control.CursorShape.PointingHand,
        };
        slot.AddChild(tile);

        // 选中光（原作 faguang，九宫格 80，平时透明）在最底下，上面是卡底
        NinePatchRect glow = NewPatch("Glow", "faguang", new Rect2(-Vector2.One * GlowPad, TileSize + Vector2.One * GlowPad * 2), 80, 80);
        glow.Modulate = Colors.Transparent;
        tile.AddChild(glow);
        tile.AddChild(NewImage("Bg", "zhufudi", new Rect2(Vector2.Zero, TileSize)));

        // 圆框：祝福按品质用 kuang / kuang2 / kuang3，其余选项用事件圆框；图标盖住圆框里的金色底
        Vector2 center = TileSize / 2;
        Vector2 ringSize = Vector2.One * (352 * TileUnit);
        Vector2 ringAt = new(center.X - ringSize.X / 2, center.Y - 110 * TileUnit - ringSize.Y / 2);
        int frame = view?.Frame ?? -1;
        tile.AddChild(NewImage("Ring", frame switch { 1 => "kuang2", 2 => "kuang3", >= 0 => "kuang", _ => "kuang__event" },
            new Rect2(ringAt, ringSize)));
        Vector2 iconSize = ringSize * 0.78f;
        Vector2 iconAt = ringAt + (ringSize - iconSize) / 2;
        Texture2D? icon = (option.Relic ?? view?.Relic)?.Icon ?? (view?.Sprite != null ? EventSprites.Get(view.Sprite) : null);
        if (icon != null)
        {
            // 先设 ExpandMode 再设大小，否则大小被贴图尺寸撑开
            tile.AddChild(new TextureRect
            {
                Name = "Icon", Texture = icon, MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Position = iconAt, Size = iconSize,
            });
        }
        else
        {
            // 没有图标的选项：圆形遮罩里的事件插图，铺满圆框内圈
            Vector2 innerSize = ringSize * 0.88f;
            Panel mask = new()
            {
                Name = "Mask", MouseFilter = Control.MouseFilterEnum.Ignore,
                Position = ringAt + (ringSize - innerSize) / 2, Size = innerSize,
            };
            mask.ClipChildren = CanvasItem.ClipChildrenMode.Only;
            mask.AddThemeStyleboxOverride("panel", Round(innerSize.X / 2));
            tile.AddChild(mask);
            TextureRect portrait = new()
            {
                Name = "Portrait", Texture = _event.CreateInitialPortrait(), MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            };
            mask.AddChild(portrait);
            portrait.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }

        // 名字条和名字、说明
        Vector2 barSize = new Vector2(376, 55) * TileUnit;
        Vector2 barAt = new(center.X - barSize.X / 2, center.Y + 46.6f * TileUnit - barSize.Y / 2);
        tile.AddChild(NewImage("NameBg", "biaotidi", new Rect2(barAt, barSize)));
        RichTextLabel name = NewText(tile, "Name", new Rect2(barAt + new Vector2(10, 0), barSize - new Vector2(20, 0)), NameColor, true);
        SetText(name, Center($"[b]{NameOf(option)}[/b]"), 18, 27);
        float descriptionTop = barAt.Y + barSize.Y + 8;
        RichTextLabel description = NewText(tile, "Description",
            new Rect2(20, descriptionTop, TileSize.X - 40, TileSize.Y - descriptionTop - 16), DescriptionColor, true);
        description.VerticalAlignment = VerticalAlignment.Top;
        SetText(description, Center(DescriptionOf(option)), 16, 23);

        // 锁定变灰；否则悬停放大、亮起选中光，点击选择
        if (option.IsLocked)
        {
            tile.Modulate = LockedColor;
            return;
        }
        Hover(tile, () => !_busy, hovered =>
        {
            glow.CreateTween().TweenProperty(glow, "modulate", hovered ? GlowOn : Colors.Transparent, HoverTime);
            slot.ZIndex = hovered ? 1 : 0;
        });
        tile.GuiInput += input =>
        {
            if (IsClick(input))
            {
                Choose(option);
            }
        };
    }

    /// <summary>
    /// 价格（选项卡下面的原作深色价格底）：金币或粉尘数，0 为"免费"；没有价格时不画。
    /// </summary>
    /// <param name="slot">格子。</param>
    /// <param name="option">选项。</param>
    /// <param name="view">展示信息。</param>
    private void BuildPrice(Control slot, EventOption option, OptionView? view)
    {
        int price = view?.Price ?? -1;
        if (price < 0)
        {
            return;
        }
        // 价格底 110×84 的九宫格左右各 46
        NinePatchRect plaque = NewPatch("Price", "di_SLIDER",
            new Rect2(new Vector2((TileSize.X - PriceSize.X) / 2, TileSize.Y + 12), PriceSize), 46, 0);
        slot.AddChild(plaque);
        HBoxContainer box = new()
        {
            Name = "Row", MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center,
        };
        box.AddThemeConstantOverride("separation", 6);
        plaque.AddChild(box);
        box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        float iconSide = PriceSize.Y - 12;
        if (price > 0)
        {
            box.AddChild(new TextureRect
            {
                Texture = EventSprites.Get(view!.PriceIcon), MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(iconSide, iconSide), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            });
        }
        Color color = price == 0 ? FreeColor : option.IsLocked ? PoorColor : PriceColor;
        RichTextLabel label = NewText(box, "Text", new Rect2(0, 0, price > 0 ? 76 : 140, PriceSize.Y), color, true);
        label.CustomMinimumSize = label.Size;
        string text = price == 0 ? PanelText.Get("free") : price.ToString();
        SetText(label, price > 0 ? $"[b]{text}[/b]" : Center($"[b]{text}[/b]"), 22, 28);
    }

    // ===== 选择 =====

    /// <summary>
    /// 选择一个原生选项（等同于点了原生选项按钮）。
    /// </summary>
    /// <param name="option">选项。</param>
    private void Choose(EventOption option)
    {
        if (_busy || option.IsLocked)
        {
            return;
        }
        _busy = true;
        int index = _event.CurrentOptions.ToList().IndexOf(option);
        NEventRoom.Instance!.OptionButtonClicked(option, index);
    }

    /// <summary>
    /// 事件结束后点"继续"：回到地图。
    /// </summary>
    /// <param name="proceed">回地图选项。</param>
    private void Proceed(EventOption proceed)
    {
        _busy = true;
        TaskHelper.RunSafely(proceed.Chosen());
    }

    // ===== 文字 =====

    /// <summary>
    /// 选项显示的名字：展示信息里的名字，或卡名、祝福名、选项标题。
    /// </summary>
    /// <param name="option">选项。</param>
    /// <returns>名字。</returns>
    private static string NameOf(EventOption option)
    {
        OptionView? view = KnightEvent.ViewOf(option);
        if (view?.Name != null)
        {
            return view.Name;
        }
        if (view?.Card != null)
        {
            return view.Card.TitleLocString.GetFormattedText();
        }
        return (option.Relic?.Title ?? option.Title).GetFormattedText();
    }

    /// <summary>
    /// 选项显示的说明：展示信息里的说明，或祝福说明、选项说明。
    /// </summary>
    /// <param name="option">选项。</param>
    /// <returns>说明。</returns>
    private static string DescriptionOf(EventOption option)
    {
        OptionView? view = KnightEvent.ViewOf(option);
        if (view?.Description != null)
        {
            return view.Description;
        }
        return (option.Relic?.DynamicDescription ?? option.Description).GetFormattedText();
    }

    /// <summary>
    /// 新建文字节点：复制原生描述节点（带尖塔字体、[gold] 等文字特效），没有时用普通富文本。
    /// </summary>
    /// <param name="parent">父节点。</param>
    /// <param name="name">节点名。</param>
    /// <param name="rect">位置和大小。</param>
    /// <param name="color">文字颜色。</param>
    /// <param name="outline">是否加深色描边（压在插图上时更清楚）。</param>
    /// <returns>文字节点。</returns>
    private RichTextLabel NewText(Control parent, string name, Rect2 rect, Color color, bool outline)
    {
        RichTextLabel label = _template?.Duplicate() as RichTextLabel ?? new RichTextLabel();
        label.Name = name;
        label.UniqueNameInOwner = false;
        label.Visible = true;
        // 原生描述在播逐字显示时复制出来会带着没显示完的进度，这里显示全部
        label.VisibleCharacters = -1;
        label.VisibleRatio = 1;
        label.Modulate = Colors.White;
        label.SelfModulate = Colors.White;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.BbcodeEnabled = true;
        label.FitContent = false;
        label.ScrollActive = false;
        label.ClipContents = false;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = Vector2.Zero;
        label.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        label.SizeFlagsVertical = Control.SizeFlags.Fill;
        label.AddThemeColorOverride("default_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        label.AddThemeConstantOverride("outline_size", outline ? 8 : 0);
        label.AddThemeColorOverride("font_shadow_color", outline ? new Color(0, 0, 0, 0.5f) : Colors.Transparent);
        parent.AddChild(label);
        Place(label, rect);
        return label;
    }

    /// <summary>
    /// 摆放控件（左上角锚点）。
    /// </summary>
    /// <param name="control">控件。</param>
    /// <param name="rect">位置和大小。</param>
    private static void Place(Control control, Rect2 rect)
    {
        control.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        control.Position = rect.Position;
        control.Size = rect.Size;
    }

    /// <summary>
    /// 设置文字并在字号范围内自动缩放到放得下。
    /// </summary>
    /// <param name="label">文字节点。</param>
    /// <param name="bbcode">文字。</param>
    /// <param name="minSize">最小字号。</param>
    /// <param name="maxSize">最大字号。</param>
    private static void SetText(RichTextLabel label, string bbcode, int minSize, int maxSize)
    {
        if (label is MegaRichTextLabel mega)
        {
            mega.AutoSizeEnabled = true;
            mega.MinFontSize = minSize;
            mega.MaxFontSize = maxSize;
            mega.IsVerticallyBound = true;
            mega.IsHorizontallyBound = true;
            mega.SetTextAutoSize(bbcode);
            return;
        }
        label.AddThemeFontSizeOverride("normal_font_size", maxSize);
        label.AddThemeFontSizeOverride("bold_font_size", maxSize);
        label.Text = bbcode;
    }

    /// <summary>
    /// 文字居中。
    /// </summary>
    /// <param name="bbcode">文字。</param>
    /// <returns>居中的文字。</returns>
    private static string Center(string bbcode)
    {
        return $"[center]{bbcode}[/center]";
    }

    // ===== 外观工具 =====

    /// <summary>
    /// 新建原作贴图（拉伸铺满给定区域，不接收鼠标）。
    /// </summary>
    /// <param name="name">节点名。</param>
    /// <param name="sprite">原作贴图名（images/event_ui/ 下）。</param>
    /// <param name="rect">位置和大小。</param>
    /// <returns>图片节点。</returns>
    private static TextureRect NewImage(string name, string sprite, Rect2 rect)
    {
        // 先设 ExpandMode 再设大小，否则大小被贴图尺寸撑开
        return new TextureRect
        {
            Name = name, Texture = EventSprites.Get(sprite), MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
            Position = rect.Position, Size = rect.Size,
        };
    }

    /// <summary>
    /// 新建九宫格拉伸的原作贴图（边角不变形，和原作 Sliced 图片一样）。
    /// </summary>
    /// <param name="name">节点名。</param>
    /// <param name="sprite">原作贴图名（images/event_ui/ 下）。</param>
    /// <param name="rect">位置和大小。</param>
    /// <param name="sides">左右边距（贴图像素）。</param>
    /// <param name="ends">上下边距（贴图像素）。</param>
    /// <returns>图片节点。</returns>
    private static NinePatchRect NewPatch(string name, string sprite, Rect2 rect, int sides, int ends)
    {
        return new NinePatchRect
        {
            Name = name, Texture = EventSprites.Get(sprite), MouseFilter = Control.MouseFilterEnum.Ignore,
            PatchMarginLeft = sides, PatchMarginRight = sides, PatchMarginTop = ends, PatchMarginBottom = ends,
            Position = rect.Position, Size = rect.Size,
        };
    }

    /// <summary>
    /// 圆形遮罩的形状（只用作裁切，不显示）。
    /// </summary>
    /// <param name="radius">半径。</param>
    /// <returns>底框。</returns>
    private static StyleBoxFlat Round(float radius)
    {
        StyleBoxFlat style = new() { BgColor = Colors.White, AntiAliasing = true };
        style.SetCornerRadiusAll((int)radius);
        return style;
    }

    /// <summary>
    /// 悬停效果：放大一点、变亮。
    /// </summary>
    /// <param name="control">控件（缩放中心应已设在中央）。</param>
    /// <param name="enabled">现在是否响应悬停。</param>
    /// <param name="onHover">悬停状态变化时额外执行。</param>
    private static void Hover(Control control, Func<bool> enabled, Action<bool>? onHover = null)
    {
        Tween? tween = null;
        void Animate(bool hovered)
        {
            if (hovered && !enabled())
            {
                return;
            }
            tween?.Kill();
            tween = control.CreateTween().SetParallel();
            tween.TweenProperty(control, "scale", Vector2.One * (hovered ? HoverScale : 1f), HoverTime)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(control, "self_modulate", hovered ? new Color(1.15f, 1.15f, 1.15f) : Colors.White, HoverTime);
            onHover?.Invoke(hovered);
        }
        control.MouseEntered += () => Animate(true);
        control.MouseExited += () => Animate(false);
    }

    /// <summary>
    /// 是否左键松开。
    /// </summary>
    /// <param name="input">输入事件。</param>
    /// <returns>是时为 true。</returns>
    private static bool IsClick(InputEvent input)
    {
        return input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false };
    }

    /// <summary>
    /// 按窗口大小缩放设计画布，居中。
    /// </summary>
    private void FitDesign()
    {
        Vector2 area = _host.Size;
        if (area.X <= 0 || area.Y <= 0)
        {
            return;
        }
        // 顶栏下沿（宿主坐标）：宿主可能从屏幕顶上方开始，按全局位置换算
        float viewportHeight = _host.GetViewportRect().Size.Y;
        float top = Mathf.Clamp(viewportHeight * TopBarShare - _host.GlobalPosition.Y, 0, area.Y * 0.5f);
        // 内容（ContentTop 到底）放进顶栏下面的区域，居中
        Vector2 content = new(DesignSize.X, DesignSize.Y - ContentTop);
        Vector2 free = new(area.X, area.Y - top);
        float scale = Mathf.Min(free.X / content.X, free.Y / content.Y);
        _design.Scale = new Vector2(scale, scale);
        _design.Position = new Vector2(0, top - ContentTop * scale) + (free - content * scale) / 2;
    }
}

/// <summary>
/// 事件在面板标题下面额外显示的一行信息（BBCode），例如松鼠银行的存款、时光精灵的粉尘。
/// </summary>
public interface IPanelInfo
{
    /// <summary>要显示的信息；为空时不显示。</summary>
    string? PanelInfo { get; }
}
