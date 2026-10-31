using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 飞斧：你每回合打出的第 1 张攻击牌额外打出一次。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class FlyingAxe : KnightEquipmentCard<FlyingAxePower>
{
    /// <summary>
    /// 创建飞斧。
    /// </summary>
    public FlyingAxe() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(1)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Repeat.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 飞斧装备：层数为额外打出的次数。
/// </summary>
public sealed class FlyingAxePower : KnightEquipmentPower
{
    /// <summary>本回合是否已经触发。</summary>
    private bool usedThisTurn;

    /// <summary>
    /// 本回合第 1 张攻击牌的打出次数增加层数。
    /// </summary>
    /// <param name="card">打出的牌。</param>
    /// <param name="target">目标。</param>
    /// <param name="playCount">当前打出次数。</param>
    /// <returns>修改后的打出次数。</returns>
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return card.Owner.Creature == Owner && card.Type == CardType.Attack && !usedThisTurn
            ? playCount + Amount
            : playCount;
    }

    /// <summary>
    /// 触发后标记本回合已使用。
    /// </summary>
    /// <param name="card">打出的牌。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        AssertMutable();
        usedThisTurn = true;
        Flash();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 持有者回合开始抽牌前重置。
    /// 不放在回合开始钩子里：齿轮装置会在回合中再触发一次那个钩子，不能顺带重置次数。
    /// </summary>
    /// <param name="player">抽牌的玩家。</param>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player.Creature == Owner)
        {
            AssertMutable();
            usedThisTurn = false;
        }
        return Task.CompletedTask;
    }
}
