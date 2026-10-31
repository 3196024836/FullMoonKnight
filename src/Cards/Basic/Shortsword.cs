using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 短剑（基础）：每打出 2 张攻击牌，对随机敌人造成伤害。
/// 远古之牙把它变成先祖牌齿轮装置（同原版铁甲战士的痛击 → 破碎）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Shortsword : KnightEquipmentCard<ShortswordPower>, ITranscendenceCard
{
    /// <summary>
    /// 创建短剑。
    /// </summary>
    public Shortsword() : base(CardRarity.Basic)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Unpowered)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    /// <inheritdoc />
    public CardModel GetTranscendenceTransformedCard()
    {
        return ModelDb.Card<GearDevice>();
    }
}

/// <summary>
/// 短剑装备：层数为每次伤害。
/// </summary>
public sealed class ShortswordPower : KnightEquipmentPower
{
    /// <summary>每几张攻击牌触发一次。</summary>
    public const int AttacksPerTrigger = 2;

    /// <summary>每回合最多触发几次（后期一回合打十几张攻击牌时不再线性叠加）。</summary>
    public const int TriggersPerTurn = 3;

    /// <summary>本场已打出、尚未结算的攻击牌数。</summary>
    private int attacksPlayed;

    /// <summary>本回合已触发次数。</summary>
    private int triggersThisTurn;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Attacks", AttacksPerTrigger), new DynamicVar("Limit", TriggersPerTurn)];

    /// <summary>
    /// 持有者每打出 2 张攻击牌，对随机敌人造成伤害。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        AssertMutable();
        // 本回合次数用完后不再累计，免得下回合第 1 张攻击直接触发
        if (triggersThisTurn >= TriggersPerTurn)
        {
            return;
        }
        attacksPlayed++;
        if (attacksPlayed < AttacksPerTrigger)
        {
            return;
        }

        attacksPlayed = 0;
        triggersThisTurn++;
        // 可攻击的敌人
        IReadOnlyList<Creature> enemies = CombatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }
        // 随机目标（同步随机数）
        Creature target = Owner.Player!.RunState.Rng.CombatTargets.NextItem(enemies)!;
        Flash();
        await KnightDamage.Deal(choiceContext, target, Amount, ValueProp.Unpowered | KnightDamage.Equipment,
            Owner, null);
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
