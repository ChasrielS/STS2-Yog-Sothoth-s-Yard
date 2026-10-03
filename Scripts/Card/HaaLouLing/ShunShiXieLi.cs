using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ShunShiXieLi : HaaLouLingCard
{
    public ShunShiXieLi() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13, ValueProp.Move)];
    public override bool GainsBlock => true;
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Follow;
    public override bool WantsGreenOutline => Knife > 0;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal original, out decimal modified)
    {
        modified = card == this && Knife > 0 ? Math.Max(0, original - 1) : original;
        return modified != original;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Block(play);
        if (eligible) await GainKnife(context, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
}
