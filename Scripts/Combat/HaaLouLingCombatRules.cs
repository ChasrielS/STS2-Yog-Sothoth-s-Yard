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

    public static Task GainKnife(PlayerChoiceContext context, Player player, int amount, CardModel? source = null)
    {
        if (amount <= 0 || player.Creature.HasPower<WuFaHuoDeDaoShiPower>()) return Task.CompletedTask;
        if (Knife(player) >= KnifeLimit(player) && player.Creature.HasPower<TuPoPingJingPower>())
            return PowerCmd.Apply<DaoFaJingJinPower>(context, player.Creature, amount, player.Creature, source);
        return PowerCmd.Apply<DaoShiPower>(context, player.Creature,
            Math.Min(amount, Math.Max(0, KnifeLimit(player) - Knife(player))), player.Creature, source);
    }

    public static async Task<int> SpendKnife(PlayerChoiceContext context, Player player, int amount, CardModel source)
    {
        var power = player.Creature.GetPower<DaoShiPower>();
        if (power == null || amount <= 0) return 0;
        var before = power.Amount;
        if (player.Creature.GetPower<CangFengPower>() is { } preserve)
        {
            await PowerCmd.Decrement(preserve);
            return Math.Min(amount, before);
        }
        await PowerCmd.ModifyAmount(context, power, -Math.Min(amount, before), player.Creature, source);
        var spent = before - Knife(player);
        if (spent > 0) HaaLouLingCookingService.GetState(player).KnifeSpentThisTurn = true;
        if (spent > 0 && player.GetRelic<XiaLuJiaDaoPu>() is { } manual)
            await manual.KnifeSpent(context, spent, source);
        return spent;
    }

    public static Task<int> SpendAllKnife(PlayerChoiceContext context, Player player, CardModel source) =>
        SpendKnife(context, player, Knife(player), source);

    public static int GuiYuan(Creature? creature) => creature == null ? 0 :
        creature.GetPowerAmount<GuiYuanPower>() + creature.GetPowerAmount<GuiYuanPlusPower>();

    public static bool CanExceedGuiYuanLimit(Creature? giver) => giver?.HasPower<GuiYuBenChuPower>() == true;

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
