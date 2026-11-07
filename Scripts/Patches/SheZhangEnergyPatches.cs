using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class SheZhangResourcePermissionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_she_zhang_resource_permission";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<PlayerCombatState>(nameof(PlayerCombatState.HasEnoughResourcesFor),
            typeof(CardModel), typeof(UnplayableReason).MakeByRefType())];

    public static void Postfix(PlayerCombatState __instance, CardModel card, ref UnplayableReason reason, ref bool __result)
    {
        if (card.Owner?.Creature.HasPower<SheZhangPower>() != true ||
            card.EnergyCost.GetWithModifiers(CostModifiers.All) <= __instance.Energy) return;
        reason &= ~(UnplayableReason.EnergyCostTooHigh | UnplayableReason.StarCostTooHigh);
        if (card.GetStarCostWithModifiers() > __instance.Stars) reason |= UnplayableReason.StarCostTooHigh;
        __result = reason == UnplayableReason.None;
    }
}

public sealed class SheZhangEnergyPaymentPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_she_zhang_energy_payment";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<CardEnergyCost>(nameof(CardEnergyCost.GetAmountToSpend))];

    public static void Postfix(CardModel ____card, ref int __result)
    {
        if (____card.Owner?.PlayerCombatState is { } state && ____card.Owner.Creature.HasPower<SheZhangPower>())
            __result = Math.Min(__result, Math.Max(0, state.Energy));
    }
}
