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
/// 圆盾：回合开始时获得格挡。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class RoundShield : KnightEquipmentCard<RoundShieldPower>
{
    /// <summary>
    /// 创建圆盾。
    /// </summary>
    public RoundShield() : base(CardRarity.Common)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Unpowered)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Block.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

/// <summary>
/// 圆盾装备：层数为每回合格挡。
/// </summary>
public sealed class RoundShieldPower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时获得格挡。
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
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}
