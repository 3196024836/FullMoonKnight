using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 凝视：敌人攻击时使其无效，给予易伤。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Stare : KnightCounterCard<StarePower>
{
    /// <summary>
    /// 创建凝视。
    /// </summary>
    public Stare() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(2m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 凝视的反制。
/// </summary>
public sealed class StarePower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.Attack;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(2m)];

    /// <inheritdoc />
    public override async Task Resolve(CounterContext context)
    {
        if (context.Monster.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(context.ChoiceContext, context.Monster,
                DynamicVars.Vulnerable.BaseValue, context.Owner, null);
        }
    }
}
