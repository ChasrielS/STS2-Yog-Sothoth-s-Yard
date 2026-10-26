using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NCookingRecipeFilter : Control
{
    private const string LocPrefix = "YOG_SOTHOTH_S_YARD_COOKING_";
    private CookingRecipePile _pile = null!;
    private NCardPileScreen _screen = null!;
    private Button _filter = null!;
    private PanelContainer _dropdown = null!;
    private VBoxContainer _filterItems = null!;
    private LineEdit _nameEdit = null!;
    private TextureButton _rename = null!;
    private Button _add = null!;
    private Button _remove = null!;
    private MegaRichTextLabel _bottomLabel = null!;
    private HaaLouLingCookingState _cookingState = null!;
    private CardPile _foodPile = null!;
    private int _renamingSlot;
    private bool _refreshQueued;

    public static NCookingRecipeFilter Create(NCardPileScreen screen, CookingRecipePile pile) => new()
    {
        Name = "CookingRecipeFilter",
        MouseFilter = MouseFilterEnum.Ignore,
        _screen = screen,
        _pile = pile
    };

    private static string Text(string key) => new LocString("gameplay_ui", LocPrefix + key).GetFormattedText();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var top = Math.Max(165f, NRun.Instance!.GlobalUi.RelicInventory.GetBottomOfInventory().Y + 16f);
        var grid = _screen.GetNode<NCardGrid>("CardGrid");
        grid.OffsetTop = top + 70f;
        grid.YOffset = -55;
        // 原版 ScrollContainer 是会移动的内容层，裁切必须放在固定的网格上。
        grid.ClipChildren = ClipChildrenMode.Disabled;
        grid.ClipContents = true;
        var scroll = grid.GetNode<Control>("ScrollContainer");
        scroll.ClipChildren = ClipChildrenMode.Disabled;
        scroll.ClipContents = false;
        _bottomLabel = _screen.GetNode<MegaRichTextLabel>("%BottomLabel");
        _bottomLabel.Visible = true;

        var bar = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(bar);
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        bar.OffsetLeft = 175f;
        bar.OffsetRight = -225f;
        bar.OffsetTop = top;
        bar.OffsetBottom = top + 64f;
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://images/ui/color_tab_bar.png"),
            Material = _pile.Player.Character.CardPool.FrameMaterial,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        bar.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var row = new HBoxContainer();
        bar.AddChild(row);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.OffsetLeft = 24f;
        row.OffsetRight = -24f;
        row.AddThemeConstantOverride("separation", 12);

        _filter = new Button
        {
            CustomMinimumSize = new Vector2(360f, 42f), Alignment = HorizontalAlignment.Left,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };
        StyleFilterButton(_filter);
        row.AddChild(_filter);
        // 下拉列表留在游戏视口内，不创建会令主窗口失焦的 PopupMenu 窗口。
        _dropdown = new PanelContainer { Visible = false, ZIndex = 20 };
        _dropdown.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("482628"), BorderColor = new Color("c9ac67"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            ContentMarginLeft = 2, ContentMarginRight = 2, ContentMarginTop = 4, ContentMarginBottom = 4
        });
        AddChild(_dropdown);
        _filterItems = new VBoxContainer();
        _filterItems.AddThemeConstantOverride("separation", 0);
        _dropdown.AddChild(_filterItems);
        _filter.Pressed += () =>
        {
            _dropdown.Visible = !_dropdown.Visible;
            _dropdown.GlobalPosition = _filter.GlobalPosition + new Vector2(0f, _filter.Size.Y);
            _dropdown.Size = new Vector2(_filter.Size.X, 0f);
        };
        _nameEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(360f, 42f), Visible = false,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        StyleFont(_nameEdit);
        row.AddChild(_nameEdit);
        _nameEdit.TextSubmitted += _ => CommitRename();
        _nameEdit.FocusExited += CommitRename;
        _rename = new TextureButton
        {
            CustomMinimumSize = new Vector2(44f, 48f), IgnoreTextureSize = true,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            TextureNormal = GD.Load<Texture2D>("res://images/atlases/ui_atlas.sprites/cursor_pencil.tres"),
            TooltipText = Text("PRESET_RENAME")
        };
        row.AddChild(_rename);
        _rename.Pressed += StartRename;
        row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _add = new Button { Text = Text("PRESET_ADD"), CustomMinimumSize = new Vector2(180f, 48f) };
        StyleButton(_add, "popup_confirm_button", mirror: true);
        row.AddChild(_add);
        _add.Pressed += () => TaskHelper.RunSafely(CookingRecipeScreen.EditPreset(_pile.Player,
            CookingMenuPreferences.Data.SelectedFilter - 2, adding: true));
        _remove = new Button { Text = Text("PRESET_REMOVE"), CustomMinimumSize = new Vector2(180f, 48f) };
        StyleButton(_remove, "popup_cancel_button", mirror: true);
        row.AddChild(_remove);
        _remove.Pressed += () => TaskHelper.RunSafely(CookingRecipeScreen.EditPreset(_pile.Player,
            CookingMenuPreferences.Data.SelectedFilter - 2, adding: false));

        var creature = _pile.Player.Creature;
        creature.PowerApplied += OnPowerChanged;
        creature.PowerRemoved += OnPowerChanged;
        creature.PowerIncreased += OnPowerIncreased;
        creature.PowerDecreased += OnPowerDecreased;
        _foodPile = Entry.FoodPile.GetPile(_pile.Player);
        _foodPile.ContentsChanged += QueueRefresh;
        _cookingState = HaaLouLingCookingService.GetState(_pile.Player);
        _cookingState.RecipePreviewsChanged += QueueRefresh;
        RefreshToolbar();
        RefreshInfo(hasAvailableRecipes: true);
    }

    public override void _ExitTree()
    {
        CommitRename();
        var creature = _pile.Player.Creature;
        creature.PowerApplied -= OnPowerChanged;
        creature.PowerRemoved -= OnPowerChanged;
        creature.PowerIncreased -= OnPowerIncreased;
        creature.PowerDecreased -= OnPowerDecreased;
        _foodPile.ContentsChanged -= QueueRefresh;
        _cookingState.RecipePreviewsChanged -= QueueRefresh;
    }

    private void OnPowerChanged(PowerModel power)
    {
        if (power is ChangFenPower or ChaoHeFenPower or TingZaiZhouPower or MoYuBaoPower or
            SuShiLengPanPower or SuanLaSaoZiTiJinPower or DaChuXingTaiPower or CaiCongXinShengPower)
            QueueRefresh();
    }

    private void OnPowerIncreased(PowerModel power, int amount, bool silent) => OnPowerChanged(power);
    private void OnPowerDecreased(PowerModel power, bool silent) => OnPowerChanged(power);

    private void QueueRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true;
        Callable.From(() =>
        {
            _refreshQueued = false;
            if (GodotObject.IsInstanceValid(this) && IsInsideTree()) RefreshRecipes();
        }).CallDeferred();
    }

    private void RefreshRecipes()
    {
        var available = HaaLouLingCookingService.GetCookableRecipes(_pile.Player);
        _pile.SetRecipes(CookingMenuPreferences.FilterRecipes(available));
        RefreshInfo(available.Count > 0);
    }

    private void RefreshInfo(bool hasAvailableRecipes)
    {
        var data = CookingMenuPreferences.Data;
        _bottomLabel.Text = "[center]" + Text(_pile.IsEmpty ? data.SelectedFilter switch
        {
            0 => "SCREEN_EMPTY", 1 => "HISTORY_EMPTY", _ => "PRESET_EMPTY"
        } : "SCREEN_TITLE");
        _add.Disabled = !hasAvailableRecipes;
        _remove.Disabled = data.SelectedFilter < 2 || !HaaLouLingCookingRecipes.All.Any(recipe =>
            data.Presets[data.SelectedFilter - 2].Dishes.Contains(recipe.DishModelId.Entry));
    }

    private void RefreshToolbar()
    {
        var data = CookingMenuPreferences.Data;
        foreach (var child in _filterItems.GetChildren())
        {
            _filterItems.RemoveChild(child);
            child.QueueFree();
        }
        var titles = new List<string> { Text("FILTER_DEFAULT"), Text("FILTER_HISTORY") };
        for (var i = 0; i < 5; i++)
        {
            var preset = data.Presets[i];
            var title = new LocString("gameplay_ui", LocPrefix + "FILTER_CUSTOM");
            title.Add("SlotNumber", i + 1);
            titles.Add(preset.Name.Length > 0 ? preset.Name : title.GetFormattedText());
        }
        _filter.Text = titles[data.SelectedFilter];
        for (var i = 0; i < titles.Count; i++)
        {
            var index = i;
            var option = new Button { Text = titles[i], Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(356f, 42f) };
            StyleFilterButton(option);
            _filterItems.AddChild(option);
            option.Pressed += () =>
            {
                _dropdown.Hide();
                CookingMenuPreferences.Update(value => value.SelectedFilter = index);
                QueueRefresh();
                RefreshToolbar();
                _filter.GrabFocus();
            };
        }
        _rename.Visible = _add.Visible = _remove.Visible = data.SelectedFilter >= 2;
    }

    private void StartRename()
    {
        _renamingSlot = CookingMenuPreferences.Data.SelectedFilter - 2;
        _nameEdit.Text = _filter.Text;
        _filter.Visible = false;
        _nameEdit.Visible = true;
        _nameEdit.GrabFocus();
        _nameEdit.Edit();
        _nameEdit.SelectAll();
    }

    private void CommitRename()
    {
        if (!_nameEdit.Visible) return;
        var name = _nameEdit.Text.Trim();
        _nameEdit.Visible = false;
        _filter.Visible = true;
        CookingMenuPreferences.Update(data => data.Presets[_renamingSlot].Name = name);
        RefreshToolbar();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsVisibleInTree()) return;
        if (_dropdown.Visible && inputEvent.IsActionPressed("ui_cancel"))
        {
            _dropdown.Hide();
            GetViewport().SetInputAsHandled();
        }
        if (_dropdown.Visible && inputEvent is InputEventMouseButton { Pressed: true } &&
            !_dropdown.GetGlobalRect().HasPoint(GetGlobalMousePosition()) &&
            !_filter.GetGlobalRect().HasPoint(GetGlobalMousePosition())) _dropdown.Hide();
        if (IsVisibleInTree() && _nameEdit.Visible && inputEvent is InputEventMouseButton { Pressed: true } &&
            !_nameEdit.GetGlobalRect().HasPoint(GetGlobalMousePosition())) CommitRename();
    }

    private static void StyleFilterButton(Button button)
    {
        StyleFont(button);
        button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        button.ClipText = true;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.AddThemeColorOverride("font_focus_color", new Color("efc851"));
        button.AddThemeColorOverride("font_pressed_color", new Color("efc851"));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed" })
            button.AddThemeStyleboxOverride(state, new StyleBoxFlat
            {
                BgColor = new Color(state is "hover" or "hover_pressed" ? "713337" : "572327"),
                ContentMarginLeft = 12, ContentMarginRight = 12,
                ContentMarginTop = 4, ContentMarginBottom = 4
            });
    }

    private static void StyleFont(Control control)
    {
        control.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres"));
        control.AddThemeFontSizeOverride("font_size", 26);
        control.AddThemeColorOverride("font_color", new Color("efc851"));
        control.AddThemeColorOverride("font_hover_color", new Color("fff6e2"));
        control.AddThemeColorOverride("font_outline_color", new Color("392a16"));
        control.AddThemeConstantOverride("outline_size", 4);
    }

    private static void StyleButton(Button button, string image, bool mirror = false)
    {
        StyleFont(button);
        button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var texture = GD.Load<Texture2D>($"res://images/atlases/ui_atlas.sprites/{image}.tres");
        if (mirror)
        {
            var background = new TextureRect
            {
                Texture = texture, FlipH = true, ShowBehindParent = true,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore
            };
            button.AddChild(background);
            background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            foreach (var state in new[] { "normal", "hover", "pressed", "disabled" })
                button.AddThemeStyleboxOverride(state, new StyleBoxEmpty { ContentMarginLeft = 20, ContentMarginRight = 20 });
            button.Draw += () => background.Modulate = button.Disabled ||
                button.GetDrawMode() is BaseButton.DrawMode.Pressed or BaseButton.DrawMode.HoverPressed
                ? new Color(0.8f, 0.8f, 0.8f) : button.IsHovered() ? new Color(1.15f, 1.15f, 1.15f) : Colors.White;
            return;
        }
        var normal = new StyleBoxTexture
        {
            Texture = texture,
            ContentMarginLeft = 20, ContentMarginRight = 20
        };
        button.AddThemeStyleboxOverride("normal", normal);
        var hover = (StyleBoxTexture)normal.Duplicate();
        hover.ModulateColor = new Color(1.15f, 1.15f, 1.15f);
        button.AddThemeStyleboxOverride("hover", hover);
        var pressed = (StyleBoxTexture)normal.Duplicate();
        pressed.ModulateColor = new Color(0.8f, 0.8f, 0.8f);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", pressed);
    }
}
