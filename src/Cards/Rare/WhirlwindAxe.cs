using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 旋风斧：自动打出牌组中随机几张攻击牌的临时复制（随机目标）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class WhirlwindAxe : KnightCard
{
    /// <summary>
    /// 创建旋风斧。
    /// </summary>
    public WhirlwindAxe() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 牌组中的攻击牌（不含会永久移除自己的牌）
        List<CardModel> attacks = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Attack && !c.Keywords.Contains(BaseLib.Cards.BaseLibKeywords.Purge))
            .TakeRandom(DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        foreach (CardModel attack in attacks)
        {
            // 临时复制
            CardModel copy = KnightCards.MakeTemporary(KnightCards.CombatCopy(attack, Owner));
            await CardCmd.AutoPlay(choiceContext, copy, null);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
