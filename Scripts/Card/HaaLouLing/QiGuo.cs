using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class QiGuo : HaaLouLingCard
{
    public QiGuo() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HaaLouLingKeywords.ImmediateCookingTip];

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        HaaLouLingCookingService.CompleteLeftmostDish(context, Owner, includeRare: IsUpgraded);
}
