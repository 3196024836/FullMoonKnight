using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Visuals;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Powers;

/// <summary>
/// 装备能力基类：装备牌打出后给自己施加，整场战斗有效，同名装备叠加。
/// 层数表示效果数值之和（例如圆盾的格挡总量）；件数单独记录，供致命武器、马蹄铁等按装备数量计算。
/// </summary>
public abstract class KnightEquipmentPower : KnightPower
{
    /// <summary>
    /// 每个实例的私有数据。
    /// </summary>
    private sealed class EquipData
    {
        /// <summary>装备件数。</summary>
        public int Copies;
    }

    // ===== 属性 =====

    /// <inheritdoc />
    public override PowerType Type => PowerType.Buff;

    /// <inheritdoc />
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>这件装备的件数。</summary>
    public int Copies => GetInternalData<EquipData>().Copies;

    /// <inheritdoc />
    protected override object InitInternalData()
    {
        return new EquipData();
    }

    // ===== 装备 =====

    /// <summary>
    /// 给牌的持有者装备一件装备：层数增加 amount，件数加 1。
    /// </summary>
    /// <typeparam name="T">装备能力类型。</typeparam>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="owner">装备者。</param>
    /// <param name="amount">本件装备的效果数值。</param>
    /// <param name="cardSource">来源牌，没有时为空。</param>
    /// <returns>装备能力；战斗结束等情况下为空。</returns>
    public static async Task<T?> Equip<T>(PlayerChoiceContext choiceContext, Creature owner, decimal amount,
        CardModel? cardSource) where T : KnightEquipmentPower
    {
        // 施加后的能力
        T? power = await PowerCmd.Apply<T>(choiceContext, owner, amount, owner, cardSource);
        if (power != null)
        {
            power.GetInternalData<EquipData>().Copies++;
            SfxCmd.Play(KnightAudio.Equip);
        }
        return power;
    }

    /// <summary>
    /// 装备对持有者造成穿刺自伤（殉道系列）。伤害来源是持有者自己，殉道之剑据此判断自伤。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="amount">伤害。</param>
    /// <returns>异步任务。</returns>
    protected Task SelfDamage(PlayerChoiceContext choiceContext, decimal amount)
    {
        return KnightDamage.Deal(choiceContext, Owner, amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner, null);
    }

    /// <summary>
    /// 统计生物身上的装备件数。
    /// </summary>
    /// <param name="creature">生物。</param>
    /// <returns>全部装备的件数之和。</returns>
    public static int CountEquipment(Creature creature)
    {
        return creature.Powers.OfType<KnightEquipmentPower>().Sum(p => p.Copies);
    }
}
