using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 殉道之剑：你每次对自己造成伤害后，获得蓄势。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class MartyrSword : KnightEquipmentCard<MartyrSwordPower>
{
    /// <summary>
    /// 创建殉道之剑。
    /// </summary>
    public MartyrSword() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisedPower>(1m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisedPower>()];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars["PoisedPower"].BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["PoisedPower"].UpgradeValueBy(1m);
    }
}

/// <summary>
/// 殉道之剑装备：层数为每次获得的蓄势层数。
/// </summary>
public sealed class MartyrSwordPower : KnightEquipmentPower
{
    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisedPower>()];

    /// <summary>
    /// 持有者对自己造成伤害后获得蓄势。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="target">受伤者。</param>
    /// <param name="result">伤害结果。</param>
    /// <param name="props">伤害属性。</param>
    /// <param name="dealer">伤害来源。</param>
    /// <param name="cardSource">来源牌。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer != Owner || result.UnblockedDamage <= 0 || !Owner.IsAlive)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<PoisedPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
