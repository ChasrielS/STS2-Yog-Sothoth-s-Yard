using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class XianGong : HaaLouLingCard
{
    public XianGong() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(15, ValueProp.Move), new IntVar("Amount", 10)];
    public override bool GainsBlock => true;
    protected override bool UsesGuiYuan => true;
    public override bool WantsGreenOutline => Owner?.PlayerCombatState != null &&
        HaaLouLingCookingService.GetState(Owner).Slots.Count > 0;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        if (HaaLouLingCookingService.RemoveCookingSlots(Owner, 1, fromRight: true) > 0)
            await HaaLouLingCombatRules.GiveGuiYuanToEnemies(context, Owner, DynamicVars["Amount"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars["Amount"].UpgradeValueBy(5);
    }
}
