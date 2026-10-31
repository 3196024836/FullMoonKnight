using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 齿轮装置（先祖牌，原作 8804）：获得格挡，触发所有装备每回合开始时的效果。
/// 由远古之牙把初始的短剑变成这张牌（短剑实现 ITranscendenceCard，同原版铁甲战士的痛击 → 破碎）。
/// 先祖稀有度不进卡牌奖励，战斗中也不会被随机生成。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class GearDevice : KnightCard
{
    /// <summary>
    /// 创建齿轮装置。
    /// </summary>
    public GearDevice() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        // 先取快照：触发效果时可能新装备或移除装备
        List<KnightEquipmentPower> equipment = Owner.Creature.Powers.OfType<KnightEquipmentPower>().ToList();
        foreach (KnightEquipmentPower power in equipment)
        {
            if (power.Owner == Owner.Creature)
            {
                await power.AfterPlayerTurnStart(choiceContext, Owner);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
