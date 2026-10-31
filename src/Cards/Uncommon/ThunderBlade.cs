using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 雷刃：你每打出 1 张攻击牌，对随机敌人造成雷电伤害。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ThunderBlade : KnightEquipmentCard<ThunderBladePower>
{
    /// <summary>
    /// 创建雷刃。
    /// </summary>
    public ThunderBlade() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Unpowered | KnightDamage.Thunder)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Thunder)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 雷刃装备：层数为每次雷电伤害。
/// </summary>
public sealed class ThunderBladePower : KnightEquipmentPower
{
    /// <summary>每回合最多触发几次（后期一回合打十几张攻击牌时不再线性叠加）。</summary>
    public const int TriggersPerTurn = 6;

    /// <summary>本回合已触发次数。</summary>
    private int triggersThisTurn;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Limit", TriggersPerTurn)];

    /// <summary>
    /// 持有者打出攻击牌后，对随机敌人造成雷电伤害。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card.Type != CardType.Attack
            || triggersThisTurn >= TriggersPerTurn)
        {
            return;
        }

        AssertMutable();
        triggersThisTurn++;
        // 随机目标
        Creature? target = KnightCards.RandomEnemy(Owner.Player!);
        if (target != null)
        {
            Flash();
            await KnightDamage.Deal(choiceContext, target, Amount,
                ValueProp.Unpowered | KnightDamage.Thunder | KnightDamage.Equipment, Owner, null);
        }
    }

    /// <summary>
    /// 持有者回合开始抽牌前重置本回合的触发次数。
    /// 不放在回合开始钩子里：齿轮装置会在回合中再触发一次那个钩子，不能顺带重置次数。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player.Creature == Owner)
        {
            AssertMutable();
            triggersThisTurn = 0;
        }

        return Task.CompletedTask;
    }
}
