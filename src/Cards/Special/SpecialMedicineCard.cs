using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace FullMoonKnight.Cards;

/// <summary>
/// 特效药（特效药遗物在首领战给的牌）：0 费，消耗，抉择：恢复生命或获得能量。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class SpecialMedicineCard : KnightCard
{
    /// <summary>
    /// 创建特效药。
    /// </summary>
    public SpecialMedicineCard() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<MedicineHeal>(), HoverTipFactory.FromCard<MedicineEnergy>(),
    ];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 选项
        List<CardModel> options =
        [
            CombatState!.CreateCard<MedicineHeal>(Owner), CombatState.CreateCard<MedicineEnergy>(Owner),
        ];
        if (await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner) is KnightOptionCard chosen)
        {
            await chosen.Resolve(choiceContext, this);
        }
    }
}

/// <summary>
/// 特效药选项：恢复生命。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class MedicineHeal : KnightOptionCard
{
    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(20m)];

    /// <inheritdoc />
    public override Task Resolve(PlayerChoiceContext choiceContext, CardModel source)
    {
        return CreatureCmd.Heal(source.Owner.Creature, DynamicVars["Heal"].BaseValue);
    }
}

/// <summary>
/// 特效药选项：获得能量。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class MedicineEnergy : KnightOptionCard
{
    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3)];

    /// <inheritdoc />
    public override Task Resolve(PlayerChoiceContext choiceContext, CardModel source)
    {
        return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, source.Owner);
    }
}
