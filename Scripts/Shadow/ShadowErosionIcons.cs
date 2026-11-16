using Godot;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowErosionIcons
{
    internal const string SmallPath = "res://images/ui/shadow/erosion_swirl.png";
    internal const string LargePath = "res://images/ui/shadow/erosion_intent_swirl.png";
    private static Texture2D? _small;
    private static Texture2D? _large;

    internal static Texture2D Small => _small ??= Tint(SmallPath);
    internal static Texture2D Large => _large ??= Tint(LargePath);

    private static Texture2D Tint(string path)
    {
        var source = GD.Load<Texture2D>(path);
        var image = source.GetImage();
        if (image.IsCompressed() && image.Decompress() != Error.Ok) return source;
        image.Convert(Image.Format.Rgba8);
        var dark = new Color("110d14");
        var highlight = new Color("493b48");
        for (var y = 0; y < image.GetHeight(); y++)
        for (var x = 0; x < image.GetWidth(); x++)
        {
            var pixel = image.GetPixel(x, y);
            if (pixel.A == 0f) continue;
            var luminance = pixel.R * 0.22f + pixel.G * 0.69f + pixel.B * 0.09f;
            var light = Mathf.SmoothStep(0.36f, 0.9f, luminance);
            var ink = dark.Lerp(highlight, light);
            ink.A = pixel.A;
            image.SetPixel(x, y, ink);
        }
        return ImageTexture.CreateFromImage(image);
    }
}
