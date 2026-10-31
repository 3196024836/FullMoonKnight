using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 中场休息（先祖牌，原作 8809）：消耗，本回合结束后再进行一个回合。
/// 达夫的尘封典籍给本角色卡池里实现 <see cref="ITomeCard" /> 的先祖牌（典籍给的牌会直接升级）。
/// 先祖稀有度不进卡牌奖励，战斗中也不会被随机生成。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Intermission : KnightCard, ITomeCard
{
    /// <summary>
    /// 创建中场休息。
    /// </summary>
    public Intermission() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntermissionPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

/// <summary>
/// 中场休息：层数为还要进行的额外回合数（同原版的额外回合，敌人这期间不行动）。
/// </summary>
public sealed class IntermissionPower : KnightPower
{
    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 持有者回合结束后再进行一个回合。
    /// </summary>
    /// <param name="player">回合结束的玩家。</param>
    /// <returns>是否进行额外回合。</returns>
    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner.Player;
    }

    /// <summary>
    /// 进行了额外回合后减 1 层。
    /// </summary>
    /// <param name="player">进行额外回合的玩家。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
