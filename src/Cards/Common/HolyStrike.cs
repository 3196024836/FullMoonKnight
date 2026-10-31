using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 神圣攻击：造成伤害，回复未被格挡伤害一半的生命（向下取整）。
/// 原作是回复全部未被格挡伤害；尖塔回血稀缺（原版回血牌很少，比如 2 费稀有的“还不能”回 10 点），0 费攻击每次回 5～7 点太多，减半。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class HolyStrike : KnightCard
{
    /// <summary>
    /// 创建神圣攻击。
    /// </summary>
    public HolyStrike() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx(KnightDamage.SlashVfx)
            .Execute(choiceContext);
        // 回复量：未被格挡伤害的一半
        int heal = attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) / 2;
        if (heal > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, heal);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
