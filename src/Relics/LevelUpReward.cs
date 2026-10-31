using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.TestSupport;

namespace FullMoonKnight.Relics;

/// <summary>
/// 战斗奖励里的"等级提升"：和卡牌奖励一样排在奖励列表里，点开再三选一成长。
/// 联机时各人在自己的奖励界面里选，不会让队友干等着看不到东西。
/// 不进存档（奖励类型是原版的固定枚举）：读档时奖励按满月里记下的待选次数重新生成。
/// </summary>
public sealed class LevelUpReward : Reward
{
    /// <summary>奖励图标（满月的遗物图标）。</summary>
    private static string RewardIcon => KnightAssets.Image("relics/knight_oath.png");

    /// <summary>发奖励的满月。</summary>
    private readonly KnightOathBase oath;

    /// <summary>第一次点开时抽出的选项；再点开还是这几个，不能靠反复打开重抽。</summary>
    private List<CardModel>? options;

    /// <summary>
    /// 创建等级提升奖励。
    /// </summary>
    /// <param name="oath">满月。</param>
    /// <param name="player">奖励的玩家。</param>
    public LevelUpReward(KnightOathBase oath, Player player) : base(player)
    {
        this.oath = oath;
    }

    /// <inheritdoc />
    protected override RewardType RewardType => RewardType.None;

    /// <summary>排在卡牌奖励（5）之后、删牌（7）之前。</summary>
    public override int RewardsSetIndex => 6;

    /// <inheritdoc />
    public override LocString Description => new("relics", "FULLMOONKNIGHT-KNIGHT_OATH.levelUpReward");

    /// <inheritdoc />
    public override bool IsPopulated => true;

    /// <inheritdoc />
    public override void Populate()
    {
    }

    /// <inheritdoc />
    public override Control? CreateIcon()
    {
        if (TestMode.IsOn)
        {
            return null;
        }
        TextureRect icon = new()
        {
            Texture = ResourceLoader.Load<Texture2D>(RewardIcon),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return icon;
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSelect()
    {
        options ??= oath.RollLevelUpOptions();
        return await oath.ResolveLevelUp(options);
    }

    /// <inheritdoc />
    public override void OnSkipped()
    {
        oath.DropPendingLevelUp();
    }

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
    }
}
