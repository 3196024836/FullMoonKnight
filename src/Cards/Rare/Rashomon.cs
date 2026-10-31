using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 罗生门：直到你的下个回合，你每造成 1 点伤害，获得 1 点格挡。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Rashomon : KnightCard
{
    /// <summary>
    /// 创建罗生门。
    /// </summary>
    public Rashomon() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RashomonPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

/// <summary>
/// 罗生门效果：直到持有者下个回合开始，持有者每造成 1 点伤害，获得层数点格挡。
/// </summary>
public sealed class RashomonPower : KnightPower
{
    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者对其他生物造成伤害后获得格挡。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || target == Owner || result.TotalDamage <= 0)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner, result.TotalDamage * Amount, ValueProp.Unpowered, null);
    }

    /// <summary>
    /// 持有者一方的下个回合开始时移除。
    /// </summary>
    /// <param name="side">开始回合的一方。</param>
    /// <param name="participants">该方的生物。</param>
    /// <param name="combatState">战斗状态。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
