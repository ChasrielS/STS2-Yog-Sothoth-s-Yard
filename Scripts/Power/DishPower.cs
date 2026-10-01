using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public abstract class DishPower : ModPowerTemplate
{
    protected virtual bool HasTurnStartEffect => false;
    private int _lastTurn;
    private int _triggeredAmount;

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState) =>
        TriggerTurnStart(player, context, combatState);
    public override Task BeforeHandDrawLate(Player player, PlayerChoiceContext context, ICombatState combatState) =>
        TriggerTurnStart(player, context, combatState);

    private async Task TriggerTurnStart(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (!HasTurnStartEffect || player.Creature != Owner) return;
        var turn = player.PlayerCombatState!.TurnNumber;
        if (_lastTurn != turn) { _lastTurn = turn; _triggeredAmount = 0; }
        var state = HaaLouLingCookingService.GetState(player);
        var amount = AmountOnTurnStart + state.DishStartGains.GetValueOrDefault(Id);
        var pending = amount - _triggeredAmount;
        if (pending <= 0) return;
        _triggeredAmount = amount;
        Flash();
        await OnTurnStart(player, context, combatState, pending);
    }

    protected virtual Task OnTurnStart(Player player, PlayerChoiceContext context, ICombatState combatState, int amount) => Task.CompletedTask;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (HasTurnStartEffect && power == this && Owner.Player is { } player)
        {
            var state = HaaLouLingCookingService.GetState(player);
            if (state.ProcessingTurnStart)
                state.DishStartGains[Id] = state.DishStartGains.GetValueOrDefault(Id) + (int)amount;
        }
        return Task.CompletedTask;
    }
}
