using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Utils.Persistence;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowRun
{
    internal const int MaxLevel = 1;
    internal const int ErosionLimit = 1000;
    internal const string IntroPopup = "yog_sothoths_yard_shadow_unlocked";
    private const string LegacyUnlockFlag = "yog_sothoths_yard_shadow_eligible";
    private const string PreferencesKey = "shadow_preferences";
    private static ModDataStore Preferences => RitsuLibFramework.GetDataStore(Entry.ModId);

    private static RunSavedData<ShadowSettings> _settings = null!;
    private static PlayerRunSavedData<ShadowPlayerState> _players = null!;

    internal static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Preferences.Register<ShadowPreferences>(PreferencesKey, "shadow_preferences.json", SaveScope.Profile,
                defaultFactory: static () => new ShadowPreferences(), autoCreateIfMissing: false);
        var store = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId);
        var options = new RunSavedDataOptions { SyncLobbyOnChange = true };
        _settings = store.Register("shadow", static () => new ShadowSettings());
        _players = store.RegisterPerPlayer("shadow_player", static () => new ShadowPlayerState(), options);
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(evt =>
        {
            if (evt.Reason == RunSavedDataLobbyStagingReason.Committing && (!evt.IsMultiplayer || evt.IsHost))
                CommitSelection(evt.Lobby);
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(evt =>
        {
            foreach (var player in evt.RunState.Players) RestorePotions(player);
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(RecordVictory, replayCurrentState: false);
        ModUnlockRegistry.For(Entry.ModId).RegisterPostRunRule(PostRunEpochUnlockRule.Create(
            YinYingEpoch.EpochId,
            "以阴影1通关后获得阴影历史节点。",
            context => context.IsVictory && IsYardCharacter(ModelDb.GetByIdOrNull<CharacterModel>(context.CharacterId)) &&
                Level(RunManager.Instance.DebugOnlyGetState()) == 1));
    }

    internal static int Level(RunState? run) =>
        run != null && _settings.TryGet(run, out var data) ? data.Level : 0;

    internal static int Level(Player? player) => Level(player?.RunState as RunState);

    internal static int SelectedLevel(StartRunLobby lobby) =>
        _settings.Lobby.TryGet(lobby, out var data) ? data.Level : 0;

    internal static int PreferredLevel(StartRunLobby lobby) => SaveManager.Instance.IsProfileInitialized
        ? Preferences.Get<ShadowPreferences>(PreferencesKey).GetPreferredLevel(SingleplayerCharacter(lobby)) : 0;

    internal static bool IsYardCharacter(CharacterModel? character) =>
        character != null && ModContentRegistry.TryGetOwnerModId(character.GetType(), out var owner) && owner == Entry.ModId;

    internal static int SyncLocalMaxLevel(StartRunLobby lobby)
    {
        var maxLevel = LocalMaxLevel(lobby);
        var id = lobby.NetService.NetId;
        if (!_players.Lobby.TryGet(lobby, id, out var state) || state.MaxLevel != maxLevel)
            _players.Lobby.Set(lobby, id, new ShadowPlayerState { MaxLevel = maxLevel });
        return maxLevel;
    }

    internal static int LobbyMaxLevel(StartRunLobby lobby) => SupportsShadow(lobby)
        ? lobby.Players.Min(player => _players.Lobby.TryGet(lobby, player.id, out var state)
            ? Math.Clamp(state.MaxLevel, 0, MaxLevel) : 0)
        : 0;

    internal static void Select(StartRunLobby lobby, int level)
    {
        if (lobby.NetService.Type == NetGameType.Client) return;
        level = Math.Clamp(level, 0, LobbyMaxLevel(lobby));
        if (level != SelectedLevel(lobby))
            _settings.Lobby.Set(lobby, new ShadowSettings { Level = level });
    }

    private static bool SupportsShadow(StartRunLobby lobby) =>
        lobby.GameMode == GameMode.Standard && !lobby.IsCustom && lobby.Players.Count > 0 &&
        lobby.Players.All(player => IsYardCharacter(player.character));

    private static string? SingleplayerCharacter(StartRunLobby lobby) =>
        lobby.NetService.Type == NetGameType.Singleplayer ? lobby.LocalPlayer.character.Id.Entry : null;

    private static int LocalMaxLevel(StartRunLobby lobby)
    {
        var save = SaveManager.Instance;
        var character = lobby.LocalPlayer.character;
        if (!save.IsProfileInitialized || lobby.GameMode != GameMode.Standard || lobby.IsCustom ||
            !IsYardCharacter(character)) return 0;

        var preferences = Preferences.Get<ShadowPreferences>(PreferencesKey);
        if (preferences.HighestClearedLevel.HasValue) return UnlockedMaxLevel(preferences);

        var previouslyUnlocked = save.SeenPopup(LegacyUnlockFlag) || save.SeenPopup(IntroPopup);
        var ascension = lobby.NetService.Type == NetGameType.Singleplayer
            ? save.Progress.GetStatsForCharacter(character.Id)?.MaxAscension ?? 0
            : save.Progress.MaxMultiplayerAscension;
        if (!previouslyUnlocked && ascension <= 0) return 0;

        SavePreferences(data =>
        {
            data.HighestClearedLevel = 0;
            if (previouslyUnlocked)
                data.MultiplayerLevel = data.Levels.GetValueOrDefault(ModelDb.GetId<HaaLouLing>().Entry);
        });
        return 1;
    }

    private static int UnlockedMaxLevel(ShadowPreferences preferences) =>
        preferences.HighestClearedLevel is { } cleared ? Math.Clamp(cleared, 0, MaxLevel - 1) + 1 : 0;

    private static void CommitSelection(StartRunLobby lobby)
    {
        SyncLocalMaxLevel(lobby);
        Select(lobby, SelectedLevel(lobby));
        var selected = SelectedLevel(lobby);
        if (!SupportsShadow(lobby) || !SaveManager.Instance.IsProfileInitialized || PreferredLevel(lobby) == selected) return;
        SavePreferences(data => data.SetPreferredLevel(SingleplayerCharacter(lobby), selected));
    }

    private static void RecordVictory(RunEndedEvent evt)
    {
        var manager = RunManager.Instance;
        if (!evt.IsVictory || !manager.ShouldSave || !SaveManager.Instance.IsProfileInitialized ||
            manager.DebugOnlyGetState() is not { GameMode: GameMode.Standard, IsCustomGame: false } run ||
            LocalContext.GetMe(run) is not { } player || !IsYardCharacter(player.Character)) return;

        var level = Level(run);
        var preferences = Preferences.Get<ShadowPreferences>(PreferencesKey);
        if (level <= preferences.HighestClearedLevel.GetValueOrDefault()) return;

        var previousMax = UnlockedMaxLevel(preferences);
        var singleplayerCharacter = run.Players.Count == 1 ? player.Character.Id.Entry : null;
        SavePreferences(data =>
        {
            data.HighestClearedLevel = level;
            var max = UnlockedMaxLevel(data);
            if (max > previousMax) data.SetPreferredLevel(singleplayerCharacter, max);
        });
    }

    private static void SavePreferences(Action<ShadowPreferences> update)
    {
        Preferences.Modify(PreferencesKey, update);
        Preferences.Save(PreferencesKey);
    }

    internal static int Erosion(Player player) =>
        player.RunState is RunState run && _players.TryGet(run, player.NetId, out var state) ? state.Erosion : 0;

    internal static void StoreEnergy(Player player, int amount) =>
        _players.Modify(player, state => state.StoredEnergy = amount);

    internal static int TakeStoredEnergy(Player player)
    {
        if (player.RunState is not RunState run || !_players.TryGet(run, player.NetId, out var state) ||
            state.StoredEnergy == 0) return 0;
        var amount = state.StoredEnergy;
        StoreEnergy(player, 0);
        return amount;
    }

    internal static bool EnterUnknownFloor(Player player)
    {
        if (player.RunState is not RunState run || !_players.TryGet(run, player.NetId, out var state) ||
            state.LastUnknownFloor == run.TotalFloor) return false;
        _players.Modify(player, data => data.LastUnknownFloor = run.TotalFloor);
        return true;
    }

    internal static Dictionary<int, ShadowPotionSave>? SavePotions(Player player)
    {
        if (Level(player) == 0) return null;
        var ranks = new Dictionary<int, ShadowPotionSave>();
        for (var slot = 0; slot < player.PotionSlots.Count; slot++)
            if (player.PotionSlots[slot] is { } potion && ShadowRanks.Of(potion) is > 0 and var rank)
                ranks.Add(slot, new ShadowPotionSave(potion.Id.Entry, rank));
        _players.Modify(player, state => state.Potions = ranks);
        return ranks;
    }

    internal static void RestorePotions(Player player)
    {
        if (player.RunState is not RunState run || !_players.TryGet(run, player.NetId, out var state)) return;
        foreach (var (slot, saved) in state.Potions)
            if ((uint)slot < (uint)player.PotionSlots.Count && player.PotionSlots[slot] is { } potion &&
                potion.Id.Entry == saved.Id && ShadowPotionRules.Supports(potion, saved.Rank))
                ShadowRanks.Set(potion, saved.Rank);
    }

    internal static async Task AddErosion(Player player, int amount, PlayerChoiceContext? context = null)
    {
        if (amount <= 0 || Level(player) == 0 || !player.Creature.IsAlive) return;
        var erosion = Erosion(player);
        if (erosion < ErosionLimit)
        {
            _players.Modify(player, state => state.Erosion = erosion + Math.Min(amount, ErosionLimit - erosion));
            ShadowSoulstoneIcons.Find(player)?.Flash();
            ShadowErosionGainVfx.Play(player.Creature);
            return;
        }
        await CreatureCmd.Damage(context ?? new ThrowingPlayerChoiceContext(), player.Creature,
            (decimal)amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }
}

public sealed class ShadowSettings
{
    public int Level { get; set; }
}

public sealed class ShadowPreferences
{
    // Null is locked. Zero means the profile has unlocked Shadow.
    public int? HighestClearedLevel { get; set; }
    public Dictionary<string, int> Levels { get; set; } = [];
    public int MultiplayerLevel { get; set; }

    internal int GetPreferredLevel(string? singleplayerCharacter) => singleplayerCharacter == null
        ? MultiplayerLevel : Levels.GetValueOrDefault(singleplayerCharacter);

    internal void SetPreferredLevel(string? singleplayerCharacter, int level)
    {
        if (singleplayerCharacter == null) MultiplayerLevel = level;
        else Levels[singleplayerCharacter] = level;
    }
}

public sealed class ShadowPlayerState
{
    public int MaxLevel { get; set; }
    public int Erosion { get; set; }
    public int StoredEnergy { get; set; }
    public int LastUnknownFloor { get; set; } = -1;
    public Dictionary<int, ShadowPotionSave> Potions { get; set; } = [];
}

public sealed record ShadowPotionSave(string Id, int Rank);
