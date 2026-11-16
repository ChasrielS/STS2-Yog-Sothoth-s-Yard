using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.Persistence;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowCardDiscovery
{
    private const string Key = "shadow_card_discovery";
    private static ModDataStore Store => RitsuLibFramework.GetDataStore(Entry.ModId);

    internal static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Store.Register<ShadowCardDiscoveryData>(Key, "shadow_card_discovery.json", SaveScope.Profile,
                defaultFactory: static () => new ShadowCardDiscoveryData(), autoCreateIfMissing: false);
    }

    internal static bool HasSeen(CardModel card, int rank)
    {
        if (!SaveManager.Instance.IsProfileInitialized) return false;
        var data = Store.Get<ShadowCardDiscoveryData>(Key);
        return data.Seen.Contains(CardRankKey(card, rank));
    }

    internal static void Observe(CardModel card)
    {
        var rank = ShadowRanks.Of(card);
        if (rank == 0 || !SaveManager.Instance.IsProfileInitialized || !ShadowCardRules.TryGet(card, rank, out _)) return;
        var data = Store.Get<ShadowCardDiscoveryData>(Key);
        var key = CardRankKey(card, rank);
        if (data.Seen.Contains(key)) return;
        Store.Modify<ShadowCardDiscoveryData>(Key, data => data.Seen.Add(key));
        Store.Save(Key);
    }

    internal static void UnlockAll()
    {
        if (!SaveManager.Instance.IsProfileInitialized) return;
        var ranks = ModelDb.AllCards.SelectMany(card => ShadowCardRules.Available(card).Select(rank => CardRankKey(card, rank)));
        Store.Modify<ShadowCardDiscoveryData>(Key, data => data.Seen.UnionWith(ranks));
        Store.Save(Key);
    }

    private static string CardRankKey(CardModel card, int rank) => $"{card.Id.Entry}:{rank}";
}

public sealed class ShadowCardDiscoveryData
{
    public HashSet<string> Seen { get; set; } = [];
}

public sealed class ShadowCardSeenPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_seen";
    public static ModPatchTarget[] GetTargets() => [new(typeof(SaveManager), nameof(SaveManager.MarkCardAsSeen), [typeof(CardModel)])];

    public static void Postfix(CardModel card)
    {
        if (card.IsMutable && card.RunState != null && LocalContext.IsMine(card)) ShadowCardDiscovery.Observe(card);
    }
}

public sealed class ShadowUnlockAllCardsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unlock_all";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(UnlockConsoleCmd), nameof(UnlockConsoleCmd.Process), [typeof(Player), typeof(string[])])];

    public static void Postfix(string[] args, CmdResult __result)
    {
        if (__result.success && args.Length > 0 && args[0] == "all") ShadowCardDiscovery.UnlockAll();
    }
}
