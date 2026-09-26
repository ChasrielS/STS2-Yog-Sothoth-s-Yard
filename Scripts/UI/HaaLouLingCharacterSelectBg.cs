using Godot;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingCharacterSelectBg : Control
{
    private static readonly Rect2 ShyTriggerArea = new(new Vector2(1080.0f, 150.0f), new Vector2(310.0f, 300.0f));

    private CanvasItem? _shyCg;
    private bool _isShyActive;

    public override void _Ready()
    {
        _shyCg = GetNodeOrNull<CanvasItem>("%ShyCg");

        if (_shyCg is not null)
        {
            _shyCg.Modulate = Colors.Transparent;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }
            && ShyTriggerArea.HasPoint(GetLocalMousePosition()))
        {
            ShowShyExpression();
        }
    }

    private void ShowShyExpression()
    {
        if (_shyCg is null || _isShyActive)
        {
            return;
        }

        _isShyActive = true;
        _shyCg.Modulate = Colors.Transparent;

        var expressionTween = CreateTween();
        expressionTween.TweenProperty(_shyCg, "modulate:a", 1.0f, 0.28f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        expressionTween.TweenInterval(1.35f);
        expressionTween.TweenProperty(_shyCg, "modulate:a", 0.0f, 0.65f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        expressionTween.TweenCallback(Callable.From(() => _isShyActive = false));
    }
}
