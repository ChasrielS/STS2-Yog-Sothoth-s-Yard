using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace YogSothothsYard.Scripts.Cooking;

public sealed class CookingMenuData
{
    public int SelectedFilter { get; set; }
    public List<string> History { get; set; } = [];
    public List<CookingPreset> Presets { get; set; } =
        Enumerable.Range(0, 5).Select(_ => new CookingPreset()).ToList();
}

public sealed class CookingPreset
{
    public string Name { get; set; } = "";
    public List<string> Dishes { get; set; } = [];
}

public static class CookingMenuPreferences
{
    private const string Key = "cooking_menu";
    private static ModDataStore Store => RitsuLibFramework.GetDataStore(Entry.ModId);
    public static CookingMenuData Data => Store.Get<CookingMenuData>(Key);

    public static void Register()
    {
        using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
            Store.Register<CookingMenuData>(Key, "cooking_menu.json", SaveScope.Profile,
                defaultFactory: () => new CookingMenuData(), autoCreateIfMissing: true);
    }

    public static void Update(Action<CookingMenuData> update)
    {
        Store.Modify(Key, update);
        Store.Save(Key);
    }

    public static void RecordCooking(Player player, ModelId dishId)
    {
        if (!LocalContext.IsMe(player)) return;
        Update(data =>
        {
            data.History.Remove(dishId.Entry);
            data.History.Insert(0, dishId.Entry);
        });
    }
}
