using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JieZhouChuDao : HaaLouLingCard
{
    public JieZhouChuDao() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HaaLouLingCardChoices.CurseTip(ErosionRank)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
        var curse = HaaLouLingCardChoices.CreateCurse(Owner, ShadowRanks.Of(this));
        var result = await CardPileCmd.AddGeneratedCardToCombat(curse, PileType.Discard, Owner);
        CardCmd.PreviewCardPileAdd(result);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
    }
}
