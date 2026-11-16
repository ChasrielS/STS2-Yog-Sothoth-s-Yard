using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TianZao : HaaLouLingCard
{
    public TianZao() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var amount = DynamicVars["Amount"].IntValue;
        var rank = ErosionRank;
        if (rank == 0 && !IsUpgraded)
            await Apply<TianZaoPower>(context, amount);
        else if (rank < 2 && !(rank == 1 && IsUpgraded))
            await Apply<TianZaoPlusPower>(context, amount);
        else
        {
            CardRarity? rarity = rank == 3 || rank == 2 && IsUpgraded ? CardRarity.Uncommon : null;
            var upgraded = rank == 3 && IsUpgraded;
            var existing = Owner.Creature.Powers.OfType<TianZaoDishPower>()
                .FirstOrDefault(power => power.Rarity == rarity && power.UpgradeDish == upgraded);
            if (existing != null)
                await PowerCmd.ModifyAmount(context, existing, amount, Owner.Creature, this);
            else
            {
                var power = (TianZaoDishPower)ModelDb.Power<TianZaoDishPower>().ToMutable();
                power.Rarity = rarity;
                power.UpgradeDish = upgraded;
                await PowerCmd.Apply(context, power, Owner.Creature, amount, Owner.Creature, this);
            }
        }
    }
}
