using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 处决：造成伤害；目标生命低于 30% 时改为造成大量伤害。
/// 场上有斩杀线以下的敌人时牌发金光；瞄准这样的敌人时敌人闪红（见 <see cref="Visuals.ExecuteTargetHighlight" />）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Execute : KnightCard
{
    /// <summary>斩杀伤害变量名。</summary>
    private const string ExecuteDamageVar = "ExecuteDamage";

    /// <summary>斩杀线（目标生命占最大生命的百分比）。</summary>
    private const int ThresholdPercent = 30;

    /// <summary>
    /// 创建处决。
    /// </summary>
    public Execute() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DamageVar(ExecuteDamageVar, 20m, ValueProp.Move),
        new DynamicVar("Threshold", ThresholdPercent),
    ];

    /// <inheritdoc />
    protected override bool ShouldGlowGoldInternal => CombatState?.HittableEnemies.Any(IsBelowThreshold) ?? false;

    /// <summary>
    /// 目标是否在斩杀线以下。
    /// </summary>
    /// <param name="target">目标。</param>
    /// <returns>在斩杀线以下时为 true。</returns>
    public static bool IsBelowThreshold(Creature target)
    {
        return target.IsAlive && target.CurrentHp * 100 < target.MaxHp * ThresholdPercent;
    }

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        // 目标是否在斩杀线以下
        bool below = IsBelowThreshold(cardPlay.Target);
        // 本次伤害
        decimal damage = below ? DynamicVars[ExecuteDamageVar].BaseValue : DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx(below ? KnightDamage.BluntVfx : KnightDamage.SlashVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[ExecuteDamageVar].UpgradeValueBy(6m);
    }
}
