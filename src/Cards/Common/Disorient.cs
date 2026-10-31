using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 迷失方向：敌人做非攻击行动时使其无效，并夺取效果；升级后还抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Disorient : KnightCounterCard<DisorientPower>
{
    /// <summary>
    /// 创建迷失方向。
    /// </summary>
    public Disorient() : base(CardRarity.Common)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Steal)];

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 迷失方向的反制。
/// </summary>
public sealed class DisorientPower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.NonAttack;

    /// <inheritdoc />
    public override bool Steals => true;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    public override async Task Resolve(CounterContext context)
    {
        await context.Steal(false);
        await context.Draw(DynamicVars.Cards.BaseValue);
    }
}
