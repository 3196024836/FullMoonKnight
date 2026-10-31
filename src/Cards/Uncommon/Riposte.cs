using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 还击：敌人攻击时使其无效，对它造成伤害并抽牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Riposte : KnightCounterCard<RipostePower>
{
    /// <summary>
    /// 创建还击。
    /// </summary>
    public Riposte() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new CardsVar(1)];

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

/// <summary>
/// 还击的反制。
/// </summary>
public sealed class RipostePower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.Attack;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move), new CardsVar(1)];

    /// <inheritdoc />
    public override async Task Resolve(CounterContext context)
    {
        if (context.Monster.IsAlive)
        {
            await KnightDamage.Deal(context.ChoiceContext, context.Monster, DynamicVars.Damage.BaseValue,
                ValueProp.Move, context.Owner, null);
        }
        await context.Draw(DynamicVars.Cards.BaseValue);
    }
}
