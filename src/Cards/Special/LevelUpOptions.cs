using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using FullMoonKnight.Relics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 骑士升级选项牌：升级时由 <see cref="KnightOathBase" /> 三选一，选中后结算成长效果。
/// 只出现在升级界面，不进牌组，不能打出。参照原作的升级奖励（行动、生命、强化、清除）；抽牌数随等级自动成长，不作为选项。
/// </summary>
public abstract class LevelUpOption : KnightCard
{
    /// <summary>
    /// 创建选项牌。
    /// </summary>
    protected LevelUpOption() : base(-1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <summary>抽到的权重（越大越常出现）。</summary>
    public abstract int Weight { get; }

    /// <summary>
    /// 这一项现在能不能出现（达到上限、牌组里没有可选的牌时不出现）。
    /// </summary>
    /// <param name="oath">满月。</param>
    /// <returns>可以出现时为 true。</returns>
    public virtual bool IsAvailable(KnightOathBase oath)
    {
        return true;
    }

    /// <summary>
    /// 结算选中的成长。
    /// </summary>
    /// <param name="oath">满月。</param>
    /// <returns>异步任务。</returns>
    public abstract Task Apply(KnightOathBase oath);

    /// <inheritdoc />
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 行动：每回合能量 +1。最多选 <see cref="KnightOathBase.MaxBonusEnergy" /> 次，
/// 且总能量上限（含原版遗物给的）达到 <see cref="KnightOathBase.EnergyCap" /> 后不再出现。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class LevelUpEnergy : LevelUpOption
{
    /// <inheritdoc />
    public override int Weight => 2;

    /// <inheritdoc />
    public override bool IsAvailable(KnightOathBase oath)
    {
        return oath.BonusEnergy < KnightOathBase.MaxBonusEnergy && oath.TotalMaxEnergy < KnightOathBase.EnergyCap;
    }

    /// <inheritdoc />
    public override Task Apply(KnightOathBase oath)
    {
        oath.AddBonusEnergy();
        return Task.CompletedTask;
    }
}

/// <summary>
/// 生命：生命上限增加（同时回复同样多的生命）。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class LevelUpVitality : LevelUpOption
{
    /// <inheritdoc />
    public override int Weight => 3;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(6m)];

    /// <inheritdoc />
    public override Task Apply(KnightOathBase oath)
    {
        return CreatureCmd.GainMaxHp(oath.Owner.Creature, DynamicVars.MaxHp.BaseValue);
    }
}

/// <summary>
/// 强化：选择牌组中的牌升级。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class LevelUpUpgrade : LevelUpOption
{
    /// <inheritdoc />
    public override int Weight => 2;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    /// <inheritdoc />
    public override bool IsAvailable(KnightOathBase oath)
    {
        return PileType.Deck.GetPile(oath.Owner).Cards.Any(c => c.IsUpgradable);
    }

    /// <inheritdoc />
    public override async Task Apply(KnightOathBase oath)
    {
        // 选中要升级的牌
        IEnumerable<CardModel> cards = await CardSelectCmd.FromDeckForUpgrade(oath.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, DynamicVars.Cards.IntValue));
        foreach (CardModel card in cards)
        {
            CardCmd.Upgrade(card);
        }
    }
}

/// <summary>
/// 清除：从牌组中移除 1 张牌。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class LevelUpPurge : LevelUpOption
{
    /// <inheritdoc />
    public override int Weight => 2;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    public override bool IsAvailable(KnightOathBase oath)
    {
        return PileType.Deck.GetPile(oath.Owner).Cards.Any(c => c.IsRemovable);
    }

    /// <inheritdoc />
    public override async Task Apply(KnightOathBase oath)
    {
        // 选中要移除的牌
        IEnumerable<CardModel> cards = await CardSelectCmd.FromDeckForRemoval(oath.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, DynamicVars.Cards.IntValue));
        await CardPileCmd.RemoveFromDeck(cards.ToList());
    }
}
