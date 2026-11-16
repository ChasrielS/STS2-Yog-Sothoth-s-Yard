using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowEventGoldPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_gold";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PlayerCmd), nameof(PlayerCmd.GainGold), [typeof(decimal), typeof(Player), typeof(bool)])];
    public static void Prefix(Player player, ref decimal amount) => ShadowEvents.AdjustGold(player, ref amount);
}

public sealed class ShadowEventHealPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_heal";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal), [typeof(Creature), typeof(decimal), typeof(bool)])];
    public static void Prefix(Creature creature, ref decimal amount) => ShadowEvents.AdjustHeal(creature, ref amount);
}

public sealed class ShadowVegetationRestPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_vegetation_rest";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HealRestSiteOption), nameof(HealRestSiteOption.GetHealAmount), [typeof(Player)])];

    public static bool Prefix(Player player, ref decimal __result)
    {
        if (!ShadowEvents.IsVegetationRest(player)) return true;
        __result = Hook.ModifyRestSiteHealAmount(player.RunState, player.Creature, Math.Floor(player.Creature.MaxHp * 0.4m));
        return false;
    }
}

public sealed class ShadowDummyRewardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_dummy_rewards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BattlewornDummy), nameof(BattlewornDummy.Resume), [typeof(AbstractRoom)])];
    public static void Prefix(BattlewornDummy __instance, AbstractRoom room, out object? __state) =>
        ShadowEvents.BeginDummyRewards(__instance, room, out __state);
    public static void Postfix(object? __state) => ShadowEvents.RestoreChoice(__state);
}

public sealed class ShadowEventOfferRewardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_event_offer_rewards";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RewardsCmd), nameof(RewardsCmd.OfferCustom), [typeof(Player), typeof(List<Reward>)])];
    public static void Prefix(Player player, List<Reward> rewards) => ShadowEvents.PrepareAlreadyGeneratedRewards(player, rewards);
}
