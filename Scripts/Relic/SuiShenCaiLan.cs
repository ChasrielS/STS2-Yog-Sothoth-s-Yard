using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class SuiShenCaiLan : HaaLouLingRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override async Task BeforeCombatStart()
    {
        Flash();
        await HaaLouLingCookingService.AddRandomIngredients(new ThrowingPlayerChoiceContext(), Owner, 3, nonRare: true);
    }
}
