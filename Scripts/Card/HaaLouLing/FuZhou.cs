using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class FuZhou : HaaLouLingCard
{
    public FuZhou() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(20, ValueProp.Move)];

    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HaaLouLingCardChoices.CurseTip(ErosionRank)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        await HaaLouLingCardChoices.AddCurses(Owner, 1, PileType.Discard, ShadowRanks.Of(this));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(6);
    }
}
