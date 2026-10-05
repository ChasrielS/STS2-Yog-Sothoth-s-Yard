using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.UI;

public partial class NCookingQueueDisplay : Control
{
    private const string AttachmentId = "cooking_queue";
    private const float SlotSize = 56f;
    private const float SlotSpacing = 68f;
    private const float DashLength = 7f;
    private const float DashGap = 5f;
    private const float DashThickness = 2f;

    private static readonly Dictionary<ulong, NCookingQueueDisplay> Displays = [];

    private Player? _player;
    private bool _bindLocalPlayer;
    private int _lastCapacity = -1;
    private int _lastSlotHash;

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NCombatRoom, NCookingQueueDisplay>(
                AttachmentId,
                static room => new NCookingQueueDisplay
                {
                    _bindLocalPlayer = room.Mode == CombatRoomMode.ActiveCombat
                },
                new NodeAttachmentOptions
                {
                    Name = "HaaLouLingCookingQueueDisplay",
                    AttachParentSelector = parent => ((NCombatRoom)parent).CombatVfxContainer,
                    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName
                }
            );

    public static void EnsureFor(Player player)
    {
        if (Displays.TryGetValue(player.NetId, out var existing) && GodotObject.IsInstanceValid(existing))
        {
            if (!existing.IsInsideTree() && NCombatRoom.Instance is { } existingRoom)
            {
                existingRoom.CombatVfxContainer.AddChildSafely(existing);
            }

            return;
        }

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
        {
            return;
        }

        if (LocalContext.IsMe(player) &&
            ModNodeAttachmentRegistry.For(Entry.ModId)
                .TryGetAttached<NCombatRoom, NCookingQueueDisplay>(
                    combatRoom, AttachmentId, out var attached))
        {
            attached.Bind(player);
            return;
        }

        var display = new NCookingQueueDisplay
        {
            Name = "HaaLouLingCookingQueueDisplay",
            _player = player
        };

        Displays[player.NetId] = display;
        combatRoom.CombatVfxContainer.AddChildSafely(display);
    }

    public static void RefreshFor(Player player)
    {
        if (Displays.TryGetValue(player.NetId, out var display) && GodotObject.IsInstanceValid(display))
        {
            display.Rebuild(force: true);
        }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        if (_bindLocalPlayer)
        {
            CombatManager.Instance.CombatSetUp += OnCombatSetUp;
            if (CombatManager.Instance.IsStarting || CombatManager.Instance.IsInProgress)
            {
                OnCombatSetUp(CombatManager.Instance.DebugOnlyGetState()!);
            }
        }
        else
        {
            Rebuild(force: true);
        }
    }

    public override void _ExitTree()
    {
        if (_bindLocalPlayer)
        {
            CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        }

        if (_player != null && Displays.GetValueOrDefault(_player.NetId) == this)
        {
            Displays.Remove(_player.NetId);
        }
    }

    private void OnCombatSetUp(CombatState state)
    {
        foreach (var player in state.Players.Where(player => player.Character is HaaLouLing && player.PlayerCombatState != null))
        {
            if (LocalContext.IsMe(player)) Bind(player);
            else EnsureFor(player);
        }
    }

    private void Bind(Player player)
    {
        _player = player;
        Displays[player.NetId] = this;
        Rebuild(force: true);
    }

    public override void _Process(double delta)
    {
        if (_player == null || !CombatManager.Instance.IsInProgress)
        {
            // 附加节点可能早于战斗初始化就绪，随房间释放即可。
            Visible = false;
            return;
        }

        var combatRoom = NCombatRoom.Instance;
        var activeScreen = ActiveScreenContext.Instance.GetCurrentScreen();
        var isPeeking = activeScreen is Node screen && screen.GetNodeOrNull<NPeekButton>("%PeekButton")?.IsPeeking == true;
        if (combatRoom == null || activeScreen != combatRoom && !isPeeking)
        {
            Visible = false;
            return;
        }
        Visible = true;

        var creatureNode = combatRoom.GetCreatureNode(_player.Creature);
        if (creatureNode != null)
        {
            var visuals = creatureNode.Visuals;
            var bounds = visuals.Bounds;
            Scale = visuals.Scale.Abs();
            GlobalPosition = bounds.GlobalPosition + new Vector2(bounds.Size.X * 0.5f, 0f) * Scale
                + new Vector2(-Size.X * 0.5f, -82f) * Scale;
        }

        Rebuild(force: false);
    }

    private void Rebuild(bool force)
    {
        if (_player == null)
        {
            return;
        }

        var state = HaaLouLingCookingService.GetState(_player);
        var hash = state.Slots.Aggregate(state.Slots.Count, (current, slot) =>
            HashCode.Combine(
                current,
                slot.DishModelId,
                slot.RemainingTurns,
                slot.IsUpgraded,
                slot.ReplayCount
            ));

        if (!force && _lastCapacity == state.Capacity && _lastSlotHash == hash)
        {
            return;
        }

        _lastCapacity = state.Capacity;
        _lastSlotHash = hash;

        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        Size = new Vector2(
            Math.Max(1, state.Capacity) * SlotSize + Math.Max(0, state.Capacity - 1) * (SlotSpacing - SlotSize),
            SlotSize
        );

        var startX = 0f;
        for (var i = 0; i < state.Capacity; i++)
        {
            var slotRoot = new Control
            {
                Position = new Vector2(startX + i * SlotSpacing, 0f),
                Size = new Vector2(SlotSize, SlotSize),
                MouseFilter = MouseFilterEnum.Stop
            };
            AddChild(slotRoot);

            if (i >= state.Slots.Count)
            {
                AddDashedBorder(slotRoot);
                continue;
            }

            var slot = state.Slots[i];
            var recipe = HaaLouLingCookingRecipes.Get(slot.DishModelId);
            if (recipe != null)
            {
                slotRoot.MouseEntered += () => ShowDishHoverTip(slotRoot, slot, recipe);
                slotRoot.MouseExited += () => NHoverTipSet.Remove(slotRoot);

                var texture = GD.Load<Texture2D>(recipe.SlotTexturePath);
                if (texture != null)
                {
                    var icon = new TextureRect
                    {
                        Texture = texture,
                        ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        Size = new Vector2(SlotSize, SlotSize),
                        MouseFilter = MouseFilterEnum.Ignore
                    };
                    slotRoot.AddChild(icon);
                }
            }

            var label = new Label
            {
                Text = slot.RemainingTurns.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Position = new Vector2(SlotSize - 25f, SlotSize - 25f),
                Size = new Vector2(22f, 22f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            label.AddThemeFontSizeOverride("font_size", 18);
            slotRoot.AddChild(label);
        }
    }

    private static void ShowDishHoverTip(
        Control owner,
        CookingSlot slot,
        CookingDishDefinition recipe
    )
    {
        var card = (HaaLouLingDishCard)recipe.CreatePreviewCard().ToMutable();
        card.IsCookingCompletion = true;
        card.BaseReplayCount = slot.ReplayCount;
        if (slot.IsUpgraded)
        {
            card.UpgradeInternal();
        }

        var description = card.GetDescriptionForPile(MegaCrit.Sts2.Core.Entities.Cards.PileType.None);
        var title = new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_COOKING_SLOT_HOVER.title");
        title.Add("DishTitle", card.Title);

        var hoverDescription = new LocString(
            "gameplay_ui",
            "YOG_SOTHOTH_S_YARD_COOKING_SLOT_HOVER.description"
        );
        hoverDescription.Add("DishDescription", description);
        hoverDescription.Add("RemainingTurns", slot.RemainingTurns);

        var icon = GD.Load<Texture2D>(recipe.SlotTexturePath);
        NHoverTipSet.CreateAndShow(
            owner,
            new HoverTip(title, hoverDescription, icon),
            HoverTip.GetHoverTipAlignment(owner)
        )?.SetFollowOwner();
    }

    private static void AddDashedBorder(Control root)
    {
        for (var offset = 0f; offset < SlotSize; offset += DashLength + DashGap)
        {
            var horizontalLength = Math.Min(DashLength, SlotSize - offset);
            AddDash(root, new Vector2(offset, 0f), new Vector2(horizontalLength, DashThickness));
            AddDash(
                root,
                new Vector2(offset, SlotSize - DashThickness),
                new Vector2(horizontalLength, DashThickness)
            );

            var verticalLength = Math.Min(DashLength, SlotSize - offset);
            AddDash(root, new Vector2(0f, offset), new Vector2(DashThickness, verticalLength));
            AddDash(
                root,
                new Vector2(SlotSize - DashThickness, offset),
                new Vector2(DashThickness, verticalLength)
            );
        }
    }

    private static void AddDash(Control root, Vector2 position, Vector2 size)
    {
        root.AddChild(new ColorRect
        {
            Color = new Color(1f, 1f, 1f, 0.9f),
            Position = position,
            Size = size,
            MouseFilter = MouseFilterEnum.Ignore
        });
    }
}
