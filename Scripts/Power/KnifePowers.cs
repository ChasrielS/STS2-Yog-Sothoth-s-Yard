using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class DaoShiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/dao_shi_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_shi_power.png";
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Amount", Amount);
            text.Add("Maximum", IsMutable ? HaaLouLingCombatRules.KnifeLimit(Owner.Player!) : 3);
            return text;
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || amount <= 0) return;
        var cry = Owner.GetPower<DaoMingPower>();
        if (cry == null) return;
        cry.FlashEffect();
        for (var i = 0; i < (int)amount; i++)
            await HaaLouLingCookingService.DamageRandomEnemy(context, Owner.Player!, cry.Amount);
    }
}

[RegisterPower]
public sealed class DaoFaJingJinPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/dao_fa_jing_jin_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_fa_jing_jin_power.png";
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) ClampKnife();
        return Task.CompletedTask;
    }
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.GetPower<LiangShouJueHuoPower>() is { } linked)
            await linked.Synchronize(new ThrowingPlayerChoiceContext());
        ClampKnife();
    }
    private void ClampKnife()
    {
        var knife = Owner.GetPower<DaoShiPower>();
        if (knife != null) knife.SetAmount(Math.Min(knife.Amount, HaaLouLingCombatRules.KnifeLimit(Owner.Player!)));
    }
}

[RegisterPower]
public sealed class DaoMingPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/dao_ming_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_ming_power.png";
}

[RegisterPower]
public sealed class ChenLianPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/chen_lian_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/chen_lian_power.png";
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        Flash();
        await HaaLouLingCombatRules.GainKnife(context, player, AmountOnTurnStart);
    }
}

[RegisterPower]
public sealed class WuXieKeJiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/wu_xie_ke_ji_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/wu_xie_ke_ji_power.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new[] { HaaLouLingKeywords.KeywordId("DaoShu") }.ToHoverTips();
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature == Owner && play.Card is HaaLouLingCard { Form: not KnifeForm.None })
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class ChongZhengJiaShiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/chong_zheng_jia_shi_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/chong_zheng_jia_shi_power.png";
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature == Owner && play.Card is HaaLouLingCard { Form: KnifeForm.Finish })
            await HaaLouLingCombatRules.GainKnife(context, play.Player, Amount, play.Card);
    }
}

[RegisterPower]
public sealed class ChiDaoErLiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/chi_dao_er_li_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/chi_dao_er_li_power.png";
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await CreatureCmd.GainBlock(Owner, Amount * HaaLouLingCombatRules.Knife(Owner.Player!), ValueProp.Unpowered, null);
    }
}

public abstract class ThisTurnHaaLouLingPower : HaaLouLingPower
{
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

[RegisterPower]
public sealed class CangFengPower : ThisTurnHaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/cang_feng_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/cang_feng_power.png";
}

[RegisterPower]
public sealed class GuiYuanJuePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/gui_yuan_jue_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gui_yuan_jue_power.png";
    private int _givenThisTurn;

    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (participants.Contains(Owner)) _givenThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (applier == Owner && power is GuiYuanBasePower && amount > 0) _givenThisTurn++;
        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) =>
        giver == Owner && power is GuiYuanBasePower && amount > 0 && _givenThisTurn <= Amount ? 2 : 1;

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class WuFaHuoDeDaoShiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/wu_fa_huo_de_dao_shi_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/wu_fa_huo_de_dao_shi_power.png";
    public override PowerType Type => PowerType.Debuff;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}

public abstract class TiQianBeiCaiBasePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/ti_qian_bei_cai_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/ti_qian_bei_cai_power.png";
    protected virtual int Ingredients => 3;
    protected virtual int Cards => 2;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        Flash();
        await HaaLouLingCookingService.AddRandomIngredients(context, player, Ingredients);
        await CardPileCmd.Draw(context, Cards, player);
        await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class TiQianBeiCaiPower : TiQianBeiCaiBasePower;

[RegisterPower]
public sealed class TiQianBeiCaiPlusPower : TiQianBeiCaiBasePower
{
    protected override int Ingredients => 4;
    protected override int Cards => 3;
}
