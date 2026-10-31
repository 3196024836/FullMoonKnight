using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 吸血鬼剑：你每累计造成 12 点伤害，回复生命。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class VampireSword : KnightEquipmentCard<VampireSwordPower>
{
    /// <summary>
    /// 创建吸血鬼剑。
    /// </summary>
    public VampireSword() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(1m), new DynamicVar("DamageStep", VampireSwordPower.DamagePerHeal)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Heal.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 吸血鬼剑装备：层数为每 12 点伤害回复的生命。
/// </summary>
public sealed class VampireSwordPower : KnightEquipmentPower
{
    /// <summary>每累计多少伤害回复一次。</summary>
    public const int DamagePerHeal = 12;

    /// <summary>尚未结算的累计伤害。</summary>
    private int damageDealt;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageStep", DamagePerHeal)];

    /// <summary>
    /// 累计持有者造成的伤害，满 12 点回复生命。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || target == Owner || result.UnblockedDamage <= 0)
        {
            return;
        }
        AssertMutable();
        damageDealt += result.UnblockedDamage;
        // 本次可回复的次数
        int heals = damageDealt / DamagePerHeal;
        if (heals <= 0 || !Owner.IsAlive)
        {
            return;
        }
        damageDealt %= DamagePerHeal;
        Flash();
        await CreatureCmd.Heal(Owner, heals * Amount);
    }
}
