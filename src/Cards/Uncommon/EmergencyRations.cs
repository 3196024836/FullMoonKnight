using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 应急干粮：消耗，获得能量。你的能量用完时，若它在抽牌堆中，把它放入手牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class EmergencyRations : KnightCard
{
    /// <summary>
    /// 创建应急干粮。
    /// </summary>
    public EmergencyRations() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(0)];

    /// <summary>
    /// 能量用完时从抽牌堆回到手牌。
    /// </summary>
    /// <param name="card">花费能量的牌。</param>
    /// <param name="amount">花费的能量。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner == Owner && Pile?.Type == PileType.Draw && Owner.PlayerCombatState?.Energy == 0)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
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
