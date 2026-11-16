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
public sealed class GuoWanPiaoPenPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/guo_wan_piao_pen_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/guo_wan_piao_pen_power.png";
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
    public override string? CustomIconPath => "res://images/powers/cai_lan_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/cai_lan_power.png";
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature == Owner && AmountOnTurnStart > 0)
            await HaaLouLingCookingService.AddRandomIngredients(context, player, AmountOnTurnStart, nonRare: true);
    }
}

[RegisterPower]
public sealed class ChaLouBuQuePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/cha_lou_bu_que_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/cha_lou_bu_que_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class DaChuXingTaiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/da_chu_xing_tai_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/da_chu_xing_tai_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class CaiCongXinShengPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/cai_cong_xin_sheng_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/cai_cong_xin_sheng_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public class ShunShouTianHuoPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/shun_shou_tian_huo_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/shun_shou_tian_huo_power.png";
    protected virtual int Turns => 1;
    public override LocString Description
    {
        get
        {
            var text = new LocString("powers", ModelDb.GetId<ShunShouTianHuoPower>().Entry + ".description");
            text.Add("Amount", Amount);
            text.Add("Turns", Turns);
            return text;
        }
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || play.Card.Type != CardType.Skill) return;
        var first = CombatManager.Instance.History.CardPlaysStarted.FirstOrDefault(entry => entry.HappenedThisTurn(CombatState) &&
            entry.CardPlay.Player == play.Player && entry.CardPlay.Card.Type == CardType.Skill);
        if (first?.CardPlay != play) return;
        Flash();
        await HaaLouLingCookingService.ChangeCookingTurns(context, play.Player, -Turns, left: true, count: Amount);
    }
}

[RegisterPower]
public sealed class ShunShouTianHuoTwoTurnsPower : ShunShouTianHuoPower
{
    protected override int Turns => 2;
}

[RegisterPower]
public sealed class ShunShouTianHuoThreeTurnsPower : ShunShouTianHuoPower
{
    protected override int Turns => 3;
}

public abstract class TianZaoBasePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/tian_zao_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/tian_zao_power.png";
    internal virtual bool BeforeCooking => false;

    internal async Task ExpandQueue(PlayerChoiceContext context)
    {
        Flash();
        await PowerCmd.Apply<GuoWanPiaoPenPower>(context, Owner, AmountOnTurnStart, Owner, null);
    }
}

[RegisterPower]
public sealed class TianZaoPower : TianZaoBasePower;

[RegisterPower]
public sealed class TianZaoPlusPower : TianZaoBasePower
{
    internal override bool BeforeCooking => true;
}

[RegisterPower]
public sealed class TianZaoDishPower : TianZaoBasePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    internal override bool BeforeCooking => true;
    internal CardRarity? Rarity { get; set; }
    internal bool UpgradeDish { get; set; }
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Uncommon", Rarity == CardRarity.Uncommon);
            text.Add("UpgradeDish", UpgradeDish);
            return text;
        }
    }

    internal async Task CookAddedDishes(PlayerChoiceContext context, int count)
    {
        for (var i = 0; i < count; i++)
            await HaaLouLingCookingService.StartRandomDish(context, Owner.Player!, Rarity, UpgradeDish);
    }
}

[RegisterPower]
public sealed class FenNiYiFenPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/fen_ni_yi_fen_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/fen_ni_yi_fen_power.png";
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    [SavedProperty]
    public string RecipientId { get; set; } = "";
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            var all = IsMutable && RecipientId.Length == 0;
            var recipient = IsMutable && !all ? Recipients.FirstOrDefault() : null;
            text.Add("AllPlayers", all);
            text.Add("Recipient", recipient == null ? "队友" : PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, recipient.NetId));
            return text;
        }
    }
    internal IEnumerable<Player> Recipients => Owner.CombatState!.Players.Where(player => player.Creature != Owner &&
        (RecipientId.Length == 0 || player.NetId.ToString() == RecipientId));
}
