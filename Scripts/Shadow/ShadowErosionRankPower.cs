using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace YogSothothsYard.Scripts.Shadow;

[RegisterPower]
public sealed class ShadowErosionRankPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => false;
    public override bool ShouldPlayVfx => false;
    public override string? CustomIconPath => ShadowErosionIcons.SmallPath;
    public override string? CustomBigIconPath => ShadowErosionIcons.LargePath;
    internal string RankText => Amount switch { 3 => "III", 2 => "II", _ => "I" };

    public override LocString Title
    {
        get
        {
            var title = base.Title;
            title.Add("Rank", RankText);
            return title;
        }
    }

    public override LocString Description => Amount > 1
        ? new LocString("powers", Id.Entry + ".descriptionAdvanced") : base.Description;

    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    internal static void AddTo(Creature creature, int rank)
    {
        // The marker reflects a monster's existing rank; applying it must not trigger combat power effects.
        if (!creature.HasPower<ShadowErosionRankPower>())
            ModelDb.Power<ShadowErosionRankPower>().ToMutable().ApplyInternal(creature, rank, silent: true);
    }
}

public sealed class ShadowErosionRankPowerPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_rank_power_amount";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NPower), "RefreshAmount", Type.EmptyTypes)];

    public static void Postfix(PowerModel? ____model, MegaLabel ____amountLabel)
    {
        if (____model is ShadowErosionRankPower power) ____amountLabel.SetTextAutoSize(power.RankText);
    }
}
