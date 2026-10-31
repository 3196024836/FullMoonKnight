using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 连击：打出你上一张攻击牌的临时复制；升级后还抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Combo : KnightCard
{
    /// <summary>
    /// 创建连击。
    /// </summary>
    public Combo() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 上一张攻击牌
        CardModel? last = CombatManager.Instance.History.CardPlaysFinished
            .Select(e => e.CardPlay.Card)
            .LastOrDefault(c => c.Owner == Owner && c.Type == CardType.Attack && c is not Combo);
        if (last != null)
        {
            // 复制品打出后直接离开战斗
            await CardCmd.AutoPlay(choiceContext, KnightCards.Duplicate(last), cardPlay.Target);
        }
        if (DynamicVars.Cards.BaseValue > 0m)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
