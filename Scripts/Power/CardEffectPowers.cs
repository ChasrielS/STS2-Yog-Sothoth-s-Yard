using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class LiuYiShouPower : HaaLouLingPower
{
    private bool _checkingUndiscountedCost;
    protected override bool IncludeEnergyHoverTip => true;
    public override string? CustomIconPath => "res://images/powers/liu_yi_shou_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/liu_yi_shou_power.png";

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal original, out decimal modified)
    {
        modified = !_checkingUndiscountedCost && original >= 1 && AppliesTo(card)
            ? Math.Max(0, original - Amount) : original;
        return modified != original;
    }

    public override Task AfterEnergySpent(CardModel card, int amount) =>
        AppliesTo(card) && GetCostBeforeDiscount(card) >= 1 ? PowerCmd.Remove(this) : Task.CompletedTask;

    private int GetCostBeforeDiscount(CardModel card)
    {
        _checkingUndiscountedCost = true;
        try { return card.EnergyCost.GetWithModifiers(CostModifiers.All); }
        finally { _checkingUndiscountedCost = false; }
    }

    private bool AppliesTo(CardModel card) =>
        card.Owner.Creature == Owner && card.Type == CardType.Power && !card.EnergyCost.CostsX &&
        card.Pile?.Type is PileType.Hand or PileType.Play;
}

[RegisterPower]
public sealed class YiZhouWeiYinPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/yi_zhou_wei_yin_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/yi_zhou_wei_yin_power.png";
    private bool _triggeredThisTurn;
    internal bool DiscardCurse { get; set; }
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("DiscardCurse", DiscardCurse);
            return text;
        }
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (participants.Contains(Owner)) _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (_triggeredThisTurn || card.Owner.Creature != Owner || card.Type != CardType.Curse) return;
        _triggeredThisTurn = true;
        Flash();
        if (DiscardCurse && card.Pile?.Type == PileType.Hand)
            await CardCmd.Discard(context, card);
        await CardPileCmd.Draw(context, Amount, Owner.Player!);
    }
}

public abstract class ZhuanHuoWeiFuBasePower : HaaLouLingPower
{
    internal readonly record struct Rewards(int Dishes, CardRarity? Rarity, bool Upgraded,
        int KnifeLimit, int KnifeGain, int Energy, int Draw);

    protected override bool IncludeEnergyHoverTip => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        new[] { HaaLouLingKeywords.KeywordId("DaoShi") }.ToHoverTips().Append(HaaLouLingKeywords.ImmediateCookingTip);
    public override string? CustomIconPath => "res://images/powers/zhuan_huo_wei_fu_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/zhuan_huo_wei_fu_power.png";
    internal abstract Rewards Reward { get; }

    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Curse) return;
        var reward = Reward;
        var player = Owner.Player!;
        Flash();
        switch (player.RunState.Rng.CombatCardGeneration.NextInt(3))
        {
            case 0:
                await HaaLouLingCookingService.CompleteRandomDishes(context, player, Amount * reward.Dishes,
                    reward.Rarity, reward.Upgraded);
                break;
            case 1:
                if (reward.KnifeLimit > 0) await PowerCmd.Apply<DaoFaJingJinPower>(context, Owner, Amount * reward.KnifeLimit, Owner, null);
                await HaaLouLingCombatRules.GainKnife(context, player, Amount * reward.KnifeGain);
                break;
            case 2:
                await PlayerCmd.GainEnergy(Amount * reward.Energy, player);
                if (reward.Draw > 0) await CardPileCmd.Draw(context, Amount * reward.Draw, player);
                break;
        }
    }
}

[RegisterPower]
public sealed class ZhuanHuoWeiFuPower : ZhuanHuoWeiFuBasePower
{
    internal override Rewards Reward => new(1, null, false, 0, 1, 1, 0);
}

[RegisterPower]
public sealed class ZhuanHuoWeiFuPlusPower : ZhuanHuoWeiFuBasePower
{
    internal override Rewards Reward => new(1, CardRarity.Uncommon, false, 1, 1, 1, 1);
}

[RegisterPower]
public sealed class ZhuanHuoWeiFuErodedPower : ZhuanHuoWeiFuBasePower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    internal Rewards Settings { get; set; } = new(1, CardRarity.Uncommon, false, 1, 1, 1, 1);
    internal override Rewards Reward => Settings;
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("DishCount", Amount * Reward.Dishes);
            text.Add("UpgradeDish", Reward.Upgraded);
            text.Add("KnifeLimit", Amount * Reward.KnifeLimit);
            text.Add("KnifeGain", Amount * Reward.KnifeGain);
            text.Add("EnergyGain", Amount * Reward.Energy);
            text.Add("Draw", Amount * Reward.Draw);
            return text;
        }
    }
}

[RegisterPower]
public sealed class TuPoPingJingPower : HaaLouLingPower
{
    protected override IEnumerable<string> RegisteredKeywordIds => [HaaLouLingKeywords.KeywordId("DaoShi")];
    public override string? CustomIconPath => "res://images/powers/tu_po_ping_jing_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/tu_po_ping_jing_power.png";
}

[RegisterPower]
public sealed class ShuNengShengQiaoPower : HaaLouLingPower
{
    protected override bool IncludeEnergyHoverTip => true;
    public override string? CustomIconPath => "res://images/powers/shu_neng_sheng_qiao_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/shu_neng_sheng_qiao_power.png";

    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return Task.CompletedTask;
        var candidates = PileType.Hand.GetPile(player).Cards.Where(card => card.Type == CardType.Skill).ToList();
        for (var i = 0; i < AmountOnTurnStart && candidates.Count > 0; i++)
        {
            var card = player.RunState.Rng.CombatCardGeneration.NextItem(candidates)!;
            candidates.Remove(card);
            card.EnergyCost.SetThisTurn(0);
        }
        Flash();
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class SheZhangPower : HaaLouLingPower
{
    protected override bool IncludeEnergyHoverTip => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CanZhou>()];
    public override string? CustomIconPath => "res://images/powers/she_zhang_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/she_zhang_power.png";
    public override PowerStackType StackType => PowerStackType.Single;
    internal bool DiscardPile { get; set; }
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("DiscardPile", DiscardPile);
            text.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
            return text;
        }
    }

    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner.Creature != Owner || card.EnergyCost.CostsX) return;
        var missing = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All) - amount);
        if (missing <= 0) return;
        Flash();
        await HaaLouLingCardChoices.AddCurses(Owner.Player!, missing, DiscardPile ? PileType.Discard : PileType.Draw);
    }
}

[RegisterPower]
public sealed class MianFeiPengRenPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/mian_fei_peng_ren_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/mian_fei_peng_ren_power.png";

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? source)
    {
        if (power == this) HaaLouLingCookingService.GetState(Owner.Player!).RefreshRecipePreviews();
        return Task.CompletedTask;
    }
}

public abstract class XiaHuiHeShiCaiBasePower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/xia_hui_he_shi_cai_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/xia_hui_he_shi_cai_power.png";
    protected abstract CardRarity? IngredientRarity { get; }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        var amount = AmountOnTurnStart;
        Flash();
        await HaaLouLingCookingService.AddRandomIngredients(context, player, amount, IngredientRarity);
        await PowerCmd.ModifyAmount(context, this, -amount, Owner, null);
    }
}

[RegisterPower]
public sealed class XiaHuiHeShiCaiPower : XiaHuiHeShiCaiBasePower
{
    protected override CardRarity? IngredientRarity => CardRarity.Common;
}

[RegisterPower]
public sealed class XiaHuiHeShiCaiAnyPower : XiaHuiHeShiCaiBasePower
{
    protected override CardRarity? IngredientRarity => null;
}
