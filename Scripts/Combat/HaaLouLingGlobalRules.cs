using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterSingleton]
public sealed class HaaLouLingGlobalRules : HookedSingletonModel
{
    public HaaLouLingGlobalRules() : base(HookType.Combat) { }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side == CombatSide.Player)
            foreach (var creature in participants)
                if (creature.Player is { } player)
                {
                    var cooking = HaaLouLingCookingService.GetState(player);
                    cooking.IngredientsUsedThisTurn = 0;
                    cooking.DishesStartedThisTurn = 0;
                    cooking.CookedThisTurn.Clear();
                    cooking.DishStartGains.Clear();
                    cooking.ProcessingTurnStart = true;
                    cooking.AwaitingNativeTurnStart = true;
                    cooking.PlatingGrantedAtTurnStart = false;
                    cooking.RefreshRecipePreviews();
                }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (side != CombatSide.Player) return;
        foreach (var creature in participants)
            if (creature.Player is { } player)
            {
                var cooking = HaaLouLingCookingService.GetState(player);
                cooking.AwaitingNativeTurnStart = false;
                var gains = cooking.PendingTurnStartPoison.ToList();
                cooking.PendingTurnStartPoison.Clear();
                foreach (var (amount, applier) in gains)
                    await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), creature, amount, applier, null);
            }
    }

    public override Task BeforeHandDrawLate(Player player, PlayerChoiceContext context, ICombatState state)
    {
        HaaLouLingCookingService.GetState(player).ProcessingTurnStart = false;
        return Task.CompletedTask;
    }
    public override Task BeforeCardPlayed(CardPlay play)
    {
        HaaLouLingCookingService.GetState(play.Player).CardKnifeSnapshots.Add((play.Card, HaaLouLingCombatRules.Knife(play.Player)));
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        var snapshots = HaaLouLingCookingService.GetState(play.Player).CardKnifeSnapshots;
        var index = snapshots.FindLastIndex(item => item.Card == play.Card);
        if (index >= 0) snapshots.RemoveAt(index);
        return Task.CompletedTask;
    }

    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        // 首次施加也需要限制修正后的数量；已有归元由自身处理额外获得层数。
        if (power is GuiYuanBasePower && HaaLouLingCombatRules.GuiYuan(target) == 0)
            modifiedAmount = Math.Clamp(amount, 0, 100);
        if (target.Player is { } player)
        {
            if (power is DaoShiPower)
            {
                var knife = HaaLouLingCombatRules.Knife(player);
                modifiedAmount = amount > 0 && target.Powers.Any(existing => existing is WuFaHuoDeDaoShiPower) ? 0 :
                    Math.Clamp(amount, -knife, Math.Max(0, HaaLouLingCombatRules.KnifeLimit(player) - knife));
            }
            if (power is PlatingPower { AmountOnTurnStart: 0 } && amount == -1 && applier == null)
            {
                var cooking = HaaLouLingCookingService.GetState(player);
                // 本轮才获得的覆甲没有度过上一轮，不参与这次原版扣层。
                if (cooking.AwaitingNativeTurnStart && cooking.PlatingGrantedAtTurnStart) modifiedAmount = 0;
            }
            if (power is DaoFaJingJinPower or GuoWanPiaoPenPower)
                modifiedAmount = Math.Max(amount, -target.Powers.Where(existing => existing.Id == power.Id).Sum(existing => existing.Amount));
        }
        return modifiedAmount != amount;
    }
}
