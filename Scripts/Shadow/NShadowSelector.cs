using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace YogSothothsYard.Scripts.Shadow;

public partial class NShadowSelector : Control
{
    private const string LocPrefix = "YOG_SOTHOTH_S_YARD_SHADOW_";
    private const string LeftArrow = "HBoxContainer/LeftArrowContainer/LeftArrow";
    private const string RightArrow = "HBoxContainer/RightArrowContainer/RightArrow";
    private static readonly AccessTools.FieldRef<NAscensionPanel, bool> ArrowsVisible =
        AccessTools.FieldRefAccess<NAscensionPanel, bool>("_arrowsVisible");
    private static readonly RitsuLibSidecarMessageDescriptor<int> PreviewMessage = new(
        Entry.ModId, "shadow_lobby_preview", static level => [(byte)(level + 1)],
        static data => data.Length == 1 ? data[0] - 1 : -1);
    private static readonly ConditionalWeakTable<StartRunLobby, Preview> ReceivedPreviews = new();

    private NAscensionPanel _ascension = null!;
    private NAscensionPanel _panel = null!;
    private NButton _nativeLeft = null!;
    private NButton _nativeRight = null!;
    private NButton _left = null!;
    private NButton _right = null!;
    private bool _refreshing;
    private bool? _isClient;
    private (StartRunLobby Lobby, string Character)? _selectionOwner;
    private readonly Dictionary<ulong, int> _sentPreviews = [];
    private IDisposable? _previewSubscription;
    private StartRunLobby? _hostingLobby;
    private double _previewRetry;
    private int _lastPreview = -1;

    internal static void Register() => ModNodeAttachmentRegistry.For(Entry.ModId)
        .RegisterReadyChild<NCharacterSelectScreen, NShadowSelector>(
            "shadow_difficulty", static _ => new NShadowSelector());

    internal static LocString Loc(string key) => new("gameplay_ui", LocPrefix + key);

    internal static int LobbyPreview(StartRunLobby lobby) =>
        ReceivedPreviews.TryGetValue(lobby, out var preview) ? preview.Level : -1;

    public override void _EnterTree() =>
        _previewSubscription = RitsuLibSidecarTypedMessageRegistry.Subscribe(PreviewMessage, ReceivePreview);

    public override void _ExitTree()
    {
        _previewSubscription?.Dispose();
        SetHostingLobby(null);
    }

    public override void _Ready()
    {
        _ascension = GetParent().GetNode<NAscensionPanel>("%AscensionPanel");
        _nativeLeft = _ascension.GetNode<NButton>(LeftArrow);
        _nativeRight = _ascension.GetNode<NButton>(RightArrow);
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Callable.From(() =>
        {
            if (IsInsideTree()) GetParent().MoveChild(this, _ascension.GetIndex());
        }).CallDeferred();

        _panel = GD.Load<PackedScene>("res://scenes/screens/ascension_panel.tscn").Instantiate<NAscensionPanel>();
        _left = _panel.GetNode<NButton>(LeftArrow);
        _right = _panel.GetNode<NButton>(RightArrow);
        _left.FocusMode = FocusModeEnum.All;
        _right.FocusMode = FocusModeEnum.All;
        AddChild(_panel);
        // Reuse the native panel's buttons without binding the ascension shortcuts a second time.
        _panel.Initialize(MultiplayerUiMode.Client, GameMode.Standard);
        _panel.GetNode<Control>("%LeftTabIcon").Hide();
        _panel.GetNode<Control>("%RightTabIcon").Hide();
        _panel.AnchorLeft = _ascension.AnchorLeft;
        _panel.AnchorRight = _ascension.AnchorRight;
        _panel.AnchorTop = _ascension.AnchorTop;
        _panel.AnchorBottom = _ascension.AnchorBottom;
        _panel.OffsetLeft = _ascension.OffsetLeft;
        _panel.OffsetRight = _ascension.OffsetRight;
        _panel.OffsetBottom = _ascension.OffsetTop - 12f;
        _panel.OffsetTop = _panel.OffsetBottom - (_ascension.OffsetBottom - _ascension.OffsetTop);
        _panel.MouseFilter = MouseFilterEnum.Pass;
        _panel.GetNode<Control>("Background").MouseFilter = MouseFilterEnum.Ignore;
        var icon = _panel.GetNodeOrNull<TextureRect>("%AscensionIconStandard")
            ?? _panel.GetNode<TextureRect>("%AscensionIcon");
        var levelLabel = icon.GetParent().GetNodeOrNull<MegaLabel>("AscensionLevel")
            ?? icon.GetNode<MegaLabel>("AscensionLevel");
        BlackFlame(icon);
        levelLabel.AddThemeColorOverride("font_outline_color", new Color("24181f"));
        _panel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(OnLevelChanged));
        _panel.MouseEntered += ShowTip;
        _panel.MouseExited += HideTip;
        _panel.VisibilityChanged += () =>
        {
            if (!_panel.IsVisibleInTree()) HideTip();
        };
        _panel.Hide();
    }

    public override void _Process(double delta)
    {
        if (GetParent() is not NCharacterSelectScreen { Lobby: { } lobby } screen || !screen.IsVisibleInTree())
        {
            _selectionOwner = null;
            SetHostingLobby(null);
            _panel.Hide();
            return;
        }

        var eligible = ShadowRun.IsLocalCharacterEligible(lobby);
        ShadowRun.SyncEligibility(lobby, eligible);
        var canSelect = ShadowRun.CanSelect(lobby);
        var isClient = lobby.NetService.Type == NetGameType.Client;
        if (!isClient) RestoreSelection(lobby, canSelect);
        var level = isClient ? LobbyPreview(lobby) : ShadowRun.SelectedLevel(lobby);
        var seenIntro = SaveManager.Instance.SeenPopup(ShadowRun.IntroPopup);
        var show = eligible && seenIntro && (isClient ? level >= 0 : canSelect && _ascension.Visible);

        SetHostingLobby(lobby.NetService.Type == NetGameType.Host ? lobby : null);
        if (_hostingLobby != null) SendPreview(_hostingLobby, show ? level : -1, delta);

        if (!show)
        {
            _panel.Hide();
            if (eligible && !seenIntro && NModalContainer.Instance is { OpenModal: null } modal)
                ShadowUnlockPopup.Show(modal);
            return;
        }

        if (!_panel.Visible || _panel.Ascension != level || _isClient != isClient)
        {
            var animate = !_panel.Visible;
            _refreshing = true;
            _isClient = isClient;
            ArrowsVisible(_panel) = !isClient;
            _panel.SetMaxAscension(ShadowRun.MaxLevel);
            _panel.SetAscensionLevel(level);
            _refreshing = false;
            HideTip();
            if (animate) _panel.AnimIn();
        }

        if (isClient || (!_nativeLeft.IsEnabled && !_nativeRight.IsEnabled)) _panel.Disable();
        else
        {
            _left.Enable();
            _right.Enable();
        }
    }

    private void ReceivePreview(RitsuLibSidecarTypedDispatchContext<int> message)
    {
        if (message.IsHostIngest || message.Message < -1 || message.Message > ShadowRun.MaxLevel) return;
        if (GetParent() is NCharacterSelectScreen { Lobby: { NetService: NetClientGameService { IsConnected: true } client } lobby }
            && message.SenderNetId == client.HostNetId)
            ReceivedPreviews.GetValue(lobby, static _ => new Preview()).Level = message.Message;
    }

    private void SetHostingLobby(StartRunLobby? lobby)
    {
        if (_hostingLobby == lobby) return;
        if (_hostingLobby != null) _hostingLobby.PlayerDisconnected -= ForgetPreview;
        _hostingLobby = lobby;
        if (_hostingLobby != null) _hostingLobby.PlayerDisconnected += ForgetPreview;
        _sentPreviews.Clear();
        _previewRetry = 0;
    }

    private void ForgetPreview(StartRunLobbyPlayer player) => _sentPreviews.Remove(player.id);

    private void SendPreview(StartRunLobby lobby, int level, double delta)
    {
        _previewRetry -= delta;
        if (level == _lastPreview && _previewRetry > 0) return;
        _lastPreview = level;
        _previewRetry = 0.2;
        foreach (var player in lobby.Players)
        {
            if (player.id == lobby.NetService.NetId || _sentPreviews.GetValueOrDefault(player.id, -2) == level) continue;
            if (RitsuLibSidecarTypedMessageRegistry.SendToPeer(lobby.NetService, player.id, PreviewMessage, level))
                _sentPreviews[player.id] = level;
        }
    }

    private sealed class Preview
    {
        internal int Level = -1;
    }

    private void RestoreSelection(StartRunLobby lobby, bool canSelect)
    {
        if (!canSelect)
        {
            _selectionOwner = null;
            ShadowRun.Select(lobby, 0);
            return;
        }
        var character = lobby.LocalPlayer.character.Id.Entry;
        var owner = (lobby, character);
        if (_selectionOwner == owner) return;
        _selectionOwner = owner;
        ShadowRun.Select(lobby, ShadowRun.PreferredLevel(character));
    }

    private void OnLevelChanged()
    {
        if (!_refreshing && GetParent() is NCharacterSelectScreen { Lobby: { } lobby })
            ShadowRun.ChangeSelection(lobby, _panel.Ascension);
        HideTip();
    }

    private void ShowTip()
    {
        if (_panel.Ascension == 0 || NModalContainer.Instance?.OpenModal != null) return;
        HideTip();
        var tip = NHoverTipSet.CreateAndShow(_panel, ShadowDifficultyUi.HoverTip());
        if (tip == null) return;

        var (flow, box) = ShadowDifficultyUi.FitHoverTip(tip);

        void Place()
        {
            var panel = _panel.GetGlobalRect();
            var viewport = GetViewportRect().Size;
            var size = box.GetCombinedMinimumSize().Max(box.Size);
            const float gap = 12f;
            var right = panel.End.X + gap;
            var beside = right + size.X <= viewport.X - gap;
            var x = beside ? right : panel.GetCenter().X - size.X * 0.5f;
            var y = beside ? panel.Position.Y : panel.Position.Y - size.Y - gap;
            flow.GlobalPosition = new Vector2(
                Math.Clamp(x, gap, Math.Max(gap, viewport.X - size.X - gap)),
                Math.Clamp(y, gap, Math.Max(gap, viewport.Y - size.Y - gap)));
            tip.SetFollowOwner();
        }

        box.Resized += Place;
        Place();
    }

    private void HideTip() => NHoverTipSet.Remove(_panel);

    internal static void BlackFlame(TextureRect icon)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/hsv.gdshader") };
        material.SetShaderParameter("h", 1f);
        material.SetShaderParameter("s", 0f);
        material.SetShaderParameter("v", 0.24f);
        icon.Material = material;
    }
}

public sealed class ShadowAscensionTextPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_ascension_text";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionPanel), "RefreshAscensionText", Type.EmptyTypes)];

    public static void Postfix(NAscensionPanel __instance, MegaRichTextLabel ____info)
    {
        if (__instance.GetParent() is not NShadowSelector) return;
        var key = __instance.Ascension == 0 ? "ZERO" : "ONE";
        var title = NShadowSelector.Loc(key + "_TITLE").GetFormattedText();
        var description = NShadowSelector.Loc(key + "_DESCRIPTION").GetFormattedText();
        ____info.Text = $"[b][gold]{title}[/gold][/b]\n{description}";
    }
}

internal static class ShadowUnlockPopup
{
    internal static readonly StringName Marker = new("yard_shadow_unlock");

    internal static void Show(NModalContainer modal)
    {
        var popup = NAscensionSingleplayerFtue.Create();
        if (popup == null) return;
        popup.SetMeta(Marker, true);
        modal.Add(popup);
    }
}

public sealed class ShadowUnlockPopupReadyPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unlock_popup_ready";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionSingleplayerFtue), nameof(NAscensionSingleplayerFtue._Ready), Type.EmptyTypes)];

    public static void Postfix(NAscensionSingleplayerFtue __instance)
    {
        if (!__instance.HasMeta(ShadowUnlockPopup.Marker)) return;
        var title = __instance.GetNode<MegaLabel>("%Header");
        title.ApplyLocaleFontSubstitution(FontType.Bold, "font");
        title.AddThemeColorOverride("font_color", ShadowDifficultyUi.TextColor);
        title.AddThemeConstantOverride("outline_size", 0);
        title.SetTextAutoSize(NShadowSelector.Loc("UNLOCK_TITLE").GetFormattedText());
        __instance.GetNode<MegaRichTextLabel>("%Description")
            .SetTextAutoSize(NShadowSelector.Loc("UNLOCK_BODY").GetFormattedText());
        __instance.GetNode<MegaRichTextLabel>("%Disclaimer")
            .SetTextAutoSize(NShadowSelector.Loc("UNLOCK_NOTE").GetFormattedText());
        NShadowSelector.BlackFlame(__instance.GetNode<TextureRect>("VerticalPopup/AscensionIcon"));
    }
}

public sealed class ShadowUnlockPopupConfirmPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unlock_popup_confirm";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionSingleplayerFtue), "CloseFtue", [typeof(NButton)])];

    public static bool Prefix(NAscensionSingleplayerFtue __instance)
    {
        if (!__instance.HasMeta(ShadowUnlockPopup.Marker)) return true;
        SaveManager.Instance.MarkFtueAsComplete(ShadowRun.IntroPopup);
        SaveManager.Instance.SaveProgressFile();
        NModalContainer.Instance?.Clear();
        return false;
    }
}
