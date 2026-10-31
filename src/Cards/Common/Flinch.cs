using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 退缩：敌人攻击时使其无效，抽牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Flinch : KnightCounterCard<FlinchPower>
{
    /// <summary>
    /// 创建退缩。
    /// </summary>
    public Flinch() : base(CardRarity.Common)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 退缩的反制。
/// </summary>
public sealed class FlinchPower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.Attack;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    public override async Task Resolve(CounterContext context)
    {
        await context.Draw(DynamicVars.Cards.BaseValue);
    }
}
