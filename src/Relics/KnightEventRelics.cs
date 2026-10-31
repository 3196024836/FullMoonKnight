using MegaCrit.Sts2.Core.Models;

namespace FullMoonKnight.Relics;

/// <summary>
/// 事件发放的遗物分组：限次祝福（短暂的祝福）、负面乐章（梦魇音乐盒）、荆棘代价（荆棘丛）。
/// 都是图鉴实例。
/// </summary>
public static class KnightEventRelics
{
    /// <summary>9 个限次祝福（无尽魔力、魔力强化给法力，女骑士没有用法力的牌，暂不发放；类保留给旧存档）。</summary>
    public static IReadOnlyList<RelicModel> Temporary =>
    [
        ModelDb.Relic<TempComposure>(),
        ModelDb.Relic<TempMiniShield>(), ModelDb.Relic<TempNatureResistance>(), ModelDb.Relic<TempPiercing>(),
        ModelDb.Relic<TempPotentialBurst>(), ModelDb.Relic<TempScorchingFlame>(),
        ModelDb.Relic<TempSkillBreakthrough>(), ModelDb.Relic<TempWarHorn>(), ModelDb.Relic<TempWoundHealing>(),
    ];

    /// <summary>5 个负面乐章（梦魇音乐盒等）。</summary>
    public static IReadOnlyList<RelicModel> Movements =>
    [
        ModelDb.Relic<CurseBarrenMovement>(), ModelDb.Relic<CurseDarkMovement>(),
        ModelDb.Relic<CurseMelancholyMovement>(), ModelDb.Relic<CursePlagueMovement>(),
        ModelDb.Relic<CurseWitherMovement>(),
    ];

    /// <summary>8 种恶魔交易代价。</summary>
    public static IReadOnlyList<KnightCostRelic> Costs =>
    [
        ModelDb.Relic<CurseLesserLoseHp>(), ModelDb.Relic<CurseGreaterLoseHp>(),
        ModelDb.Relic<CurseLesserLoseMaxHp>(), ModelDb.Relic<CurseGreaterLoseMaxHp>(),
        ModelDb.Relic<CurseLesserPayGold>(), ModelDb.Relic<CurseGreaterPayGold>(),
        ModelDb.Relic<CurseLesserSlow>(), ModelDb.Relic<CurseGreaterSlow>(),
    ];
}
