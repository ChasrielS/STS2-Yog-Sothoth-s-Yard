using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class AncientGlimpseRemovalPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_ancient_glimpse_removal";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Method(typeof(PowerCmd), nameof(PowerCmd.Remove), typeof(PowerModel))];
    public static bool Prefix(PowerModel power, ref Task __result)
    {
        if (power is not GuShenDeYiPiePower || !power.Owner.IsAlive || !CombatManager.Instance.IsInProgress) return true;
        __result = Task.CompletedTask;
        return false;
    }
}
