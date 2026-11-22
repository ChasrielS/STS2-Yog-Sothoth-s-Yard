using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public static class HaaLouLingCombatRules
{
    private static readonly AsyncLocal<Creature?> GuiYuanTransferTarget = new();

    public static int Knife(Player player) => player.Creature.GetPowerAmount<DaoShiPower>();
    public static int KnifeLimit(Player player) => 3 + player.Creature.GetPowerAmount<DaoFaJingJinPower>();

    public static int KnifeForCard(CardModel card)
    {
        if (card.Owner?.PlayerCombatState == null) return 0;
        var plays = HaaLouLingCookingService.GetState(card.Owner).CardKnifeSnapshots;
        for (var i = plays.Count - 1; i >= 0; i--)
            if (plays[i].Card == card) return plays[i].Knife;
        return Knife(card.Owner);
    }

    public static async Task GainKnife(PlayerChoiceContext context, Player player, int amount, CardModel? source = null)
    {
        var creature = player.Creature;
        if (amount <= 0 || creature.HasPower<WuFaHuoDeDaoShiPower>()) return;
        var room = Math.Max(0, KnifeLimit(player) - Knife(player));
        if (room > 0 && creature.GetPower<QiShiPower>() is { Amount: > 0 } gain)
        {
            amount += gain.Amount;
            gain.FlashEffect();
        }
        var gained = Math.Min(amount, room);
        var overflow = amount - gained;
        if (gained > 0) await PowerCmd.Apply<DaoShiPower>(context, creature, gained, creature, source);
        if (overflow > 0 && creature.GetPower<TuPoPingJingPower>() is { Amount: > 0 } breakthrough)
        {
            breakthrough.FlashEffect();
            await PowerCmd.Apply<DaoFaJingJinPower>(context, creature, overflow * (decimal)breakthrough.Amount, creature, source);
        }
    }

    public static async Task<int> SpendKnife(PlayerChoiceContext context, Player player, int amount, CardModel source)
    {
        var power = player.Creature.GetPower<DaoShiPower>();
        if (power == null || amount <= 0) return 0;
        var before = power.Amount;
        await PowerCmd.ModifyAmount(context, power, -Math.Min(amount, before), player.Creature, source);
        var spent = before - Knife(player);
        if (spent > 0) HaaLouLingCookingService.GetState(player).KnifeSpentThisTurn = true;
        if (spent > 0 && player.GetRelic<XiaLuJiaDaoPu>() is { } manual)
            await manual.KnifeSpent(context, spent, source);
        if (spent > 0 && player.Creature.GetPower<ChengShiPower>() is { Amount: > 0 } restore)
        {
            restore.FlashEffect();
            await GainKnife(context, player, restore.Amount, source);
        }
        return spent;
    }

    internal static async Task<int> SpendAllKnife(PlayerChoiceContext context, CardPlay play)
    {
        var player = play.Player;
        var amount = Knife(player);
        if (play.IsFirstInSeries && player.Creature.GetPower<ZhuanShiPower>() is { Amount: > 0 } form)
        {
            form.FlashEffect();
            await PowerCmd.Decrement(form);
            return amount;
        }
        return await SpendKnife(context, player, amount, play.Card);
    }

    public static int GuiYuan(Creature? creature) => creature == null ? 0 :
        creature.GetPowerAmount<GuiYuanPower>() + creature.GetPowerAmount<GuiYuanPlusPower>();

    public static bool CanExceedGuiYuanLimit(Creature? giver) => giver?.HasPower<GuiYuBenChuPower>() == true;
    internal static bool IsReceivingTransferredGuiYuan(Creature target) => GuiYuanTransferTarget.Value == target;

    internal static async Task ReceiveTransferredGuiYuan(PlayerChoiceContext context, Creature target, int amount, CardModel source)
    {
        if (!target.IsAlive || target.HasPower<KaiShuiBaiCaiPower>()) return;
        var previous = GuiYuanTransferTarget.Value;
        GuiYuanTransferTarget.Value = target;
        try
        {
            var existing = target.Powers.OfType<GuiYuanBasePower>().FirstOrDefault();
            if (existing != null) await PowerCmd.ModifyAmount(context, existing, amount, null, source);
            else if (target.CombatState!.Players.Any(player => player.GetRelic<DaNuoGuiMian>() != null))
                await PowerCmd.Apply<GuiYuanPlusPower>(context, target, amount, null, source);
            else
                await PowerCmd.Apply<GuiYuanPower>(context, target, amount, null, source);
        }
        finally { GuiYuanTransferTarget.Value = previous; }
    }

    public static async Task GiveGuiYuan(PlayerChoiceContext context, Creature target, int amount, Creature giver, CardModel? source = null)
    {
        if (amount <= 0 || !target.IsAlive) return;
        var enhanced = giver.CombatState!.Players.Any(player => player.GetRelic<DaNuoGuiMian>() != null);
        var existing = target.Powers.OfType<GuiYuanBasePower>().FirstOrDefault();
        if (existing != null)
        {
            await PowerCmd.ModifyAmount(context, existing, amount, giver, source);
            return;
        }
        if (enhanced) await PowerCmd.Apply<GuiYuanPlusPower>(context, target, amount, giver, source);
        else await PowerCmd.Apply<GuiYuanPower>(context, target, amount, giver, source);
    }

    public static Task GiveGuiYuanToEnemies(PlayerChoiceContext context, Player player, int amount, CardModel? source = null) =>
        GiveGuiYuanGroup(context, player.Creature.CombatState!.HittableEnemies.Select(target => (target, amount)), player.Creature, source);

    public static async Task GiveGuiYuanGroup(PlayerChoiceContext context, IEnumerable<(Creature Target, int Amount)> targets,
        Creature giver, CardModel? source = null)
    {
        var entries = targets.Where(entry => entry.Target.IsAlive && entry.Amount > 0).ToList();
        if (entries.Count == 0) return;
        var amplifier = giver.GetPower<GuiYuanJuePower>();
        amplifier?.BeginGivingGroup();
        try
        {
            foreach (var (target, amount) in entries) await GiveGuiYuan(context, target, amount, giver, source);
        }
        finally { amplifier?.EndGivingGroup(); }
    }

    public static async Task RemoveGuiYuan(Creature target)
    {
        foreach (var power in target.Powers.OfType<GuiYuanBasePower>().ToList())
            await PowerCmd.Remove(power);
    }

    public static void GainAncientGlimpse(Player player)
    {
        var owner = player.Creature;
        var power = owner.GetPower<GuShenDeYiPiePower>();
        if (power == null)
        {
            power = (GuShenDeYiPiePower)ModelDb.Power<GuShenDeYiPiePower>().ToMutable();
            power.Applier = owner;
            power.ApplyInternal(owner, 1);
        }
        else power.SetAmount(Math.Min(4, power.Amount + 1));
    }
}
