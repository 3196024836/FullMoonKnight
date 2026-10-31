using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 突击：每次战斗使用的第 1 张攻击牌伤害翻倍。
/// 状态只在 AfterCardPlayed 中消耗，伤害钩子本身不改状态，这样手牌预览也能正确显示翻倍。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Assault : KnightRelic
{
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
        if (!_pending || dealer != Owner.Creature || !props.IsPoweredAttack() || cardSource?.Type != CardType.Attack)
        {
            return 1m;
        }

        return 2m;
    }

    /// <inheritdoc />
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_pending && cardPlay.Card.Owner == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            _pending = false;
            Flash();
        }

        return Task.CompletedTask;
    }
}
