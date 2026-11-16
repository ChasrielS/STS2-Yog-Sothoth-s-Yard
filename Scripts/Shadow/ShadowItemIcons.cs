using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowItemIcons
{
    private static readonly ConditionalWeakTable<Texture2D, Texture2D?[]> Cache = new();
    private static readonly ConditionalWeakTable<Texture2D, Image> AtlasImages = new();
    private static readonly float[] Coverage = [0f, 0.24f, 0.50f, 0.78f];
    private static readonly float[] Darkness = [0f, 0.83f, 0.91f, 0.96f];
    private static readonly float[] Rim = [0f, 0.62f, 0.77f, 0.88f];

    internal static Texture2D Get(Texture2D source, int rank)
    {
        if (rank is < 1 or > 3) return source;
        var textures = Cache.GetValue(source, static _ => new Texture2D?[3]);
        return textures[rank - 1] ??= Tint(source, rank);
    }

    private static Texture2D Tint(Texture2D source, int rank)
    {
        var image = ReadIcon(source);
        if (image == null) return source;
        var longest = Math.Max(image.GetWidth(), image.GetHeight());
        if (longest > 256)
            image.Resize(Math.Max(1, image.GetWidth() * 256 / longest), Math.Max(1, image.GetHeight() * 256 / longest),
                Image.Interpolation.Lanczos);
        var bounds = image.GetUsedRect();
        var visible = new List<(Vector2I Pixel, float Field, float Wisp)>();
        for (var y = bounds.Position.Y; y < bounds.End.Y; y++)
        for (var x = bounds.Position.X; x < bounds.End.X; x++)
        {
            if (image.GetPixel(x, y).A < 0.125f) continue;
            var u = (x - bounds.Position.X) / (float)Math.Max(1, bounds.Size.X - 1);
            var v = (y - bounds.Position.Y) / (float)Math.Max(1, bounds.Size.Y - 1);
            var field = 0.82f * u + 0.52f * v +
                0.52f * (Noise(u * 3.6f + 3.8f, v * 3.6f + 7.1f) - 0.5f) +
                0.18f * (Noise(u * 9 + 2, v * 9 + 4.2f) - 0.5f) +
                0.06f * (Noise(u * 19 + 8.3f, v * 19 + 6.1f) - 0.5f);
            visible.Add((new Vector2I(x, y), field, Noise(u * 8.3f + 24.1f, v * 7.7f + 13.4f)));
        }
        if (visible.Count == 0) return source;
        visible.Sort(static (a, b) => a.Field.CompareTo(b.Field));
        var limit = visible[(int)((visible.Count - 1) * Coverage[rank])].Field;
        var red = new Color("9d242d");
        foreach (var (pixel, field, wisps) in visible)
        {
            var original = image.GetPixelv(pixel);
            var depth = limit - field;
            var luminance = original.R * 0.22f + original.G * 0.69f + original.B * 0.09f;
            var line = Smooth(0.027f, 0.133f, luminance);
            var dark = Smooth(-0.025f, 0.042f, depth) * Darkness[rank] * line;
            var wisp = Smooth(0.24f, 0.78f, wisps);
            var glow = Math.Max(0, 1 - Math.Abs(depth - 0.057f) / 0.048f) *
                Mathf.Lerp(0.16f, 1f, wisp) * Rim[rank] * line +
                0.16f * wisp * dark * Math.Max(0, 1 - Math.Abs(depth - 0.14f) / 0.085f);
            var ink = new Color(0.039f + luminance * 0.1f, 0.027f + luminance * 0.075f,
                0.035f + luminance * 0.085f, original.A);
            var color = original.Lerp(ink, dark).Lerp(red, glow);
            color.A = original.A;
            image.SetPixelv(pixel, color);
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static Image? ReadIcon(Texture2D source)
    {
        if (source is not AtlasTexture { Atlas: { } atlas } texture) return Rgba(source);

        // AtlasTexture.GetImage() crops before it decompresses, and compressed images cannot be blitted.
        if (!AtlasImages.TryGetValue(atlas, out var image))
        {
            image = ReadIcon(atlas);
            if (image == null) return null;
            AtlasImages.Add(atlas, image);
        }
        var region = (Rect2I)texture.Region;
        if (region.Size.X <= 0 || region.Size.Y <= 0)
            region = new Rect2I(Vector2I.Zero, image.GetSize());
        var padded = Image.CreateEmpty(source.GetWidth(), source.GetHeight(), false, Image.Format.Rgba8);
        padded.BlitRect(image, region, (Vector2I)texture.Margin.Position);
        return padded;
    }

    private static Image? Rgba(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image == null || image.IsEmpty() || image.IsCompressed() && image.Decompress() != Error.Ok) return null;
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    private static float Smooth(float min, float max, float value)
    {
        var t = Math.Clamp((value - min) / (max - min), 0f, 1f);
        return t * t * (3 - 2 * t);
    }

    internal static float Noise(float x, float y)
    {
        var i = MathF.Floor(x);
        var j = MathF.Floor(y);
        var u = Smooth(0, 1, x - i);
        var v = Smooth(0, 1, y - j);
        return Mathf.Lerp(Mathf.Lerp(Hash(i, j), Hash(i + 1, j), u),
            Mathf.Lerp(Hash(i, j + 1), Hash(i + 1, j + 1), u), v);
    }

    private static float Hash(float x, float y)
    {
        var value = Math.Sin(127.1 * x + 311.7 * y + 19.2) * 43758.5453;
        return (float)(value - Math.Floor(value));
    }
}

public sealed class ShadowRelicIconPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_icons";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(RelicModel), "get_Icon", Type.EmptyTypes),
        new(typeof(RelicModel), "get_BigIcon", Type.EmptyTypes)
    ];
    public static void Postfix(RelicModel __instance, ref Texture2D __result)
    {
        if (__result != null) __result = ShadowItemIcons.Get(__result, ShadowRanks.Of(__instance));
    }
}

public sealed class ShadowPotionIconPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_icons";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(PotionModel), "get_Image", Type.EmptyTypes),
        new(typeof(PotionModel), "get_LargeImage", Type.EmptyTypes)
    ];
    public static void Postfix(PotionModel __instance, ref Texture2D __result)
    {
        if (__result != null) __result = ShadowItemIcons.Get(__result, ShadowRanks.Of(__instance));
    }
}
