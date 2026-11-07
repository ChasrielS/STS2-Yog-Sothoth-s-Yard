using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.ValueProps;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XiJinWeiQun : HaaLouLingCard
{
    public XiJinWeiQun() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    public override bool GainsBlock => true;
    public override bool WantsConditionalHighlight => Owner?.PlayerCombatState != null &&
        HaaLouLingCookingService.GetState(Owner).DishesStartedThisTurn > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var extra = WantsConditionalHighlight;
        await Block(play);
        if (extra) await Block(play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}
