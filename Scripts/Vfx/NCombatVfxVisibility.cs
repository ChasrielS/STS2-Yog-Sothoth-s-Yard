using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace YogSothothsYard.Scripts;

public partial class NCombatVfxVisibility : Node
{
    private CanvasItem _visual = null!;

    public static void Attach(CanvasItem visual) =>
        visual.AddChild(new NCombatVfxVisibility { _visual = visual, ProcessPriority = -100 });

    public override void _EnterTree()
    {
        _visual ??= (CanvasItem)GetParent();
        ActiveScreenContext.Instance.Updated += UpdateVisibility;
    }
    public override void _Ready() => UpdateVisibility();
    public override void _Process(double delta) => UpdateVisibility();
    public override void _ExitTree() => ActiveScreenContext.Instance.Updated -= UpdateVisibility;

    public static bool IsBattleVisible
    {
        get
        {
            var screen = ActiveScreenContext.Instance.GetCurrentScreen();
            return NCombatRoom.Instance?.IsVisibleInTree() == true &&
                (screen is NCombatRoom || screen is Node selection &&
                    selection.GetNodeOrNull<NPeekButton>("%PeekButton")?.IsPeeking == true);
        }
    }

    private void UpdateVisibility() => _visual.Visible = IsBattleVisible &&
        (_visual is not NCardTrailVfx || !NCombatUi.IsDebugHidingPlayContainer);
}
