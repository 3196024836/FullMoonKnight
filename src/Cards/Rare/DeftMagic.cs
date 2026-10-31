using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 娴熟魔术：给予所有敌人恶作剧。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class DeftMagic : KnightCard
{
    /// <summary>
    /// 创建娴熟魔术。
    /// </summary>
    public DeftMagic() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MischiefPower>(3m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MischiefPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (Creature enemy in CombatState!.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<MischiefPower>(choiceContext, enemy, DynamicVars["MischiefPower"].BaseValue,
                Owner.Creature, this);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
