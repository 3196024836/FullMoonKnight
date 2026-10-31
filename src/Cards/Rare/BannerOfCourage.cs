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
/// 勇气旗帜：回合开始时抽 1 张牌，随机敌人本轮攻击伤害提高。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BannerOfCourage : KnightEquipmentCard<BannerOfCouragePower>
{
    /// <summary>
    /// 创建勇气旗帜。
    /// </summary>
    public BannerOfCourage() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new PowerVar<InspiredPower>(2m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<InspiredPower>()];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars["InspiredPower"].BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 勇气旗帜装备：层数为给敌人的振奋层数，件数为抽牌数。
/// </summary>
public sealed class BannerOfCouragePower : KnightEquipmentPower
{
    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<InspiredPower>()];

    /// <summary>
    /// 持有者回合开始时抽牌，并给随机敌人振奋。
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
        Flash();
        await CardPileCmd.Draw(choiceContext, Copies, player);
        // 随机目标
        Creature? target = KnightCards.RandomEnemy(player);
        if (target != null)
        {
            await PowerCmd.Apply<InspiredPower>(choiceContext, target, Amount, Owner, null);
        }
    }
}
