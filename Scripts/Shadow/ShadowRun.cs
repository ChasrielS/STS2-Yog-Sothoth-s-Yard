using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;
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
    private const string UnlockFlag = "yog_sothoths_yard_shadow_eligible";
    private const string PreferencesKey = "shadow_preferences";
    private static ModDataStore Preferences => RitsuLibFramework.GetDataStore(Entry.ModId);

    private static RunSavedData<ShadowSettings> _settings = null!;
    private static PlayerRunSavedData<ShadowPlayerState> _players = null!;
    private static int? _checkedProfile;

    internal static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Preferences.Register<ShadowPreferences>(PreferencesKey, "shadow_preferences.json", SaveScope.Profile,
                defaultFactory: static () => new ShadowPreferences(), autoCreateIfMissing: false);
        var store = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId);
        var options = new RunSavedDataOptions { SyncLobbyOnChange = true };
        _settings = store.Register("shadow", static () => new ShadowSettings(), options);
        _players = store.RegisterPerPlayer("shadow_player", static () => new ShadowPlayerState(), options);
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
        {
            var run = evt.RunState;
            if (Level(run) > 0 && (run.GameMode != GameMode.Standard || run.IsCustomGame ||
                run.Players.Any(player => player.Character is not HaaLouLing ||
                    !_players.TryGet(run, player.NetId, out var state) || !state.Unlocked)))
                _settings.Set(run, new ShadowSettings());
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(evt =>
        {
            foreach (var player in evt.RunState.Players) RestorePotions(player);
        }, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(evt =>
        {
            if (evt.IsVictory && evt.Run is { GameMode: GameMode.Standard, Ascension: 0, IsCustomGame: false } run &&
                LocalContext.GetMe(run)?.CharacterId == ModelDb.GetId<HaaLouLing>())
                SaveUnlock(SaveManager.Instance);
        }, replayCurrentState: false);
        ModUnlockRegistry.For(Entry.ModId).RegisterPostRunRule(PostRunEpochUnlockRule.Create(
            YinYingEpoch.EpochId,
            "以阴影1通关后获得阴影历史节点。",
            context => context.IsVictory && context.CharacterId == ModelDb.GetId<HaaLouLing>() &&
                Level(RunManager.Instance.DebugOnlyGetState()) == 1));
    }

    internal static int Level(RunState? run) =>
        run != null && _settings.TryGet(run, out var data) ? data.Level : 0;

    internal static int Level(Player? player) => Level(player?.RunState as RunState);

    internal static int SelectedLevel(StartRunLobby lobby) =>
        _settings.Lobby.TryGet(lobby, out var data) ? data.Level : 0;

    internal static int PreferredLevel(string character) => SaveManager.Instance.IsProfileInitialized
        ? Preferences.Get<ShadowPreferences>(PreferencesKey).Levels.GetValueOrDefault(character) : 0;

    internal static void ChangeSelection(StartRunLobby lobby, int level)
    {
        if (lobby.NetService.Type == NetGameType.Client || !CanSelect(lobby)) return;
        level = Math.Clamp(level, 0, MaxLevel);
        Select(lobby, level);
        var character = lobby.LocalPlayer.character.Id.Entry;
        if (!SaveManager.Instance.IsProfileInitialized || PreferredLevel(character) == level) return;
        Preferences.Modify<ShadowPreferences>(PreferencesKey, preferences => preferences.Levels[character] = level);
        Preferences.Save(PreferencesKey);
    }

    internal static bool IsLocalCharacterEligible(StartRunLobby lobby) =>
        lobby.GameMode == GameMode.Standard && lobby.LocalPlayer.character is HaaLouLing && HasUnlocked();

    internal static void SyncEligibility(StartRunLobby lobby, bool eligible)
    {
        var id = lobby.NetService.NetId;
        if (!_players.Lobby.TryGet(lobby, id, out var state) || state.Unlocked != eligible)
            _players.Lobby.Set(lobby, id, new ShadowPlayerState { Unlocked = eligible });
    }

    internal static bool CanSelect(StartRunLobby lobby) =>
        lobby.Players.Count > 0 && lobby.Players.All(player =>
            player.character is HaaLouLing &&
            _players.Lobby.TryGet(lobby, player.id, out var state) && state.Unlocked);

    internal static void Select(StartRunLobby lobby, int level)
    {
        if (lobby.NetService.Type == NetGameType.Client) return;
        level = CanSelect(lobby) ? Math.Clamp(level, 0, MaxLevel) : 0;
        if (level != SelectedLevel(lobby))
            _settings.Lobby.Set(lobby, new ShadowSettings { Level = level });
    }

    private static bool HasUnlocked()
    {
        var save = SaveManager.Instance;
        if (!save.IsProfileInitialized) return false;
        if (save.SeenPopup(UnlockFlag)) return true;
        var characterId = ModelDb.GetId<HaaLouLing>();
        if (save.Progress.GetStatsForCharacter(characterId)?.MaxAscension > 0)
            return true;
        if (_checkedProfile == save.CurrentProfileId) return false;
        _checkedProfile = save.CurrentProfileId;
        foreach (var name in save.GetAllRunHistoryNames())
        {
            if (save.LoadRunHistory(name).SaveData is not
                { Win: true, Ascension: 0, GameMode: GameMode.Standard, IsCustomGame: false } history)
                continue;
            var localId = PlatformUtil.GetLocalPlayerId(history.PlatformType);
            if (!history.Players.Any(player => player.Character == characterId &&
                    (history.Players.Count == 1 || player.Id == localId)))
                continue;
            SaveUnlock(save);
            return true;
        }
        return false;
    }

    private static void SaveUnlock(SaveManager save)
    {
        if (save.SeenPopup(UnlockFlag)) return;
        save.MarkFtueAsComplete(UnlockFlag);
        save.SaveProgressFile();
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

    internal static void SavePotions(Player player)
    {
        if (Level(player) == 0) return;
        var ranks = new Dictionary<int, ShadowPotionSave>();
        for (var slot = 0; slot < player.PotionSlots.Count; slot++)
            if (player.PotionSlots[slot] is { } potion && ShadowRanks.Of(potion) is > 0 and var rank)
                ranks.Add(slot, new ShadowPotionSave(potion.Id.Entry, rank));
        _players.Modify(player, state => state.Potions = ranks);
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
            (decimal)amount, ValueProp.Unpowered, null, null);
    }
}

public sealed class ShadowSettings
{
    public int Level { get; set; }
}

public sealed class ShadowPreferences
{
    public Dictionary<string, int> Levels { get; set; } = [];
}

public sealed class ShadowPlayerState
{
    public bool Unlocked { get; set; }
    public int Erosion { get; set; }
    public int StoredEnergy { get; set; }
    public int LastUnknownFloor { get; set; } = -1;
    public Dictionary<int, ShadowPotionSave> Potions { get; set; } = [];
}

public sealed record ShadowPotionSave(string Id, int Rank);
