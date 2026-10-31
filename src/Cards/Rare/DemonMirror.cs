using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 恶魔手镜：回合开始时，给予随机敌人恶作剧。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class DemonMirror : KnightEquipmentCard<DemonMirrorPower>
{
    /// <summary>
    /// 创建恶魔手镜。
    /// </summary>
    public DemonMirror() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MischiefPower>(1m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MischiefPower>()];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars["MischiefPower"].BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["MischiefPower"].UpgradeValueBy(1m);
    }
}

/// <summary>
/// 恶魔手镜装备：层数为每回合的恶作剧层数。
/// </summary>
public sealed class DemonMirrorPower : KnightEquipmentPower
{
    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MischiefPower>()];

    /// <summary>
    /// 持有者回合开始时，给予随机敌人恶作剧。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="player">回合开始的玩家。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        // 随机目标
        Creature? target = KnightCards.RandomEnemy(player);
        if (target != null)
        {
            Flash();
            await PowerCmd.Apply<MischiefPower>(choiceContext, target, Amount, Owner, null);
        }
    }
}
