using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public class HaaLouLingCookingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState
    )
    {
        if (player.Creature != Owner)
        {
            return;
        }

        await ExpandIfFull(player, choiceContext, beforeCooking: true);
        await HaaLouLingCookingService.Advance(choiceContext, player);
        await ExpandIfFull(player, choiceContext, beforeCooking: false);
    }

    private async Task ExpandIfFull(Player player, PlayerChoiceContext context, bool beforeCooking)
    {
        var state = HaaLouLingCookingService.GetState(player);
        if (state.Slots.Count < state.Capacity) return;
        var powers = Owner.Powers.OfType<TianZaoBasePower>()
            .Where(power => power.BeforeCooking == beforeCooking && power.AmountOnTurnStart > 0).ToList();
        foreach (var power in powers)
        {
            var slots = power.AmountOnTurnStart;
            await power.ExpandQueue(context);
            if (power is TianZaoDishPower dish) await dish.CookAddedDishes(context, slots);
        }
    }
}
