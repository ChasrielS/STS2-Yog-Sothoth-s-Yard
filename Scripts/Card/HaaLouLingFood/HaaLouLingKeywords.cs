using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterOwnedCardKeyword(
    nameof(Ingredient),
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription
)]
[RegisterOwnedCardKeyword(nameof(AncientGlimpse))]
public sealed class HaaLouLingKeywords
{
    public static readonly CardKeyword Ingredient = ModContentRegistry
        .GetQualifiedKeywordId(Entry.ModId, nameof(Ingredient))
        .GetModCardKeyword();

    public static readonly CardKeyword AncientGlimpse = ModContentRegistry
        .GetQualifiedKeywordId(Entry.ModId, nameof(AncientGlimpse))
        .GetModCardKeyword();

}
