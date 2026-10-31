using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 支援：造成伤害，抽 1 张牌。你打出攻击牌时，若它在抽牌堆中，把它放入手牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Backup : KnightCard
{
    /// <summary>
    /// 创建支援。
    /// </summary>
    public Backup() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];

    /// <summary>
    /// 打出其他攻击牌后从抽牌堆回到手牌。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && cardPlay.Card.Type == CardType.Attack && cardPlay.Card != this
            && Pile?.Type == PileType.Draw)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.SlashVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        // 第五批平衡：后期连打时回手太频繁，升级伤害 +3 → +2
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
