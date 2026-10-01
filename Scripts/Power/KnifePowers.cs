using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class DaoShiPower : HaaLouLingPower
{
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
        if (Owner.GetPower<DaoShiGaugePower>() is { } gauge) await PowerCmd.Remove(gauge);
        var cry = Owner.GetPower<DaoMingPower>();
        if (cry == null) return;
        cry.FlashEffect();
        for (var i = 0; i < (int)amount; i++)
            await HaaLouLingCookingService.DamageRandomEnemy(context, Owner.Player!, cry.Amount);
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        if (CombatManager.Instance.IsInProgress && oldOwner.IsAlive && !oldOwner.HasPower<DaoShiGaugePower>())
            ModelDb.Power<DaoShiGaugePower>().ToMutable().ApplyInternal(oldOwner, 1, silent: true);
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class DaoShiGaugePower : HaaLouLingPower
{
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override LocString Title => new("powers", "YOG_SOTHOTH_S_YARD_POWER_DAO_SHI_POWER.title");
    public override LocString Description
    {
        get
        {
            var text = new LocString("powers", "YOG_SOTHOTH_S_YARD_POWER_DAO_SHI_POWER.description");
            text.Add("Amount", 0);
            text.Add("Maximum", IsMutable ? HaaLouLingCombatRules.KnifeLimit(Owner.Player!) : 3);
            return text;
        }
    }
}

[RegisterPower]
public sealed class DaoFaJingJinPower : HaaLouLingPower
{
    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) ClampKnife();
        return Task.CompletedTask;
    }
    public override Task AfterRemoved(Creature oldOwner)
    {
        ClampKnife();
        return Task.CompletedTask;
    }
    private void ClampKnife()
    {
        var knife = Owner.GetPower<DaoShiPower>();
        if (knife != null) knife.SetAmount(Math.Min(knife.Amount, HaaLouLingCombatRules.KnifeLimit(Owner.Player!)));
    }
}

[RegisterPower]
public sealed class DaoMingPower : HaaLouLingPower;

[RegisterPower]
public sealed class ZhuanShiPower : HaaLouLingPower
{
    private int _triggeredOnTurn;
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || play.Card is not HaaLouLingCard { Form: KnifeForm.Turn } ||
            _triggeredOnTurn == play.Player.PlayerCombatState!.TurnNumber) return;
        _triggeredOnTurn = play.Player.PlayerCombatState.TurnNumber;
        await HaaLouLingCombatRules.GainKnife(context, play.Player, Amount, play.Card);
    }
}

[RegisterPower]
public sealed class ShouDaoPower : HaaLouLingPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature == Owner && play.Card is HaaLouLingCard { Form: KnifeForm.Finish })
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class ChongZhengJiaShiPower : HaaLouLingPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature == Owner && play.Card is HaaLouLingCard { Form: KnifeForm.Finish })
            await HaaLouLingCombatRules.GainKnife(context, play.Player, Amount, play.Card);
    }
}

[RegisterPower]
public sealed class XuShiHuShenPower : HaaLouLingPower
{
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
public sealed class CangFengPower : ThisTurnHaaLouLingPower;

[RegisterPower]
public sealed class GuiYuanJuePower : HaaLouLingPower
{
    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) =>
        giver == Owner && target?.Side == CombatSide.Enemy && power is GuiYuanBasePower && amount > 0 ? Amount : 0;
}

[RegisterPower]
public sealed class ZhouFuDaoFengPower : HaaLouLingPower
{
    public override PowerStackType StackType => PowerStackType.Single;
    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && target.Side == CombatSide.Enemy && cardSource?.Type == CardType.Attack && result.TotalDamage > 0)
            await HaaLouLingCombatRules.GiveGuiYuan(context, target, HaaLouLingCombatRules.KnifeForCard(cardSource), Owner, cardSource);
    }
}

[RegisterPower]
public sealed class DaoShiJinDePower : HaaLouLingPower
{
    public override PowerType Type => PowerType.Debuff;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}

public abstract class ZaoHuoYangFengBasePower : HaaLouLingPower
{
    protected virtual int ExtraKnife => 0;
    public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        await HaaLouLingCombatRules.GainKnife(context, player,
            HaaLouLingCookingService.GetState(player).CompletedAtTurnStart + ExtraKnife);
        await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class ZaoHuoYangFengPower : ZaoHuoYangFengBasePower;

[RegisterPower]
public sealed class ZaoHuoYangFengPlusPower : ZaoHuoYangFengBasePower
{
    protected override int ExtraKnife => 1;
}
