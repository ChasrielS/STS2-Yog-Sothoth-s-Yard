using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YogSothothsYard.Scripts;

public static class HaaLouLingDishCompletedVfx
{
    public static void Show(Creature creature, string title)
    {
        var room = NCombatRoom.Instance;
        var creatureNode = room?.GetCreatureNode(creature);
        if (creatureNode == null || NCombatUi.IsDebugHideTextVfx) return;

        // 复用原版弹字的字体、描边和布局，不给角色施加临时能力。
        var template = PreloadManager.Cache.GetScene("res://scenes/vfx/power_applied_vfx.tscn").Instantiate<Control>();
        var label = template.GetNode<MegaLabel>("Label");
        template.RemoveChild(label);
        template.Free();
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(label);
        room!.CombatVfxContainer.AddChild(root);
        root.GlobalPosition = creatureNode.VfxSpawnPosition;
        label.Position += NCreature.PowerAppliedVfxPositionOffset;
        label.SetTextAutoSize(title);
        label.Modulate = StsColors.green;
        var tween = root.CreateTween().SetParallel();
        tween.TweenProperty(label, "position:y", label.Position.Y - 100f, 1.25)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(label, "modulate:a", 1f, 0.25).From(0f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        tween.TweenProperty(label, "modulate:a", 0f, 0.75).From(1f).SetDelay(0.25)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Expo);
        tween.Chain().TweenCallback(Callable.From(root.QueueFree));
    }
}
