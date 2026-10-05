using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class DaoLuZaiXinPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/dao_lu_zai_xin_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_lu_zai_xin_power.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new[] { HaaLouLingKeywords.KeywordId("DaoShu") }.ToHoverTips();
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || play.Card is not HaaLouLingCard { Form: not KnifeForm.None }) return;
        var count = CombatManager.Instance.History.CardPlaysFinished.Count(entry => entry.HappenedThisTurn(CombatState) &&
            entry.CardPlay.Player == play.Player && entry.CardPlay.Card is HaaLouLingCard { Form: not KnifeForm.None });
        if (count == 1)
        {
            Flash();
            await CardPileCmd.Draw(context, Amount, play.Player);
        }
    }
}

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
    public override async Task AfterRemoved(Creature oldOwner)
    {
        HaaLouLingCookingService.RefreshCapacity(oldOwner.Player!);
        if (oldOwner.GetPower<LiangShouJueHuoPower>() is { } linked)
            await linked.Synchronize(new ThrowingPlayerChoiceContext());
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
public sealed class ZaoHuoChangMingPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/zao_huo_chang_ming_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/zao_huo_chang_ming_power.png";
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature == Owner && AmountOnTurnStart > 0)
            await HaaLouLingCookingService.CookRandomDishes(context, player, AmountOnTurnStart);
    }
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
public sealed class YinYangHuoHouPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/yin_yang_huo_hou_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/yin_yang_huo_hou_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
    public async Task AfterDishEnqueued(PlayerChoiceContext context)
    {
        var player = Owner.Player!;
        var total = HaaLouLingCookingService.GetRemainingCookingTurnsTotal(player);
        Flash();
        await HaaLouLingCookingService.ChangeCookingTurns(context, player, total % 2 == 1 ? -1 : 1);
    }
}

public abstract class TianZaoBasePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/tian_zao_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/tian_zao_power.png";
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
            var recipient = IsMutable ? Owner.CombatState!.Players.FirstOrDefault(player => player.NetId.ToString() == RecipientId) : null;
            text.Add("Recipient", recipient == null ? "队友" : PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, recipient.NetId));
            return text;
        }
    }
    public Player? Recipient => Owner.CombatState!.Players.FirstOrDefault(player => player.NetId.ToString() == RecipientId);
}
