using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal enum ShadowSoulstoneKind { QuSuiZhiQing, Stone11, Stone10, Stone7, Stone4 }

internal interface IShadowSoulstone
{
    ShadowSoulstoneKind SoulstoneKind { get; }
    (Color Deep, Color Light) ErosionMistColors { get; }
}

internal static class ShadowSoulstoneIcons
{
    private const string GreenPath = "res://images/relics/ling_hun_shi_qu_sui_zhi_qing";
    private static readonly Dictionary<(ShadowSoulstoneKind, int, bool), Texture2D> Textures = [];
    private static readonly Color Ink = new("17121c");
    private static readonly Color Vein = new("542437");
    private static readonly Shard[] Shards =
    [
        new(new(0.35f, 0.72f), -0.88f, 0.15f, 0.31f),
        new(new(0.55f, 0.64f), 0.72f, 0.22f, 0.27f),
        new(new(0.67f, 0.77f), 1.08f, 0.11f, 0.19f),
        new(new(0.47f, 0.83f), 1.34f, 0.08f, 0.15f),
        new(new(0.23f, 0.81f), -1.14f, 0.05f, 0.13f),
        new(new(0.80f, 0.82f), 1.62f, 0.06f, 0.13f)
    ];

    internal static RelicModel? Find(Player player) => player.Relics.FirstOrDefault(relic => relic is IShadowSoulstone);

    internal static int Stage(int erosion) => Math.Clamp(erosion / (ShadowRun.ErosionLimit / 4), 0, 4);

    internal static Texture2D Get(ShadowSoulstoneKind kind, int erosion, bool large = false)
    {
        var stage = Stage(erosion);
        var key = (kind, stage, large);
        if (Textures.TryGetValue(key, out var texture)) return texture;
        var path = kind == ShadowSoulstoneKind.QuSuiZhiQing
            ? GreenPath + (large ? ".png" : "_icon.png")
            : $"res://images/ui/shadow/soulstones/{kind.ToString().ToLowerInvariant()}.png";
        var source = GD.Load<Texture2D>(path);
        texture = stage == 0 ? source : DrawStage(source, stage, large);
        Textures.Add(key, texture);
        return texture;
    }

    private static Texture2D DrawStage(Texture2D source, int stage, bool large)
    {
        var image = source.GetImage();
        if (image.IsCompressed() && image.Decompress() != Error.Ok) return source;
        image.Convert(Image.Format.Rgba8);
        var size = large ? 256 : 85;
        if (image.GetWidth() != size) image.Resize(size, size, Image.Interpolation.Lanczos);
        if (stage == 4) return ImageTexture.CreateFromImage(Break(image, size));

        var radius = stage == 1 ? 0.34f : stage == 2 ? 0.62f : 1.16f;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var original = image.GetPixel(x, y);
            if (original.A == 0f) continue;
            var u = (x + 0.5f) / size;
            var v = (y + 0.5f) / size;
            var cloud = ShadowItemIcons.Noise(9f * u + 1.6f, 9f * v + 2.8f);
            var filament = ShadowItemIcons.Noise(23f * u + 3.6f, 18f * v + 7.2f);
            var distance = new Vector2((u - 0.5f) * 4.1f, (v - 0.49f) * 2.25f).Length() +
                (cloud - 0.5f) * 0.31f + (filament - 0.5f) * 0.13f;
            var depth = radius - distance;
            var darkness = Mathf.SmoothStep(-0.06f, 0.12f, depth) * 0.94f;
            var rim = 1f - Mathf.SmoothStep(0.008f, 0.13f, Math.Abs(depth - 0.035f));
            var color = Ink.Lerp(original, 0.07f + 0.10f * filament).Lerp(Vein, rim * filament * 0.56f);
            color = original.Lerp(color, darkness);
            color.A = original.A;
            image.SetPixel(x, y, color);
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static Image Break(Image source, int size)
    {
        var result = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        foreach (var shard in Shards)
        {
            var cos = MathF.Cos(shard.Rotation);
            var sin = MathF.Sin(shard.Rotation);
            var span = MathF.Max(shard.Width, shard.Length) * 0.63f;
            var start = (shard.Center - Vector2.One * span) * size;
            var end = (shard.Center + Vector2.One * span) * size;
            for (var y = (int)start.Y; y < end.Y && y < size; y++)
            for (var x = (int)start.X; x < end.X && x < size; x++)
            {
                if (x < 0 || y < 0) continue;
                var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size) - shard.Center;
                var u = (p.X * cos + p.Y * sin) / shard.Width + 0.5f;
                var v = (-p.X * sin + p.Y * cos) / shard.Length + 0.5f;
                var edge = MathF.Min(MathF.Min(v, 1f - v),
                    MathF.Min(u - MathF.Abs(v - 0.39f) * 0.95f, 1f - u - MathF.Abs(v - 0.55f) * 0.83f));
                if (edge <= 0f) continue;
                var facet = u > 0.55f - 0.29f * v ? 0.32f : 0.08f;
                var texture = source.GetPixel((int)((0.41f + u * 0.16f) * size), (int)((0.27f + v * 0.42f) * size));
                var glint = new Color("685563").Lerp(texture, 0.11f);
                var bevel = 1f - Mathf.SmoothStep(0.01f, 0.10f, edge);
                var color = Ink.Lerp(glint, facet + bevel * (0.35f + 0.22f * (1f - v)));
                color.A = Mathf.SmoothStep(0f, Math.Max(0.018f, 0.4f / (size * shard.Width)), edge);
                if (color.A >= result.GetPixel(x, y).A) result.SetPixel(x, y, color);
            }
        }
        return result;
    }

    private readonly record struct Shard(Vector2 Center, float Rotation, float Width, float Length);
}

public sealed class ShadowSoulstoneIconPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_soulstone_icons";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(RelicModel), "get_Icon", Type.EmptyTypes),
        new(typeof(RelicModel), "get_BigIcon", Type.EmptyTypes),
        new(typeof(RelicModel), "get_IconOutline", Type.EmptyTypes)
    ];

    public static void Postfix(RelicModel __instance, System.Reflection.MethodBase __originalMethod, ref Texture2D __result)
    {
        if (__instance is not IShadowSoulstone stone || !__instance.IsMutable || __instance.Owner is not { } owner ||
            ShadowRun.Level(owner) == 0) return;
        var erosion = ShadowRun.Erosion(owner);
        if (ShadowSoulstoneIcons.Stage(erosion) == 0 ||
            (__originalMethod.Name == "get_IconOutline" && erosion < ShadowRun.ErosionLimit)) return;
        __result = ShadowSoulstoneIcons.Get(stone.SoulstoneKind, erosion, __originalMethod.Name == "get_BigIcon");
    }
}

public sealed class ShadowSoulstoneFlashPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_soulstone_refresh_icon";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NRelicInventoryHolder), "OnRelicFlashed", [typeof(RelicModel), typeof(IEnumerable<Creature>)])];

    public static void Prefix(NRelicInventoryHolder __instance)
    {
        var visual = __instance.Relic;
        var relic = visual.Model;
        if (relic is IShadowSoulstone) visual.Model = relic;
    }
}
