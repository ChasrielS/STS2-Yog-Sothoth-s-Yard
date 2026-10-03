using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class DaoShiPersistencePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dao_shi_persistence";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method(typeof(PowerModel), nameof(PowerModel.ShouldRemoveDueToAmount))];

    public static void Postfix(PowerModel __instance, ref bool __result)
    {
        if (__instance is DaoShiPower && __instance.Amount == 0) __result = false;
    }
}
