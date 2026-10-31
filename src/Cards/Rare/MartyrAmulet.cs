using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 殉道护符：回合开始时，失去 1 点生命，抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class MartyrAmulet : KnightEquipmentCard<MartyrAmuletPower>
{
    /// <summary>
    /// 创建殉道护符。
    /// </summary>
    public MartyrAmulet() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar(1m), new CardsVar(1)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Cards.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 殉道护符装备：层数为每回合抽牌数，件数为每回合失去的生命。
/// </summary>
public sealed class MartyrAmuletPower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时失去生命并抽牌。
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
            await CardPileCmd.Draw(choiceContext, Amount, player);
        }
    }
}
