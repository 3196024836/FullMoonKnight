using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

using FullMoonKnight.Character;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events.Data;

/// <summary>
/// 一个原作祝福（LostThing.txt 的一行）和它在模组里的遗物。
/// </summary>
/// <param name="Id">原作编号。</param>
/// <param name="Relic">模组遗物（图鉴实例）。</param>
/// <param name="Level">原作等级：4/5 荆棘丛，6 普通，9 高级。</param>
/// <param name="Negative">原作 ng：有坏处的祝福。</param>
/// <param name="Weight">女骑士权重（warrior 列）。</param>
/// <param name="KnightOnly">只有女骑士能抽到（原作只给女骑士，或依赖女骑士的装备、反制等机制）。</param>
public sealed record ClassicBlessing(int Id, Func<RelicModel> Relic, int Level, bool Negative, int Weight,
    bool KnightOnly);

/// <summary>
/// 原作祝福登记表和抽取规则（原作 GetLostThingId）。
/// 祝福都只由事件给（稀有度"事件"，不进随机遗物奖励）。
/// 女骑士角色遗物池里的不进事件奖池：给装备槽的（有备而来、武器之书、禁忌附魔、武器大师）和船载火炮、冲锋号角、争强好胜。
/// 节日限定的（巧克力之棺、水晶头骨、亡灵之花）和幸运币也放进奖池；原作需要天梯解锁的视为已解锁。
/// </summary>
public static class ClassicBlessings
{
    /// <summary>抽不到时补的"小钱堆"（原作 1040）。</summary>
    public const int FallbackId = 1040;

    /// <summary>原作权重倍数（RandomLostThingByCareer：职业列 × 10）。</summary>
    private const int WeightScale = 10;

    /// <summary>事件随机奖池里的全部祝福。</summary>
    public static readonly IReadOnlyList<ClassicBlessing> Pool =
    [
        // 6 级（仙女祝福第 1、2 幕）
        new(2002, () => ModelDb.Relic<ScorchingFlame>(), 6, false, 30, false),
        new(1105, () => ModelDb.Relic<RallyHorn>(), 6, false, 20, true),
        new(1107, () => ModelDb.Relic<CampfireRest>(), 6, false, 15, false),
        new(1043, () => ModelDb.Relic<Hourglass>(), 6, false, 10, true), // 只给女骑士：其他角色 3 能量 +1 没有代价
        new(1077, () => ModelDb.Relic<CheeseTrap>(), 6, false, 10, true),
        new(1135, () => ModelDb.Relic<FollowRecipe>(), 6, false, 10, false),
        new(1001, () => ModelDb.Relic<GiftCard>(), 6, false, 5, false),
        new(1030, () => ModelDb.Relic<MiniShield>(), 6, false, 5, false),
        new(1031, () => ModelDb.Relic<EagleVigilance>(), 6, false, 5, true),
        new(1037, () => ModelDb.Relic<CatGrace>(), 6, false, 5, false),
        new(1041, () => ModelDb.Relic<BigMoneybag>(), 6, false, 5, false),
        new(1046, () => ModelDb.Relic<CleanSlate>(), 6, false, 5, false),
        new(1070, () => ModelDb.Relic<SkullAmulet>(), 6, false, 5, false),
        new(1080, () => ModelDb.Relic<Endurance>(), 6, false, 5, false),
        new(1124, () => ModelDb.Relic<HermitPotion>(), 6, false, 5, false),
        new(1137, () => ModelDb.Relic<SpecialMedicine>(), 6, false, 5, false),
        new(1139, () => ModelDb.Relic<WildFire>(), 6, false, 5, false),
        new(1142, () => ModelDb.Relic<DeerSwiftness>(), 6, false, 5, false),
        new(1621, () => ModelDb.Relic<MassProduction>(), 6, false, 5, false),
        new(2006, () => ModelDb.Relic<EvilSacrifice>(), 6, false, 5, false),
        // 原作节日限定和天梯祝福（原作不进事件奖池，这里补进来，权重取最低）
        new(1095, () => ModelDb.Relic<LuckyCoin>(), 6, false, 5, false),
        new(1096, () => ModelDb.Relic<ChocolateCoffin>(), 6, false, 5, false),
        new(1098, () => ModelDb.Relic<CrystalSkull>(), 6, false, 5, false),
        // 9 级（仙女祝福第 3 幕、女巫的奖赏）
        new(1104, () => ModelDb.Relic<Assault>(), 9, false, 30, true),
        new(1045, () => ModelDb.Relic<MoreMerrier>(), 9, false, 20, false),
        new(1047, () => ModelDb.Relic<IronDecision>(), 9, false, 10, false),
        new(1048, () => ModelDb.Relic<SwordTraining>(), 9, false, 10, false),
        new(1049, () => ModelDb.Relic<PhoenixFeather>(), 9, false, 10, false),
        new(1079, () => ModelDb.Relic<Gigantify>(), 9, false, 10, false),
        new(1120, () => ModelDb.Relic<JusticeRuling>(), 9, false, 10, false),
        new(1035, () => ModelDb.Relic<SlightOfHand>(), 9, false, 5, false),
        new(1082, () => ModelDb.Relic<LifePact>(), 9, false, 5, false),
        new(1083, () => ModelDb.Relic<Numb>(), 9, false, 5, false),
        new(1125, () => ModelDb.Relic<MagicStatue>(), 9, false, 5, false),
        new(2008, () => ModelDb.Relic<GuardianStatue>(), 9, false, 5, true),
        // 6 级负面（潘多拉魔盒）
        new(1056, () => ModelDb.Relic<Antique>(), 6, true, 5, true),
        new(1060, () => ModelDb.Relic<DemonHourglass>(), 6, true, 5, false),
        new(1061, () => ModelDb.Relic<HeavyChest>(), 6, true, 5, false),
        new(1062, () => ModelDb.Relic<FrenzyBook>(), 6, true, 5, false),
        new(1064, () => ModelDb.Relic<DemonCodex>(), 6, true, 5, false),
        new(1065, () => ModelDb.Relic<AngelCodex>(), 6, true, 5, true),
        new(1066, () => ModelDb.Relic<JesterTrick>(), 6, true, 5, false),
        new(1067, () => ModelDb.Relic<VampireCodex>(), 6, true, 5, false),
        new(1081, () => ModelDb.Relic<DoubleSword>(), 6, true, 5, false),
        new(1094, () => ModelDb.Relic<DeathPact>(), 6, true, 5, false),
        new(1117, () => ModelDb.Relic<GreedyHand>(), 6, true, 5, false),
        new(1130, () => ModelDb.Relic<CopyIllusion>(), 6, true, 5, false),
        new(1140, () => ModelDb.Relic<Bloodletting>(), 6, true, 5, false),
        new(1059, () => ModelDb.Relic<PigBackpack>(), 6, true, 5, true),
        new(1101, () => ModelDb.Relic<UndeadFlower>(), 6, true, 5, false),
        // 荆棘丛：第 2 幕 4 级，第 3 幕 5 级
        new(9100, () => ModelDb.Relic<CurseLesserLoseHp>(), 4, true, 5, false),
        new(9102, () => ModelDb.Relic<CurseLesserSlow>(), 4, true, 5, false),
        new(9106, () => ModelDb.Relic<CurseLesserPayGold>(), 4, true, 5, false),
        new(9108, () => ModelDb.Relic<CurseLesserLoseMaxHp>(), 4, true, 5, false),
        new(9101, () => ModelDb.Relic<CurseGreaterLoseHp>(), 5, true, 5, false),
        new(9103, () => ModelDb.Relic<CurseGreaterSlow>(), 5, true, 5, false),
        new(9107, () => ModelDb.Relic<CurseGreaterPayGold>(), 5, true, 5, false),
        new(9109, () => ModelDb.Relic<CurseGreaterLoseMaxHp>(), 5, true, 5, false),
    ];

    /// <summary>
    /// 按原作编号找登记的祝福（含奖池外的固定祝福和小钱堆）。
    /// </summary>
    /// <param name="id">原作编号。</param>
    /// <returns>祝福；没有时为空。</returns>
    public static ClassicBlessing? Find(int id)
    {
        return Pool.FirstOrDefault(b => b.Id == id) ?? Fixed.FirstOrDefault(b => b.Id == id);
    }

    /// <summary>不进随机奖池、由事件固定给出的祝福。</summary>
    public static readonly IReadOnlyList<ClassicBlessing> Fixed =
    [
        new(FallbackId, () => ModelDb.Relic<SmallMoneyPile>(), 1, false, 0, false),
        new(1085, () => ModelDb.Relic<BloodCompensation>(), 6, false, 0, false),
        new(1087, () => ModelDb.Relic<SluggishTrade>(), 6, false, 0, false),
        new(1088, () => ModelDb.Relic<Obsession>(), 6, false, 0, false),
        new(1089, () => ModelDb.Relic<Meditation>(), 6, false, 0, false),
        new(1090, () => ModelDb.Relic<Madness>(), 6, false, 0, false),
        new(9120, () => ModelDb.Relic<CurseDarkMovement>(), 3, true, 0, false),
        new(9121, () => ModelDb.Relic<CurseBarrenMovement>(), 3, true, 0, false),
        new(9122, () => ModelDb.Relic<CurseMelancholyMovement>(), 3, true, 0, false),
        new(9123, () => ModelDb.Relic<CursePlagueMovement>(), 3, true, 0, false),
        new(9124, () => ModelDb.Relic<CurseWitherMovement>(), 3, true, 0, false),
        new(14001, () => ModelDb.Relic<Remorse>(), 13, true, 0, false),
        new(14002, () => ModelDb.Relic<CreativePower>(), 13, true, 0, false),
    ];

    /// <summary>
    /// 按原作规则抽 <paramref name="count" /> 个不同的祝福：等级在范围内、负面与否一致，
    /// 按女骑士权重 × 10 加权，本局出现过的和已经持有的不抽，抽中立刻记为已出现；不够时用小钱堆补。
    /// 其他角色只抽通用祝福。
    /// </summary>
    /// <param name="player">玩家。</param>
    /// <param name="rng">随机数（事件随机数，联机同步）。</param>
    /// <param name="minLevel">最低等级。</param>
    /// <param name="maxLevel">最高等级。</param>
    /// <param name="negative">是否抽负面祝福。</param>
    /// <param name="count">个数。</param>
    /// <returns>原作编号列表（长度等于 count）。</returns>
    public static List<int> Draw(Player player, Rng rng, int minLevel, int maxLevel, bool negative, int count)
    {
        FullMoonLedger ledger = FullMoonLedger.Of(player);
        // 候选
        List<ClassicBlessing> candidates = Pool.Where(b => b.Level >= minLevel && b.Level <= maxLevel
                && b.Negative == negative && b.Weight > 0 && (!b.KnightOnly || player.Character is Knight)
                && !ledger.HasAppeared(b.Id) && player.GetRelicById(b.Relic().Id) == null)
            .ToList();
        // 抽中的
        List<int> picked = [];
        while (picked.Count < count && candidates.Count > 0)
        {
            ClassicBlessing blessing = WeightedPick(candidates, rng);
            candidates.Remove(blessing);
            ledger.MarkAppeared(blessing.Id);
            picked.Add(blessing.Id);
        }
        while (picked.Count < count)
        {
            picked.Add(FallbackId);
        }
        return picked;
    }

    /// <summary>
    /// 按权重 × 10 抽一个。
    /// </summary>
    /// <param name="candidates">候选（非空）。</param>
    /// <param name="rng">随机数。</param>
    /// <returns>抽中的祝福。</returns>
    private static ClassicBlessing WeightedPick(List<ClassicBlessing> candidates, Rng rng)
    {
        int roll = rng.NextInt(candidates.Sum(b => b.Weight * WeightScale));
        foreach (ClassicBlessing blessing in candidates)
        {
            roll -= blessing.Weight * WeightScale;
            if (roll < 0)
            {
                return blessing;
            }
        }
        return candidates[^1];
    }

    /// <summary>
    /// 新建原作编号对应的遗物（可直接获得的实例）。
    /// </summary>
    /// <param name="id">原作编号。</param>
    /// <returns>遗物。</returns>
    public static RelicModel Create(int id)
    {
        return (Find(id) ?? Find(FallbackId)!).Relic().ToMutable();
    }

    /// <summary>
    /// 原作底框：6 级非负面 0、9 级 1、负面 2（BansheePanel：level 6 且 ng≠1 → 0，否则 level 9 → 1，否则 ng → 2）。
    /// </summary>
    /// <param name="id">原作编号。</param>
    /// <returns>底框序号；不符合时为 -1（保持预制体默认）。</returns>
    public static int FrameIndex(int id)
    {
        ClassicBlessing? blessing = Find(id);
        if (blessing == null)
        {
            return -1;
        }
        if (blessing.Level == 6 && !blessing.Negative)
        {
            return 0;
        }
        if (blessing.Level == 9)
        {
            return 1;
        }
        return blessing.Negative ? 2 : -1;
    }

    /// <summary>
    /// 遗物对应的原作编号。
    /// </summary>
    /// <param name="relic">遗物。</param>
    /// <returns>原作编号；没有登记时为 0。</returns>
    public static int IdOf(RelicModel relic)
    {
        return Pool.Concat(Fixed).FirstOrDefault(b => b.Relic().Id == relic.Id)?.Id ?? 0;
    }
}
