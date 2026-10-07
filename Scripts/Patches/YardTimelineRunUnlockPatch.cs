using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class YardTimelineRunUnlockPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_timeline_run_unlock";

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(SaveManager), nameof(SaveManager.InitProgressData), Type.EmptyTypes),
        new(typeof(SaveManager), nameof(SaveManager.GenerateUnlockStateFromProgress), Type.EmptyTypes)
    ];

    public static void Prefix(MethodBase __originalMethod, SaveManager __instance)
    {
        if (__originalMethod.Name == nameof(SaveManager.GenerateUnlockStateFromProgress))
            ObtainFromExistingRun(__instance);
    }

    public static void Postfix(MethodBase __originalMethod, SaveManager __instance)
    {
        if (__originalMethod.Name == nameof(SaveManager.InitProgressData))
            ObtainFromExistingRun(__instance);
    }

    private static void ObtainFromExistingRun(SaveManager saveManager)
    {
        var progress = saveManager.Progress;
        if (progress.IsEpochObtained(XunRenQiChengEpoch.EpochId))
            return;

        var stats = progress.GetStatsForCharacter(ModelDb.Character<Ironclad>().Id);
        if (stats == null || (stats.TotalWins == 0 && stats.TotalLosses == 0))
            return;

        saveManager.ObtainEpoch(XunRenQiChengEpoch.EpochId);
        saveManager.SaveProgressFile();
    }
}
