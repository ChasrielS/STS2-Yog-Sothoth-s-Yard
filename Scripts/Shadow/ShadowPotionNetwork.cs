using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Networking.MessageExtensions;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowPotionNetwork
{
    private const int Version = 1;
    private static readonly ConditionalWeakTable<SerializablePlayer, byte[]> Ranks = new();

    [ThreadStatic]
    private static ReadOnlyMemory<byte> _receivedRanks;

    internal static void Register()
    {
        RitsuNetMessageTailExtensions.RegisterBytes<SyncPlayerDataMessage>("yog_sothoths_yard.potion_ranks", Version,
            static message => Ranks.TryGetValue(message.player, out var ranks) ? ranks : null,
            static (version, payload) =>
            {
                if (version == Version) _receivedRanks = payload;
            });
        Entry.Patcher.ApplyDynamicPatches([
            new("yog_sothoths_yard_shadow_potion_sync_write",
                AccessTools.DeclaredMethod(typeof(NetMessageBus), nameof(NetMessageBus.SerializeMessage))
                    .MakeGenericMethod(typeof(SyncPlayerDataMessage)),
                postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShadowPotionNetwork), nameof(Write))),
                isCritical: false)
        ]);
    }

    internal static void Capture(SerializablePlayer player, IReadOnlyDictionary<int, ShadowPotionSave> saved)
    {
        Ranks.Add(player, player.Potions.Select(potion => (byte)(saved.GetValueOrDefault(potion.SlotIndex)?.Rank ?? 0)).ToArray());
    }

    internal static bool TryGetRanks(SerializablePlayer player, out byte[] ranks) => Ranks.TryGetValue(player, out ranks!);

    internal static void Read(PacketReader reader, SerializablePlayer player)
    {
        _receivedRanks = default;
        RitsuNetMessageTailExtensions.Read<SyncPlayerDataMessage>(reader);
        if (_receivedRanks.Length > 0 && _receivedRanks.Length == player.Potions.Count)
            Ranks.Add(player, _receivedRanks.ToArray());
        _receivedRanks = default;
    }

    [HarmonyPriority(Priority.Last)]
    private static void Write(SyncPlayerDataMessage message, PacketWriter ____writer, ref int length, ref byte[] __result)
    {
        RitsuNetMessageTailExtensions.Write(____writer, message);
        length = ____writer.BytePosition;
        __result = ____writer.Buffer;
    }
}

public sealed class ShadowPotionSyncReadPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_sync_read";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NetMessageBus), nameof(NetMessageBus.TryDeserializeMessage),
            [typeof(byte[]), typeof(INetMessage).MakeByRefType(), typeof(ulong?).MakeByRefType()])];

    [HarmonyPriority(Priority.First)]
    public static void Postfix(bool __result, PacketReader ____reader, INetMessage? message)
    {
        if (__result && message is SyncPlayerDataMessage sync) ShadowPotionNetwork.Read(____reader, sync.player);
    }
}

public sealed class ShadowPotionSyncRelayPatch : IPatchMethod
{
    private delegate void Broadcast<T>(NetHostGameService host, T message, ulong excludePeer, int channel, ulong sender);

    private static readonly MethodInfo BroadcastMessage = AccessTools.DeclaredMethod(typeof(NetHostGameService), "BroadcastMessage");
    private static readonly MethodInfo OriginalRelay = BroadcastMessage.MakeGenericMethod(typeof(INetMessage));
    private static readonly Broadcast<INetMessage> Relay = OriginalRelay.CreateDelegate<Broadcast<INetMessage>>();
    private static readonly Broadcast<SyncPlayerDataMessage> RelayPlayer = BroadcastMessage
        .MakeGenericMethod(typeof(SyncPlayerDataMessage)).CreateDelegate<Broadcast<SyncPlayerDataMessage>>();

    public static string PatchId => "yog_sothoths_yard_shadow_potion_sync_relay";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NetHostGameService), nameof(NetHostGameService.OnPacketReceived),
            [typeof(ulong), typeof(byte[]), typeof(NetTransferMode), typeof(int)])];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        instructions.MethodReplacer(OriginalRelay, AccessTools.DeclaredMethod(typeof(ShadowPotionSyncRelayPatch), nameof(Forward)));

    private static void Forward(NetHostGameService host, INetMessage message, ulong excludePeer, int channel, ulong sender)
    {
        // Use the same concrete serializer for forwarding as for the original player snapshot.
        if (message is SyncPlayerDataMessage sync) RelayPlayer(host, sync, excludePeer, channel, sender);
        else Relay(host, message, excludePeer, channel, sender);
    }
}
