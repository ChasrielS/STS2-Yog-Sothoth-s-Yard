using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.HoverTips;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class SheZhang : HaaLouLingCard
{
    public SheZhang() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CanZhou>()];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var power = await PowerCmd.Apply<SheZhangPower>(context, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && ErosionRank == 3) power.DiscardPile = true;
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
