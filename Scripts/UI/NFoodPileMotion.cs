using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace YogSothothsYard.Scripts.UI;

public partial class NFoodPileMotion : Node
{
    private NCombatPilesContainer _piles = null!;
    private NModCardPileButton? _food;
    private Vector2 _drawHome;
    private Vector2 _foodHome;

    public static void Register() =>
        ModNodeAttachmentRegistry.For(Entry.ModId)
            .RegisterReadyChild<NCombatPilesContainer, NFoodPileMotion>(
                "food_pile_motion",
                static piles => new NFoodPileMotion { _piles = piles, ProcessPriority = 100 });

    public override void _Ready() => _drawHome = _piles.DrawPile.Position;

    public override void _Process(double delta)
    {
        if (_food == null)
        {
            var food = _piles.GetChildren().OfType<NModCardPileButton>()
                .FirstOrDefault(button => button.Definition?.PileType == Entry.FoodPile && button.Visible);
            if (food == null) return;
            _food = food;
            _foodHome = food.Position;
        }

        // 抽牌堆的原版进出动画纵向位移为 100，沿用它的实时进度和缓动。
        var progress = (_piles.DrawPile.Position.Y - _drawHome.Y) / 100f;
        var movement = new Vector2(-_foodHome.X - _food.Size.X * 2f, 100f);
        _food.Position = _foodHome + movement * progress;
    }
}
