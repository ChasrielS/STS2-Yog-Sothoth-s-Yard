using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YuanYangFengHuan : HaaLouLingCard
{
    public YuanYangFengHuan() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override bool UsesKnife => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("RequiredKnife", 2)];

    protected override bool ShouldGlowRedInternal => Knife < DynamicVars["RequiredKnife"].IntValue;
    private int _extraRequiredKnife;

    [SavedProperty]
    public int ExtraRequiredKnife
    {
        get => _extraRequiredKnife;
        set
        {
            AssertMutable();
            var increase = value - _extraRequiredKnife;
            var requirement = DynamicVars["RequiredKnife"];
            _extraRequiredKnife = value;
            requirement.BaseValue += increase;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var requirement = DynamicVars["RequiredKnife"].IntValue;
        if (Knife >= requirement)
        {
            await SpendKnife(context, requirement);
            if (ErosionRank == 3 || ErosionRank == 2 && IsUpgraded)
                await Apply<YuanYangFengHuanIntentPower>(context, 1);
            else
                await Apply<YuanYangFengHuanPower>(context, 1);
        }
        ExtraRequiredKnife++;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RequiredKnife"].UpgradeValueBy(-1);
    }

    protected override void AfterDowngraded()
    {
        if (ErosionRank == 0) DynamicVars["RequiredKnife"].BaseValue = 2 + ExtraRequiredKnife;
    }
}
