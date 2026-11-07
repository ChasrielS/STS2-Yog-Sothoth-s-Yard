using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Helpers;

namespace YogSothothsYard.Scripts;

public abstract class HaaLouLingCard : ModCardTemplate
{
    public virtual bool WantsConditionalHighlight => false;
    protected override bool ShouldGlowGoldInternal => WantsConditionalHighlight;
    protected int Knife => Owner?.PlayerCombatState == null ? 0 : HaaLouLingCombatRules.Knife(Owner);
    protected virtual bool UsesKnife => false;
    public bool HasKnifeEffect => UsesKnife;
    protected virtual bool UsesGuiYuan => false;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://images/packed/card_portraits/haa_lou_ling/{StringHelper.SnakeCase(GetType().Name)}.png");
    protected HaaLouLingCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target == TargetType.AnyAlly ? Entry.OtherPlayerTarget : target, true) { }
    protected override bool IsPlayable => !IsMutable || TargetType != Entry.OtherPlayerTarget ||
        Owner?.Creature.CombatState is not { } state || state.Players.Any(player => player != Owner && player.Creature.IsAlive);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => DisplayKeywordIds.ToHoverTips();
    private IEnumerable<string> DisplayKeywordIds
    {
        get
        {
            if (UsesKnife) yield return HaaLouLingKeywords.KeywordId("DaoShi");
            if (UsesGuiYuan) yield return HaaLouLingKeywords.KeywordId("GuiYuan");
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
    protected Task<int> SpendAllKnife(PlayerChoiceContext context) => HaaLouLingCombatRules.SpendAllKnife(context, Owner, this);
    protected Task GiveGuiYuan(PlayerChoiceContext context, CardPlay play, int amount) =>
        HaaLouLingCombatRules.GiveGuiYuan(context, play.Target!, amount, Owner.Creature, this);
    protected Task Apply<T>(PlayerChoiceContext context, int amount) where T : PowerModel =>
        PowerCmd.Apply<T>(context, Owner.Creature, amount, Owner.Creature, this);
}
