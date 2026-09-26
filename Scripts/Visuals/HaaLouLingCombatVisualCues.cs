using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace YogSothothsYard.Scripts;

internal static class HaaLouLingCombatVisualCues
{
    internal const string Idle = "idle_loop";
    internal const string Dead = "die";
    internal const string Hit = "hit";
    internal const string Attack = "attack";
    internal const string Cast = "cast";
    internal const string Relaxed = "relaxed_loop";

    private const float FrameDurationSeconds = 1f / 60f;
    private const int IdleFrameCount = 160;
    private const int DeathFrameCount = 13;
    private const int HitFrameCount = 89;
    private const string IdleFramePrefix = "res://images/characters/haa_lou_ling/combat/idle_sequence/idle_";
    private const string DeathFramePrefix = "res://images/characters/haa_lou_ling/combat/die_sequence/die_";
    private const string HitFramePrefix = "res://images/characters/haa_lou_ling/combat/hit_sequence/hit_";

    internal static readonly VisualCueSet Set = new(
        new Dictionary<string, string>
        {
            [Attack] = IdleFramePath(1),
            [Cast] = IdleFramePath(1)
        },
        new Dictionary<string, VisualFrameSequence>
        {
            [Idle] = BuildSequence(IdleFramePrefix, IdleFrameCount, true),
            [Dead] = BuildSequence(DeathFramePrefix, DeathFrameCount, false),
            [Hit] = BuildSequence(HitFramePrefix, HitFrameCount, false),
            [Relaxed] = BuildSequence(IdleFramePrefix, IdleFrameCount, true)
        });

    private static VisualFrameSequence BuildSequence(string framePrefix, int frameCount, bool loop) =>
        new(
            Enumerable.Range(1, frameCount)
                .Select(index => new VisualFrame($"{framePrefix}{index:000}.png", FrameDurationSeconds))
                .ToArray(),
            loop);

    private static string IdleFramePath(int index) => $"{IdleFramePrefix}{index:000}.png";
}
