using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhuanHuoWeiFu : HaaLouLingCard
{
    public ZhuanHuoWeiFu() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Amount", 1), new EnergyVar(1), new CardsVar(0), new IntVar("KnifeLimit", 0), new IntVar("KnifeGain", 1)];

    protected override bool UsesKnife => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat(
            [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HaaLouLingKeywords.ImmediateCookingTip]);


    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (ErosionRank == 0)
        {
            if (IsUpgraded) await Apply<ZhuanHuoWeiFuPlusPower>(context, 1);
            else await Apply<ZhuanHuoWeiFuPower>(context, 1);
            return;
        }
        var rewards = new ZhuanHuoWeiFuBasePower.Rewards(DynamicVars["Amount"].IntValue, CardRarity.Uncommon,
            ErosionRank == 3 || IsUpgraded, DynamicVars["KnifeLimit"].IntValue,
            DynamicVars["KnifeGain"].IntValue, DynamicVars.Energy.IntValue, DynamicVars.Cards.IntValue);
        var existing = Owner.Creature.Powers.OfType<ZhuanHuoWeiFuErodedPower>().FirstOrDefault(power => power.Reward == rewards);
        if (existing != null)
        {
            await PowerCmd.ModifyAmount(context, existing, 1, Owner.Creature, this);
            return;
        }
        var power = (ZhuanHuoWeiFuErodedPower)ModelDb.Power<ZhuanHuoWeiFuErodedPower>().ToMutable();
        power.Settings = rewards;
        await PowerCmd.Apply(context, power, Owner.Creature, 1, Owner.Creature, this);
    }
}
