using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;
using System.Text.RegularExpressions;

namespace YogSothothsYard.Scripts;

public enum KnifeForm { None, Open, Follow, Turn, Finish }

public abstract class HaaLouLingCard : ModCardTemplate
{
    public virtual KnifeForm Form => KnifeForm.None;
    public virtual bool WantsGreenOutline => false;
    protected int Knife => Owner?.PlayerCombatState == null ? 0 : HaaLouLingCombatRules.Knife(Owner);
    protected virtual bool UsesKnife => false;
    protected virtual bool UsesGuiYuan => false;
    protected virtual bool UsesKnifeForms => false;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://images/packed/card_portraits/haa_lou_ling/{Regex.Replace(GetType().Name, "(?<=[a-z])(?=[A-Z])", "_").ToLowerInvariant()}.png");
    protected HaaLouLingCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target == TargetType.AnyAlly ? Entry.OtherPlayerTarget : target, true) { }
    protected override bool IsPlayable => !IsMutable || Owner?.PlayerCombatState == null || TargetType != Entry.OtherPlayerTarget ||
        CombatState!.Players.Any(player => player != Owner && player.Creature.IsAlive);
    protected IEnumerable<CardKeyword> FormKeywords => Form switch
    {
        KnifeForm.Open => [HaaLouLingKeywords.QiShi],
        KnifeForm.Follow => [HaaLouLingKeywords.ChengShi],
        KnifeForm.Turn => [HaaLouLingKeywords.ZhuanShi],
        KnifeForm.Finish => [HaaLouLingKeywords.HeShi],
        _ => []
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => DisplayKeywordIds.ToHoverTips();
    private IEnumerable<string> DisplayKeywordIds
    {
        get
        {
            if (UsesKnife) yield return HaaLouLingKeywords.KeywordId("DaoShi");
            if (UsesGuiYuan) yield return HaaLouLingKeywords.KeywordId("GuiYuan");
            if (UsesKnifeForms) yield return HaaLouLingKeywords.KeywordId("DaoShu");
        }
    }
    protected async Task<AttackCommand> Attack(PlayerChoiceContext context, CardPlay play, int hits = 1, bool random = false, bool calculated = false)
    {
        var command = calculated ? DamageCmd.Attack(DynamicVars.CalculatedDamage) : DamageCmd.Attack(DynamicVars.Damage.BaseValue);
        command.FromCard(this, play).WithHitCount(hits).WithHitVfxNode(HaaLouLingStrikeSlashVfx.Create);
        if (random) command.TargetingRandomOpponents(CombatState!, true);
        else if (TargetType == TargetType.AllEnemies) command.TargetingAllOpponents(CombatState!);
        else command.Targeting(play.Target!);
        return await command.Execute(context);
    }
    protected Task Block(CardPlay play) => CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    protected Task GainKnife(PlayerChoiceContext context, int amount) => HaaLouLingCombatRules.GainKnife(context, Owner, amount, this);
    protected Task<int> SpendKnife(PlayerChoiceContext context, int amount) => HaaLouLingCombatRules.SpendKnife(context, Owner, amount, this);
    protected Task<int> FinishKnife(PlayerChoiceContext context) => HaaLouLingCombatRules.FinishKnife(context, Owner, this);
    protected Task GiveGuiYuan(PlayerChoiceContext context, CardPlay play, int amount) =>
        HaaLouLingCombatRules.GiveGuiYuan(context, play.Target!, amount, Owner.Creature, this);
    protected Task Apply<T>(PlayerChoiceContext context, int amount) where T : PowerModel =>
        PowerCmd.Apply<T>(context, Owner.Creature, amount, Owner.Creature, this);
}
