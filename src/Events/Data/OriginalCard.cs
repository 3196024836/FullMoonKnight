using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;

using FullMoonKnight.Character;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events.Data;

/// <summary>
/// 原作 EventShop.txt 的一行：一张能在商店、宝箱、收藏家出现的卡。
/// </summary>
/// <param name="Id">原作编号；不在原作表里的牌（其他角色、模组新增）为 0。</param>
/// <param name="Card">模组卡（图鉴实例）。</param>
/// <param name="Upgraded">是否升级版。</param>
/// <param name="Type">原作类型：1 行动、2 攻击、3 装备、6 反制、8 特殊。</param>
/// <param name="Tier">原作奖励等级 1–10。</param>
/// <param name="Gold">原作价格。</param>
/// <param name="Warrior">女骑士权重。</param>
/// <param name="StoreMul">商店系数。</param>
/// <param name="BoxMul">宝箱、收藏家系数。</param>
/// <param name="Repeat">重复衰减（EventRepeat，每出现一次权重 × Repeat/10）。</param>
/// <param name="MaxSpawns">本局最多出现次数。</param>
/// <param name="RewardLimit">不为 0 时不进宝箱。</param>
/// <param name="Genres">流派（商店流派保护用）。</param>
public sealed record OriginalCard(int Id, Func<CardModel> Card, bool Upgraded, int Type, int Tier, int Gold,
    int Warrior, int StoreMul, int BoxMul, int Repeat, int MaxSpawns, int RewardLimit, int[] Genres);

/// <summary>
/// 事件抽卡（原作 GetShopCardId、GetChestCardId、CommFunc.GetDealerCard）。
/// 女骑士用原作表；其他角色用自己的卡池，稀有度换成等级（普通 3、罕见 5、稀有 8），权重统一。
/// 出现次数按卡名记在月圆手账里（同名升级版合计）。
/// </summary>
public static class OriginalCardDraw
{
    /// <summary>原作商店流派保护概率（%，common.csv 1152）。</summary>
    private const int GenreProtectPercent = 60;

    /// <summary>商店货位数。</summary>
    public const int ShopSize = 3;

    /// <summary>原作特殊卡类型。</summary>
    public const int SpecialType = 8;

    /// <summary>不在原作表里的牌的统一权重（等于原作大多数牌的 warrior × 系数）。</summary>
    private const int DefaultWeight = 10;

    /// <summary>每章（幕）的等级范围（common.csv 1104）。</summary>
    private static readonly (int Min, int Max)[] ChapterTiers = [(1, 5), (2, 7), (3, 10)];

    /// <summary>
    /// 本幕的奖励等级范围。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>最低、最高等级。</returns>
    public static (int Min, int Max) TierRange(Player player)
    {
        return ChapterTiers[FullMoonNumbers.Chapter(player.RunState) - 1];
    }

    /// <summary>
    /// 稀有度对应的等级（不在原作表里的牌）。
    /// </summary>
    /// <param name="rarity">稀有度。</param>
    /// <returns>等级。</returns>
    public static int TierOf(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => 1,
            CardRarity.Common => 3,
            CardRarity.Uncommon => 5,
            CardRarity.Rare => 8,
            _ => 0,
        };
    }

    /// <summary>
    /// 等级对应的稀有度（其他角色按等级取自己卡池的牌）：1–4 普通、5–7 罕见、8–10 稀有。
    /// </summary>
    /// <param name="tier">等级。</param>
    /// <returns>稀有度。</returns>
    public static CardRarity RarityOf(int tier)
    {
        return tier switch
        {
            <= 4 => CardRarity.Common,
            <= 7 => CardRarity.Uncommon,
            _ => CardRarity.Rare,
        };
    }

    /// <summary>
    /// 原作等级价格（tier 1–10：5/10/15/25/40/50/65/80/95/110），已换算成模组金币。
    /// </summary>
    /// <param name="entry">卡。</param>
    /// <returns>模组价格。</returns>
    public static int PriceOf(OriginalCard entry)
    {
        return FullMoonNumbers.Gold(entry.Gold);
    }

    /// <summary>
    /// 玩家的事件卡池。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <returns>卡池。</returns>
    public static IReadOnlyList<OriginalCard> Pool(Player player)
    {
        if (player.Character is Knight)
        {
            return OriginalCardTable.All;
        }
        return player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => TierOf(c.Rarity) > 1)
            .Select(Synthetic)
            .ToList();
    }

    /// <summary>
    /// 不在原作表里的牌：按稀有度定等级和价格，权重统一。
    /// </summary>
    /// <param name="card">图鉴实例。</param>
    /// <returns>卡。</returns>
    private static OriginalCard Synthetic(CardModel card)
    {
        int tier = TierOf(card.Rarity);
        int[] tierGold = [0, 5, 10, 15, 25, 40, 50, 65, 80, 95, 110];
        return new OriginalCard(0, () => card, false, 0, tier, tierGold[Math.Clamp(tier, 0, 10)], DefaultWeight,
            100, 100, 3, 5, 0, []);
    }

    /// <summary>
    /// 牌组里一张牌对应的原作行（找不到时按稀有度新建）。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="card">牌组里的牌。</param>
    /// <returns>卡。</returns>
    public static OriginalCard EntryOf(Player player, CardModel card)
    {
        if (player.Character is Knight)
        {
            // 同名同升级状态的行；没有升级版的行时用未升级行
            OriginalCard? exact = OriginalCardTable.All
                .FirstOrDefault(e => e.Card().Id == card.Id && e.Upgraded == card.IsUpgraded);
            OriginalCard? any = exact ?? OriginalCardTable.All.FirstOrDefault(e => e.Card().Id == card.Id);
            if (any != null)
            {
                return any;
            }
        }
        return Synthetic(card.CanonicalInstance);
    }

    /// <summary>
    /// 新建一张属于玩家的牌（还没有牌堆），升级版直接升级。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="entry">卡。</param>
    /// <returns>新牌。</returns>
    public static CardModel Create(Player player, OriginalCard entry)
    {
        CardModel card = player.RunState.CreateCard(entry.Card(), player);
        if (entry.Upgraded && card.IsUpgradable)
        {
            CardCmd.Upgrade(card);
        }
        return card;
    }

    // ===== 商店 =====

    /// <summary>
    /// 商店抽 3 张（原作 GetShopCardId.GetShopCard）：第 1 张 60% 按牌组流派抽，其余按商店权重补满；
    /// 不重名，第 3 张不和前两张同流派；抽中即记一次出现。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">事件随机数。</param>
    /// <param name="minTier">最低等级。</param>
    /// <param name="maxTier">最高等级。</param>
    /// <returns>货（可能少于 3 张）。</returns>
    public static List<OriginalCard> Shop(Player player, Rng rng, int minTier, int maxTier)
    {
        FullMoonLedger ledger = FullMoonLedger.Of(player);
        // 本次的池
        List<OriginalCard> pool = Pool(player).Where(e => e.Tier >= minTier && e.Tier <= maxTier).ToList();
        List<OriginalCard> picked = [];
        if (player.Character is Knight && rng.NextInt(100) < GenreProtectPercent)
        {
            int genre = DeckGenre(player, rng);
            if (genre != 0)
            {
                List<OriginalCard> genreCards = pool.Where(e => e.Genres.Contains(genre)).ToList();
                OriginalCard? first = Weighted(genreCards, e => ShopWeight(ledger, e), rng);
                if (first != null && ShopWeight(ledger, first) > 0)
                {
                    picked.Add(first);
                    ledger.MarkCardAppeared(first.Card());
                }
            }
        }
        for (int attempt = 0; attempt < 500 && picked.Count < ShopSize; attempt++)
        {
            OriginalCard? next = Weighted(pool, e => ShopWeight(ledger, e), rng);
            if (next == null)
            {
                break;
            }
            if (picked.Any(p => p.Card().Id == next.Card().Id) || (picked.Count == ShopSize - 1 && SharesGenre(next, picked)))
            {
                continue;
            }
            picked.Add(next);
            ledger.MarkCardAppeared(next.Card());
        }
        // 原作末尾洗牌
        for (int i = 0; i < picked.Count; i++)
        {
            int j = rng.NextInt(picked.Count);
            (picked[i], picked[j]) = (picked[j], picked[i]);
        }
        return picked;
    }

    /// <summary>
    /// 商店权重：warrior × storeMul × 重复衰减；到达出现上限为 0。
    /// </summary>
    /// <param name="ledger">月圆手账。</param>
    /// <param name="entry">卡。</param>
    /// <returns>权重。</returns>
    private static long ShopWeight(FullMoonLedger ledger, OriginalCard entry)
    {
        return Decayed(ledger, entry, entry.Warrior * entry.StoreMul);
    }

    /// <summary>
    /// 宝箱权重：warrior × boxMul × 重复衰减；到达出现上限为 0。
    /// </summary>
    /// <param name="ledger">月圆手账。</param>
    /// <param name="entry">卡。</param>
    /// <returns>权重。</returns>
    private static long ChestWeight(FullMoonLedger ledger, OriginalCard entry)
    {
        return Decayed(ledger, entry, entry.Warrior * entry.BoxMul);
    }

    /// <summary>
    /// 乘上重复衰减 floor(100 × (repeat/10)^出现次数)；出现次数到上限时为 0。
    /// </summary>
    /// <param name="ledger">月圆手账。</param>
    /// <param name="entry">卡。</param>
    /// <param name="baseWeight">基础权重。</param>
    /// <returns>权重。</returns>
    private static long Decayed(FullMoonLedger ledger, OriginalCard entry, int baseWeight)
    {
        int count = ledger.AppearCount(entry.Card());
        if (baseWeight <= 0 || count >= entry.MaxSpawns)
        {
            return 0;
        }
        return baseWeight * (long)Math.Floor(100 * Math.Pow(entry.Repeat / 10.0, count));
    }

    /// <summary>
    /// 按牌组里各流派的张数加权挑一个流派（原作 GetGenreCardList）。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">随机数。</param>
    /// <returns>流派；牌组没有带流派的牌时为 0。</returns>
    private static int DeckGenre(Player player, Rng rng)
    {
        List<int> genres = player.Deck.Cards.SelectMany(c => EntryOf(player, c).Genres).ToList();
        return genres.Count == 0 ? 0 : genres[rng.NextInt(genres.Count)];
    }

    /// <summary>
    /// 是否和已选的牌共享流派（原作 IsAllSameGenre：第 3 张没有流派则不算）。
    /// </summary>
    /// <param name="entry">候选。</param>
    /// <param name="picked">已选。</param>
    /// <returns>共享时为 true。</returns>
    private static bool SharesGenre(OriginalCard entry, List<OriginalCard> picked)
    {
        return entry.Genres.Length > 0 && picked.Any(p => p.Genres.Intersect(entry.Genres).Any());
    }

    // ===== 宝箱 =====

    /// <summary>
    /// 宝箱抽 1 张（原作 GetChestCardId.GetChestCard）：本幕等级、RewardLimit 为 0、宝箱权重；抽中即记一次出现。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">事件随机数。</param>
    /// <param name="minTier">最低等级。</param>
    /// <param name="maxTier">最高等级。</param>
    /// <param name="exclude">不要的牌（谦虚宝箱更换、贪婪宝箱第 2 张）。</param>
    /// <returns>卡；没有可抽的为空。</returns>
    public static OriginalCard? Chest(Player player, Rng rng, int minTier, int maxTier,
        IEnumerable<OriginalCard>? exclude = null)
    {
        FullMoonLedger ledger = FullMoonLedger.Of(player);
        HashSet<ModelId> excluded = (exclude ?? []).Select(e => e.Card().Id).ToHashSet();
        // 候选
        List<OriginalCard> pool = Pool(player)
            .Where(e => e.Tier >= minTier && e.Tier <= maxTier && e.RewardLimit == 0 && e.Type != SpecialType
                && !excluded.Contains(e.Card().Id))
            .ToList();
        OriginalCard? picked = Weighted(pool, e => ChestWeight(ledger, e), rng)
            ?? Weighted(pool, e => (long)e.Warrior * e.BoxMul, rng);
        if (picked != null)
        {
            ledger.MarkCardAppeared(picked.Card());
        }
        return picked;
    }

    // ===== 收藏家 =====

    /// <summary>
    /// 收藏家换牌（原作 CommFunc.GetDealerCard）：等级在 [交出卡 −1, +4]，权重 warrior × boxMul（无衰减），
    /// 不出特殊牌，到出现上限的不出，和交出的牌不同名。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">事件随机数。</param>
    /// <param name="given">交出的牌。</param>
    /// <returns>卡；没有可抽的为空。</returns>
    public static OriginalCard? Dealer(Player player, Rng rng, CardModel given)
    {
        FullMoonLedger ledger = FullMoonLedger.Of(player);
        int tier = EntryOf(player, given).Tier;
        // 候选
        List<OriginalCard> pool = Pool(player)
            .Where(e => e.Tier >= tier - 1 && e.Tier <= tier + 4 && e.Type != SpecialType
                && ledger.AppearCount(e.Card()) < e.MaxSpawns && e.Card().Id != given.Id)
            .ToList();
        OriginalCard? picked = Weighted(pool, e => (long)e.Warrior * e.BoxMul, rng);
        if (picked != null)
        {
            ledger.MarkCardAppeared(picked.Card());
        }
        return picked;
    }

    // ===== 搞错的礼物 =====

    /// <summary>
    /// 搞错的礼物：别的角色的牌，等级 2–7（普通、罕见），不重复。
    /// 女骑士从原版角色的卡池抽；其他角色从别的角色（含女骑士）的卡池抽。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">事件随机数。</param>
    /// <param name="count">张数。</param>
    /// <returns>新牌（还没有牌堆）。</returns>
    public static List<CardModel> WrongGift(Player player, Rng rng, int count)
    {
        // 别的角色的卡池
        IEnumerable<CardPoolModel> pools = ModelDb.AllCharacterCardPools.Append(ModelDb.CardPool<KnightCardPool>())
            .Where(p => p.Id != player.Character.CardPool.Id)
            .DistinctBy(p => p.Id);
        List<CardModel> candidates = pools
            .SelectMany(p => p.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon)
            .DistinctBy(c => c.Id)
            .ToList();
        List<CardModel> picked = [];
        while (picked.Count < count && candidates.Count > 0)
        {
            int index = rng.NextInt(candidates.Count);
            picked.Add(player.RunState.CreateCard(candidates[index], player));
            candidates.RemoveAt(index);
        }
        return picked;
    }

    // ===== 工具 =====

    /// <summary>
    /// 按权重抽一个；总权重为 0 时为空。
    /// </summary>
    /// <param name="candidates">候选。</param>
    /// <param name="weight">权重函数。</param>
    /// <param name="rng">随机数。</param>
    /// <returns>抽中的卡。</returns>
    private static OriginalCard? Weighted(List<OriginalCard> candidates, Func<OriginalCard, long> weight, Rng rng)
    {
        List<long> weights = candidates.Select(weight).ToList();
        long total = weights.Sum();
        if (total <= 0)
        {
            return null;
        }
        long roll = (long)(rng.NextDouble() * total);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return candidates[i];
            }
        }
        return candidates[^1];
    }
}
