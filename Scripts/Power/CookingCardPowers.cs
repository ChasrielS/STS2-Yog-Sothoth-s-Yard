using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class SuiShouBeiCaiPower : ThisTurnHaaLouLingPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature == Owner && play.Card.Type == CardType.Attack)
            await HaaLouLingCookingService.AddRandomIngredients(context, play.Player, Amount, CardRarity.Common);
    }
}

[RegisterPower]
public sealed class GuoWanPiaoPenPower : HaaLouLingPower
{
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) HaaLouLingCookingService.RefreshCapacity(Owner.Player!);
        return Task.CompletedTask;
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        HaaLouLingCookingService.RefreshCapacity(oldOwner.Player!);
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class CaiLanPower : HaaLouLingPower
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature == Owner && AmountOnTurnStart > 0)
            await HaaLouLingCookingService.AddRandomIngredients(context, player, AmountOnTurnStart);
    }
}

[RegisterPower]
public sealed class ZaoHuoChangMingPower : HaaLouLingPower
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature == Owner && AmountOnTurnStart > 0)
            await HaaLouLingCookingService.CookRandomDishes(context, player, AmountOnTurnStart, rare: false);
    }
}

[RegisterPower]
public sealed class DaChuXingTaiPower : HaaLouLingPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class CaiCongXinShengPower : HaaLouLingPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class YinYangHuoHouPower : HaaLouLingPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        var total = HaaLouLingCookingService.GetRemainingCookingTurnsTotal(player);
        await HaaLouLingCookingService.ChangeCookingTurns(context, player, total % 2 == 1 ? -1 : 1);
    }
}

public abstract class TianZaoBasePower : HaaLouLingPower
{
    protected virtual int Threshold => 10;
    [SavedProperty]
    public int Progress { get; set; }
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            text.Add("Progress", Progress);
            text.Add("Threshold", Threshold);
            return text;
        }
    }
    public async Task IngredientsUsed(PlayerChoiceContext context, int count)
    {
        var total = Progress + count;
        Progress = total % Threshold;
        var gains = total / Threshold * Amount;
        if (gains > 0) await PowerCmd.Apply<GuoWanPiaoPenPower>(context, Owner, gains, Owner, null);
    }
}

[RegisterPower]
public sealed class TianZaoPower : TianZaoBasePower;

[RegisterPower]
public sealed class TianZaoPlusPower : TianZaoBasePower
{
    protected override int Threshold => 8;
}

[RegisterPower]
public sealed class FenNiYiFenPower : HaaLouLingPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    [SavedProperty]
    public string RecipientId { get; set; } = "";
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            var recipient = IsMutable ? Owner.CombatState!.Players.FirstOrDefault(player => player.NetId.ToString() == RecipientId) : null;
            text.Add("Recipient", recipient == null ? "队友" : PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, recipient.NetId));
            return text;
        }
    }
    public Player? Recipient => Owner.CombatState!.Players.FirstOrDefault(player => player.NetId.ToString() == RecipientId);
}
