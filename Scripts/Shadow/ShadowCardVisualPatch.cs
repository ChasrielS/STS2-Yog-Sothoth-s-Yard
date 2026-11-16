using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowCardVisualPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<NCard, Visual> Visuals = new();
    private static ShaderMaterial? _edgeMaterial;
    public static string PatchId => "yog_sothoths_yard_shadow_card_visual";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NCard), nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)])];

    public static void Postfix(NCard __instance)
    {
        if (!__instance.IsNodeReady()) return;
        var card = __instance.Model;
        var rank = card != null && __instance.Visibility == ModelVisibility.Visible ? ShadowRanks.Of(card) : 0;
        if (rank == 0)
        {
            if (Visuals.TryGetValue(__instance, out var old))
            {
                old.Overlay.Hide();
                old.Icon.Hide();
            }
            return;
        }

        var visual = Visuals.GetValue(__instance, Create);
        visual.Overlay.Show();
        visual.Overlay.SetInstanceShaderParameter("erosion_rank", rank);

        var amount = ShadowRanks.CardCost(card!, rank);
        visual.Icon.Visible = amount > 0;
        visual.Label.SetTextAutoSize(amount.ToString());
        visual.Icon.Position = visual.Energy.Position + Vector2.Down * (visual.Energy.Size.Y * 0.72f);
        visual.Icon.Size = visual.Energy.Size;
    }

    private static ShaderMaterial CreateEdgeMaterial()
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/shadow/card_edges.gdshader") };
        material.SetShaderParameter("smoke_noise", GD.Load<Texture2D>("res://shaders/shadow/smoke_noise.tres"));
        return material;
    }

    private static Visual Create(NCard card)
    {
        var frame = card.GetNode<TextureRect>("%Frame").GetRect();
        var bounds = frame.Grow(18f);
        var text = card.GetNode<Control>("%DescriptionLabel").GetRect().Grow(2f);
        var textStart = text.Position - frame.GetCenter();
        var textEnd = text.End - frame.GetCenter();
        var overlay = new ColorRect
        {
            Name = "YardShadowFrame",
            Position = bounds.Position,
            Size = bounds.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = _edgeMaterial ??= CreateEdgeMaterial()
        };
        card.Body.AddChild(overlay);
        card.Body.MoveChild(overlay, card.GetNode<MegaLabel>("%TitleLabel").GetIndex());
        overlay.SetInstanceShaderParameter("description_bounds", new Vector4(textStart.X, textStart.Y, textEnd.X, textEnd.Y));
        overlay.SetInstanceShaderParameter("phase_offset", (float)(card.GetInstanceId() % 307) / 41f);

        var energy = card.GetNode<TextureRect>("%EnergyIcon");
        var icon = new TextureRect
        {
            Size = energy.Size,
            Texture = ShadowErosionIcons.Small,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var counters = energy.GetParent();
        counters.AddChild(icon);
        counters.MoveChild(icon, energy.GetIndex());
        var number = (MegaLabel)energy.GetNode("EnergyLabel").Duplicate((int)Node.DuplicateFlags.Scripts);
        number.UniqueNameInOwner = false;
        number.MouseFilter = Control.MouseFilterEnum.Ignore;
        number.AddThemeColorOverride("font_color", new Color("fff6e2"));
        number.AddThemeColorOverride("font_outline_color", new Color("24181f"));
        icon.AddChild(number);
        return new Visual(overlay, icon, number, energy);
    }

    private sealed record Visual(Control Overlay, TextureRect Icon, MegaLabel Label, TextureRect Energy);
}
