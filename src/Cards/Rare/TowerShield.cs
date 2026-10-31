using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 塔盾：回合开始时获得 1 点能量，从抽牌堆抽 1 张获得格挡的技能牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class TowerShield : KnightEquipmentCard<TowerShieldPower>
{
    /// <summary>
    /// 创建塔盾。
    /// </summary>
    public TowerShield() : base(CardRarity.Rare)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(1)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Energy.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 塔盾装备：层数为每回合能量，件数为每回合抽的格挡牌张数。
/// </summary>
public sealed class TowerShieldPower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时获得能量，并从抽牌堆抽格挡技能牌。
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
        await PlayerCmd.GainEnergy(Amount, player);
        // 抽牌堆顶部的格挡技能牌
        List<CardModel> blocks = PileType.Draw.GetPile(player).Cards
            .Where(c => c.Type == CardType.Skill && c.GainsBlock)
            .Take(Copies)
            .ToList();
        foreach (CardModel card in blocks)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
