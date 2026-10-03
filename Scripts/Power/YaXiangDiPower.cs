using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class YaXiangDiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/ya_xiang_di_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/ya_xiang_di_power.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner) return;
        var prompt = new LocString("powers", Id.Entry + ".selectionScreenPrompt");
        var selected = await CardSelectCmd.FromHand(context, player, new CardSelectorPrefs(prompt, 0, Amount),
            card => !card.Keywords.Contains(CardKeyword.Retain), this);
        foreach (var card in selected) CardCmd.ApplyKeyword(card, CardKeyword.Retain);
    }
}
