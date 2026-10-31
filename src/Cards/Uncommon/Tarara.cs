using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 嗒啦啦噗噜噜：从连击、顽强、洗礼三个战斗技能中选 1 个立即生效，消耗；升级后不再消耗。
/// 原作 1 费（升级 0）；女骑士只有 1 点能量（单卡模拟 -1.9 层），改为 0 费，升级改为去掉消耗。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Tarara : KnightCard
{
    /// <summary>
    /// 创建嗒啦啦噗噜噜。
    /// </summary>
    public Tarara() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<TararaCombo>(), HoverTipFactory.FromCard<TararaTenacity>(),
        HoverTipFactory.FromCard<TararaBaptism>(),
    ];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 三个选项
        List<CardModel> options =
        [
            CombatState!.CreateCard<TararaCombo>(Owner), CombatState.CreateCard<TararaTenacity>(Owner),
            CombatState.CreateCard<TararaBaptism>(Owner),
        ];
        if (await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner) is KnightOptionCard chosen)
        {
            await chosen.Resolve(choiceContext, this);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

/// <summary>
/// 连击（战斗技能）：你打出的下一张牌额外打出一次。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class TararaCombo : KnightOptionCard
{
    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DuplicationPower>(1m)];

    /// <inheritdoc />
    public override Task Resolve(PlayerChoiceContext choiceContext, CardModel source)
    {
        return PowerCmd.Apply<DuplicationPower>(choiceContext, source.Owner.Creature,
            DynamicVars["DuplicationPower"].BaseValue, source.Owner.Creature, source);
    }
}

/// <summary>
/// 顽强（战斗技能）：获得已损失生命一定比例的格挡。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class TararaTenacity : KnightOptionCard
{
    /// <summary>比例变量名。</summary>
    private const string PercentVar = "Percent";

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(PercentVar, 50m)];

    /// <inheritdoc />
    public override async Task Resolve(PlayerChoiceContext choiceContext, CardModel source)
    {
        // 已损失生命对应的格挡
        decimal block = Math.Floor((source.Owner.Creature.MaxHp - source.Owner.Creature.CurrentHp)
            * DynamicVars[PercentVar].BaseValue / 100m);
        if (block > 0m)
        {
            await CreatureCmd.GainBlock(source.Owner.Creature, block, ValueProp.Unpowered, null);
        }
    }
}

/// <summary>
/// 洗礼（战斗技能）：本场战斗升级手牌中的 1 张牌。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class TararaBaptism : KnightOptionCard
{
    /// <inheritdoc />
    public override async Task Resolve(PlayerChoiceContext choiceContext, CardModel source)
    {
        // 要升级的牌
        CardModel? card = await CardSelectCmd.FromHandForUpgrade(choiceContext, source.Owner, source);
        if (card != null)
        {
            CardCmd.Upgrade(card);
        }
    }
}
