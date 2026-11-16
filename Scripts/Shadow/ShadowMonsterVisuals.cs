using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal sealed class ShadowErosionIntent(int amount) : AbstractIntent
{
    internal int Amount { get; } = amount;
    public override IntentType IntentType => IntentType.Unknown;
    protected override string IntentPrefix => "YOG_SOTHOTH_S_YARD_EROSION_INTENT";
    protected override string? SpritePath => null;
    public override IEnumerable<string> AssetPaths => [ShadowErosionIcons.SmallPath, ShadowErosionIcons.LargePath];

    public override Texture2D GetTexture(IEnumerable<Creature> targets, Creature owner) => ShadowErosionIcons.Small;

    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var description = base.GetIntentDescription(targets, owner);
        description.Add("Amount", Amount);
        return description;
    }
}

public sealed class ShadowErosionIntentVisualPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<NIntent, OriginalVisual> Original = new();
    public static string PatchId => "yog_sothoths_yard_shadow_erosion_intent_visual";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NIntent), "UpdateVisuals", Type.EmptyTypes)];

    public static bool Prefix(NIntent __instance, AbstractIntent ____intent, List<Texture2D> ____animationFrames,
        ref string? ____animationName)
    {
        var sprite = __instance.GetNode<Sprite2D>("%Intent");
        var label = __instance.GetNode<MegaRichTextLabel>("%Value");
        if (____intent is not ShadowErosionIntent intent)
        {
            if (Original.TryGetValue(__instance, out var original))
            {
                original.Spin.Kill();
                sprite.Rotation = original.Rotation;
                sprite.Scale = original.Scale;
                Original.Remove(__instance);
            }
            return true;
        }
        if (!Original.TryGetValue(__instance, out _))
        {
            var spin = sprite.CreateTween().SetLoops();
            spin.TweenProperty(sprite, "rotation", sprite.Rotation + Mathf.Tau, 2.4).From(sprite.Rotation);
            Original.Add(__instance, new OriginalVisual(spin, sprite.Rotation, sprite.Scale));
            sprite.Texture = ShadowErosionIcons.Large;
            sprite.Scale = Vector2.One * 0.88f;
            __instance.GetNode<CpuParticles2D>("%IntentParticle").Texture = ShadowErosionIcons.Small;
        }
        ____animationFrames.Clear();
        ____animationName = null;
        label.Text = intent.Amount.ToString();
        return false;
    }

    private sealed record OriginalVisual(Tween Spin, float Rotation, Vector2 Scale);
}

public sealed class ShadowMonsterVisualPatch : IPatchMethod
{
    private const int MaxMarks = 12;
    private static readonly StringName[] NativeTintParameters = ["h", "s", "v"];
    private static Shader? _normalShader;
    private static Shader? _additiveShader;

    public static string PatchId => "yog_sothoths_yard_shadow_monster_visual";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NCreature), nameof(NCreature._Ready), Type.EmptyTypes)];

    public static void Postfix(NCreature __instance)
    {
        if (__instance.Entity.Monster is not { } monster || ShadowMonsterRules.Rank(monster) != 1 ||
            __instance.Visuals.SpineBody is not { } spine ||
            ShadowMonsterErosionMarks.For(monster.Id.Entry) is not { } marks) return;

        var normal = _normalShader ??= GD.Load<Shader>("res://shaders/shadow/monster_erosion.gdshader");
        spine.SetNormalMaterial(CreateMaterial(normal, marks, spine.GetNormalMaterial()));
        if (monster is EyeWithTeeth or GasBomb)
        {
            var additive = _additiveShader ??= GD.Load<Shader>("res://shaders/shadow/monster_erosion_additive.gdshader");
            spine.BoundObject.Set("additive_material", CreateMaterial(additive, marks, spine.GetAdditiveMaterial()));
        }
        if (monster is BowlbugEgg && spine.BoundObject is Node node &&
            node.GetNodeOrNull<Node2D>("CocoonSlotNode/Cocoon") is { } cocoonNode)
        {
            var cocoon = new MegaSprite(cocoonNode);
            cocoon.SetNormalMaterial(CreateMaterial(normal, ShadowMonsterErosionMarks.BowlbugCocoon, cocoon.GetNormalMaterial()));
        }
    }

    private static ShaderMaterial CreateMaterial(Shader shader, IReadOnlyList<ShadowMonsterErosionMark> marks, Material? original)
    {
        var material = new ShaderMaterial { Shader = shader };
        if (original is ShaderMaterial tint)
            foreach (var parameter in NativeTintParameters)
            {
                var value = tint.GetShaderParameter(parameter);
                if (value.VariantType == Variant.Type.Float) material.SetShaderParameter(parameter, value);
            }

        var rects = new Vector4[MaxMarks];
        var sizes = new Vector2[MaxMarks];
        var paths = new Vector4[MaxMarks];
        var shapes = new Vector4[MaxMarks];
        for (var i = 0; i < marks.Count; i++)
        {
            rects[i] = marks[i].AtlasRect;
            sizes[i] = marks[i].AtlasSize;
            paths[i] = marks[i].Path;
            shapes[i] = marks[i].Shape;
        }
        material.SetShaderParameter("mark_count", marks.Count);
        material.SetShaderParameter("atlas_rects", rects);
        material.SetShaderParameter("atlas_sizes", sizes);
        material.SetShaderParameter("mark_paths", paths);
        material.SetShaderParameter("mark_shapes", shapes);
        return material;
    }
}
