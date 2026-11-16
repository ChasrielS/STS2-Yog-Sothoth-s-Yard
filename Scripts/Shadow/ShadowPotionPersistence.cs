using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowPotionSavePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_save";
    public static ModPatchTarget[] GetTargets() => [new(typeof(Player), nameof(Player.ToSerializable), Type.EmptyTypes)];
    public static void Prefix(Player __instance) => ShadowRun.SavePotions(__instance);
}

public sealed class ShadowPotionRestorePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_restore";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Player), nameof(Player.SyncWithSerializedPlayer), [typeof(SerializablePlayer)])];
    public static void Prefix(Player __instance) => ShadowRun.SavePotions(__instance);
    public static void Postfix(Player __instance) => ShadowRun.RestorePotions(__instance);
}

public sealed class ShadowRewardSendPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_reward_send";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RewardObtainedMessage), nameof(RewardObtainedMessage.Serialize), [typeof(PacketWriter)])];

    public static void Postfix(ref RewardObtainedMessage __instance, PacketWriter writer)
    {
        if (ShadowRun.Level(RunManager.Instance.DebugOnlyGetState()) == 0) return;
        if (__instance.rewardType == RewardType.Potion && __instance.potionModel is { } potion)
            writer.WriteInt(ShadowRanks.Of(potion), 2);
        if (__instance.rewardType == RewardType.Card && __instance.cardModel is { } card)
            writer.WriteBool(ShadowDeckAdditionPatch.WasEnchantedByLens(card));
    }
}

public sealed class ShadowRewardReceivePatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<PotionModel, Received> ReceivedPotions = new();
    public static string PatchId => "yog_sothoths_yard_shadow_reward_receive";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RewardObtainedMessage), nameof(RewardObtainedMessage.Deserialize), [typeof(PacketReader)])];

    public static void Postfix(ref RewardObtainedMessage __instance, PacketReader reader)
    {
        if (ShadowRun.Level(RunManager.Instance.DebugOnlyGetState()) == 0) return;
        if (__instance.rewardType == RewardType.Card && __instance.cardModel is { } card)
        {
            if (reader.ReadBool()) ShadowDeckAdditionPatch.MarkLens(card);
            return;
        }
        if (__instance.rewardType != RewardType.Potion || __instance.potionModel is not { } canonical) return;
        var rank = reader.ReadInt(2);
        if (rank == 0 || !ShadowPotionRules.Supports(canonical, rank)) return;
        var potion = canonical.ToMutable();
        ShadowRanks.Set(potion, rank);
        ReceivedPotions.Add(potion, new Received());
        __instance.potionModel = potion;
    }

    internal static bool TakeReceived(PotionModel potion) => ReceivedPotions.Remove(potion);
    private sealed class Received { }
}

public sealed class ShadowPotionRewardClonePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_reward_clone";
    public static ModPatchTarget[] GetTargets() => [new(typeof(PotionModel), nameof(PotionModel.ToMutable), Type.EmptyTypes)];

    public static bool Prefix(PotionModel __instance, ref PotionModel __result)
    {
        if (!ShadowRewardReceivePatch.TakeReceived(__instance)) return true;
        __result = __instance;
        return false;
    }
}
