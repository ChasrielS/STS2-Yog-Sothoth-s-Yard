using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

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

}

[RegisterPower]
public sealed class DaoFaJingJinPower : HaaLouLingPower
{
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override string? CustomIconPath => "res://images/powers/dao_fa_jing_jin_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_fa_jing_jin_power.png";
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
public sealed class ZhouZhangPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/zhou_zhang_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/zhou_zhang_power.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator?.Creature != Owner || card.Type != CardType.Curse) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class DaoGongPower : HaaLouLingPower
{
    internal int RareBonus { get; set; }
    internal int UncommonBonus { get; set; }
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override string? CustomIconPath => "res://images/powers/dao_gong_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/dao_gong_power.png";

    internal int GainFor(CardRarity rarity) => Amount + Math.Min(Amount, rarity switch
    {
        CardRarity.Rare => RareBonus,
        CardRarity.Uncommon => UncommonBonus,
        _ => 0
    });

    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Rare", GainFor(CardRarity.Rare));
            text.Add("Uncommon", GainFor(CardRarity.Uncommon));
            text.Add("RaritiesDiffer", RareBonus > 0 || UncommonBonus > 0);
            return text;
        }
    }
}

[RegisterPower]
public sealed class WenXiDaoFaPower : HaaLouLingPower
{
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override string? CustomIconPath => "res://images/powers/wen_xi_dao_fa_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/wen_xi_dao_fa_power.png";
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        Flash();
        await HaaLouLingCombatRules.GainKnife(context, player, AmountOnTurnStart);
    }
}

[RegisterPower]
public sealed class GuiYuanJuePower : HaaLouLingPower
{
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("GuiYuan")];
    public override string? CustomIconPath => "res://images/powers/gui_yuan_jue_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gui_yuan_jue_power.png";
    private int _givenThisTurn;
    private int _givingGroupDepth;

    internal void BeginGivingGroup()
    {
        if (_givingGroupDepth++ == 0) _givenThisTurn++;
    }

    internal void EndGivingGroup() => _givingGroupDepth--;

    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side, IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (participants.Contains(Owner)) _givenThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (_givingGroupDepth == 0 && applier == Owner && power is GuiYuanBasePower && amount > 0) _givenThisTurn++;
        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) =>
        giver == Owner && power is GuiYuanBasePower && amount > 0 && _givenThisTurn == 1 ? Amount : 1;

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class WuFaHuoDeDaoShiPower : HaaLouLingPower
{
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override string? CustomIconPath => "res://images/powers/wu_fa_huo_de_dao_shi_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/wu_fa_huo_de_dao_shi_power.png";
    public override PowerType Type => PowerType.Debuff;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}

public abstract class KnifeFormPower : HaaLouLingPower
{
    private string IconName => StringHelper.SnakeCase(GetType().Name);
    public override string? CustomIconPath => $"res://images/powers/{IconName}_icon.png";
    public override string? CustomBigIconPath => $"res://images/powers/{IconName}.png";
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];

    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants) =>
        side == CombatSide.Player && participants.Contains(Owner) ? PowerCmd.Remove(this) : Task.CompletedTask;
}

[RegisterPower]
public sealed class QiShiPower : KnifeFormPower;

[RegisterPower]
public sealed class ChengShiPower : KnifeFormPower;

[RegisterPower]
public sealed class ZhuanShiPower : KnifeFormPower;

[RegisterPower]
public sealed class HeShiPower : KnifeFormPower
{
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card.Owner.Creature == Owner && card is HaaLouLingCard { SpendsAllKnife: true } ? playCount + Amount : playCount;

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        FlashEffect();
        return PowerCmd.Remove(this);
    }
}
