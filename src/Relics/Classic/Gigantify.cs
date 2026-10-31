using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 巨大化：本次战斗首次造成 ≥10 点伤害时，将该伤害翻倍（仅触发 1 次）。
/// 状态只在原版确认这次真实伤害被本遗物修改后消耗，伤害钩子本身不改状态，这样手牌预览也能正确显示翻倍。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Gigantify : KnightRelic
{
    private const decimal Threshold = 10m;

    private bool _pending;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _pending = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!_pending || dealer != Owner.Creature || !props.IsPoweredAttack() || amount < Threshold)
        {
            return 1m;
        }

        return 2m;
    }

    /// <summary>
    /// 真正结算的伤害被这件遗物翻倍后（原版只对改过数值的修正者调用，预览不调用）才消耗，
    /// 翻倍与消耗用同一个判定：易伤等乘区把伤害推到 10 以上不会白白用掉。
    /// </summary>
    /// <param name="cardSource">伤害来源牌。</param>
    /// <returns>异步任务。</returns>
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        if (_pending)
        {
            _pending = false;
            Flash();
        }

        return Task.CompletedTask;
    }
}
