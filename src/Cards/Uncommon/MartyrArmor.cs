using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 殉道之铠：回合开始时，失去 1 点生命，获得格挡。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class MartyrArmor : KnightEquipmentCard<MartyrArmorPower>
{
    /// <summary>
    /// 创建殉道之铠。
    /// </summary>
    public MartyrArmor() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HpLossVar(1m), new BlockVar(11m, ValueProp.Unpowered)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Block.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

/// <summary>
/// 殉道之铠装备：层数为每回合格挡，件数为每回合失去的生命。
/// </summary>
public sealed class MartyrArmorPower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时失去生命并获得格挡。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="player">回合开始的玩家。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        await SelfDamage(choiceContext, Copies);
        if (Owner.IsAlive)
        {
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
        }
    }
}
