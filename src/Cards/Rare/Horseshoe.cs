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
/// 马蹄铁：回合开始时，你每有 1 件装备，获得格挡。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Horseshoe : KnightEquipmentCard<HorseshoePower>
{
    /// <summary>
    /// 创建马蹄铁。
    /// </summary>
    public Horseshoe() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(1m, ValueProp.Unpowered), new DynamicVar("FullBonusLimit", HorseshoePower.FullBonusLimit)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Block.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 马蹄铁装备：层数为每件装备对应的格挡。
/// </summary>
public sealed class HorseshoePower : KnightEquipmentPower
{
    /// <summary>前几件装备额外提供 1 点格挡。</summary>
    public const int FullBonusLimit = 4;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FullBonusLimit", FullBonusLimit)];

    /// <summary>
    /// 持有者回合开始时，前四件装备每件额外提供 1 点格挡。
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
        // 所有装备按层数计入，前四件再按马蹄铁件数追加格挡。
        int equipment = CountEquipment(Owner);
        decimal block = Amount * equipment + Copies * Math.Min(equipment, FullBonusLimit);
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}
