using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 共鸣：敌人的任何行动都无效并夺取效果；攻击的伤害反弹给它自己。升级后还抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Resonance : KnightCounterCard<ResonancePower>
{
    /// <summary>
    /// 创建共鸣。
    /// </summary>
    public Resonance() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [KnightKeywords.Counter, CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Steal)];

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 共鸣的反制。
/// </summary>
public sealed class ResonancePower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.Any;

    /// <inheritdoc />
    public override bool Steals => true;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0)];

    /// <inheritdoc />
    public override async Task Resolve(CounterContext context)
    {
        await context.Steal(true);
        await context.Draw(DynamicVars.Cards.BaseValue);
    }
}
