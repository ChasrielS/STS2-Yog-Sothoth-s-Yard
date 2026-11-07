using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(CurseCardPool))]
public sealed class CanZhou : HaaLouLingCard
{
    public CanZhou() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override int MaxUpgradeLevel => 0;
}
