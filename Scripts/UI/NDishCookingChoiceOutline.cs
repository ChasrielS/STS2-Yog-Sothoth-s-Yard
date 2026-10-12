using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.UI;

public partial class NDishCookingChoiceOutline : Node
{
    private const string AttachmentId = "dish_cooking_choice_outline";
    private static readonly System.Reflection.FieldInfo CardsField =
        PrivateAccess.Field<NCardGridSelectionScreen>("_cards");

    private NSimpleCardSelectScreen _screen = null!;
    private NCardGrid _grid = null!;
    private readonly Dictionary<NCard, TextureRect> _outlines = [];
    private readonly Dictionary<HaaLouLingDishCard, CookingDishDefinition> _choices = [];
    private CookingDishSelection _selection = null!;
    private readonly HashSet<HaaLouLingDishCard> _unavailable = [];
    private PendingEffects? _lastEffects;
    private int _lastSelectionVersion = -1;

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NSimpleCardSelectScreen, NDishCookingChoiceOutline>(
                AttachmentId,
                static screen => new NDishCookingChoiceOutline { _screen = screen });

    public static bool TryGet(NCardGridSelectionScreen screen, out NDishCookingChoiceOutline display)
    {
        display = null!;
        return screen is NSimpleCardSelectScreen simple &&
               ModNodeAttachmentRegistry.For(Entry.ModId)
                   .TryGetAttached<NSimpleCardSelectScreen, NDishCookingChoiceOutline>(
                       simple, AttachmentId, out display) && display._choices.Count > 0;
    }

    public override void _Ready()
    {
        _grid = _screen.GetNode<NCardGrid>("%CardGrid");
        var cards = (IReadOnlyList<CardModel>)CardsField.GetValue(_screen)!;
        foreach (var card in cards.OfType<HaaLouLingDishCard>().Where(card => card.IsCookingChoice))
            _choices.Add(card, HaaLouLingCookingRecipes.Get(card.Id)!);
        SetProcess(_choices.Count > 0);
        if (_choices.Count == 0) return;
        _selection = _choices.Keys.First().CookingSelection!;
        RefreshPreviews();
    }

    public void RefreshPreviews()
    {
        var player = _choices.Keys.First().Owner;
        var effects = PendingEffects.From(player);
        if (_lastSelectionVersion == _selection.Version && _lastEffects == effects) return;
        _lastSelectionVersion = _selection.Version;
        _lastEffects = effects;
        _unavailable.Clear();
        var changed = new HashSet<CardModel>();

        // 已选菜肴依次占用次数；其他候选牌都预览下一次选择的效果。
        foreach (var card in _selection.Selected)
        {
            if (ApplyPreview(card, effects, changed))
                effects = effects.Reserve(card, _choices[card].GetCookingTurns(card.IsUpgraded) > 0);
        }
        foreach (var card in _choices.Keys)
            if (!_selection.Selected.Contains(card)) ApplyPreview(card, effects, changed);

        foreach (var holder in _grid.CurrentlyDisplayedCardHolders)
            if (changed.Contains(holder.CardModel)) holder.CardNode?.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
    }

    private bool ApplyPreview(HaaLouLingDishCard card, PendingEffects effects, HashSet<CardModel> changed)
    {
        var upgraded = effects.UpgradeAll || effects.Upgrades > 0;
        if (card.IsUpgraded != upgraded)
        {
            if (upgraded) card.UpgradeInternal();
            else card.DowngradeInternal();
            changed.Add(card);
        }
        var replays = effects.ReplayCount(card.Rarity);
        if (card.BaseReplayCount != replays)
        {
            card.BaseReplayCount = replays;
            changed.Add(card);
        }
        var recipe = _choices[card];
        var hasRoom = recipe.GetCookingTurns(upgraded) <= 0 || effects.OpenSlots > 0;
        if (!hasRoom) _unavailable.Add(card);
        return hasRoom && HaaLouLingCookingService.CanStartRecipe(card.Owner, recipe);
    }

    private readonly record struct PendingEffects(bool UpgradeAll, int Upgrades, int CommonReplays,
        int UncommonReplays, bool FirstDishReplay, int OpenSlots, bool LimitRepeatedDishes, int CookedDishCount)
    {
        public static PendingEffects From(Player player)
        {
            var creature = player.Creature;
            var state = HaaLouLingCookingService.GetState(player);
            var cuttlefish = creature.HasPower<MoYuBaoPower>();
            return new PendingEffects(cuttlefish || creature.GetPowerAmount<TingZaiZhouPower>() > 0,
                creature.GetPowerAmount<ChangFenPower>() + creature.GetPowerAmount<ChaoHeFenPower>(),
                creature.GetPowerAmount<SuShiLengPanPower>(), creature.GetPowerAmount<SuanLaSaoZiTiJinPower>(),
                cuttlefish && state.DishesStartedThisTurn == 0, state.Capacity - state.Slots.Count,
                creature.HasPower<CaiCongXinShengPower>(), state.CookedThisTurn.Count);
        }

        public int ReplayCount(CardRarity rarity) => (FirstDishReplay ? 1 : 0) +
            (rarity == CardRarity.Common && CommonReplays > 0 ||
             rarity == CardRarity.Uncommon && UncommonReplays > 0 ? 1 : 0);

        public PendingEffects Reserve(HaaLouLingDishCard card, bool usesSlot)
        {
            var remaining = this with
            {
                Upgrades = UpgradeAll ? Upgrades : Math.Max(0, Upgrades - 1),
                CommonReplays = card.Rarity == CardRarity.Common ? Math.Max(0, CommonReplays - 1) : CommonReplays,
                UncommonReplays = card.Rarity == CardRarity.Uncommon ? Math.Max(0, UncommonReplays - 1) : UncommonReplays,
                FirstDishReplay = false
            };
            if (usesSlot) return remaining;

            // 立刻完成的菜肴先结算，再开始下一道；数值沿用已分配升级的卡牌变量。
            var completions = 1 + card.BaseReplayCount;
            return card switch
            {
                ChangFen or ChaoHeFen => remaining with
                    { Upgrades = remaining.Upgrades + (int)card.DynamicVars["Amount"].BaseValue * completions },
                TingZaiZhou => remaining with { UpgradeAll = true },
                SuShiLengPan => remaining with { CommonReplays = remaining.CommonReplays + completions },
                SuanLaSaoZiTiJin => remaining with { UncommonReplays = remaining.UncommonReplays + completions },
                _ => remaining
            };
        }
    }

    public override void _Process(double delta)
    {
        RefreshPreviews();
        foreach (var outline in _outlines.Values)
            if (GodotObject.IsInstanceValid(outline)) outline.Visible = false;

        foreach (var holder in _grid.CurrentlyDisplayedCardHolders)
        {
            if (!holder.Visible || holder.CardNode is not { } node ||
                holder.CardModel is not HaaLouLingDishCard { IsCookingChoice: true } card) continue;

            // 原版高亮显示已选中的卡牌，红边只提示尚未选中的菜肴。
            if (!_unavailable.Contains(card) || _selection.Selected.Contains(card)) continue;

            var highlight = node.CardHighlight;
            if (!_outlines.TryGetValue(node, out var warning))
            {
                warning = (TextureRect)highlight.Duplicate(0);
                warning.Name = "CookingUnavailableOutline";
                warning.UniqueNameInOwner = false;
                warning.Modulate = NCardHighlight.red;
                warning.Material = (ShaderMaterial)highlight.Material.Duplicate();
                ((ShaderMaterial)warning.Material).SetShaderParameter("width", 0.075f);
                warning.MouseFilter = Control.MouseFilterEnum.Ignore;
                var parent = highlight.GetParent();
                parent.AddChild(warning);
                parent.MoveChild(warning, highlight.GetIndex());
                _outlines.Add(node, warning);
            }
            warning.Visible = true;
        }
    }

    public override void _ExitTree()
    {
        foreach (var outline in _outlines.Values)
            if (GodotObject.IsInstanceValid(outline)) outline.QueueFree();
        _outlines.Clear();
    }
}
