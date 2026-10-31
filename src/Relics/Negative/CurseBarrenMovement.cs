using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·荒芜乐章（原作"绷带和泉水被瘟疫腐蚀，使用会降低生命上限"）：每次在休息处休息回复生命后，失去 3 点生命上限。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseBarrenMovement : KnightRelic
{
    /// <summary>每次失去的生命上限。</summary>
    private const decimal MaxHpLoss = 3m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        await LoseMaxHp(MaxHpLoss);
    }
}
