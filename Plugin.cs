using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace SupplyMissionHelper;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Supply Mission Helper";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.Gui.IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.Gui.ITooltipManager Tooltip { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.ClientState.Objects.IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.Gui.Toast.IToastGui Toast { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.ClientState.Conditions.ICondition Condition { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.ClientState.IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static Dalamud.Game.ClientState.Resolvers.ITimerManager TimerManager { get; private set; } = null!; // Namespace may differ; adjust if needed.

    private readonly WindowSystem _windows = new("SupplyMissionHelper");
    private readonly MaterialListWindow _window;

    public Plugin()
    {
        _window = new MaterialListWindow(BuildAggregator, RefreshNow);
        _windows.AddWindow(_window);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleUI;

        // First auto-refresh when plugin loads
        try
        {
            _window.RefreshData();
        }
        catch (Exception ex)
        {
            Chat.Print(new SeString(new TextPayload($"[SupplyMissionHelper] Failed to fetch missions: {ex.Message}")));
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleUI;
        _windows.RemoveAllWindows();
    }

    private void DrawUI()
    {
        _windows.Draw();
    }

    private void ToggleUI()
    {
        _window.IsOpen = !_window.IsOpen;
    }

    private void RefreshNow()
    {
        _window.RefreshData();
    }

    /// <summary>
    /// Reads GC missions and returns an aggregated map of raw materials (itemId -> qty).
    /// Crafted turn-ins are expanded into recipe ingredients; provisioning items are taken as-is.
    /// </summary>
    private Dictionary<uint, long> BuildAggregator()
    {
        var needed = new Dictionary<uint, long>();

        // 1) Pull today's missions from ITimerManager.
        var missions = GCMissionsReader.TryGetGCMissions(TimerManager);
        if (missions.Count == 0)
            return needed;

        // 2) Excel sheets.
        var itemSheet = DataManager.GetExcelSheet<Item>();
        var recipeSheet = DataManager.GetExcelSheet<Recipe>();

        foreach (var m in missions)
        {
            // Resolve item row
            var itemRow = itemSheet?.GetRow(m.ItemId);
            if (itemRow == null) continue;

            if (JobUtils.IsCraftingJob(m.JobId))
            {
                // SUPPLY: expand via recipe ingredients
                var recipes = recipeSheet!
                    .Where(r => r.ItemResult.RowId == itemRow.RowId)
                    .ToList();

                if (recipes.Count == 0)
                {
                    // No recipe found; treat as direct item
                    Add(needed, itemRow.RowId, m.Quantity);
                    continue;
                }

                // Choose first recipe (you could rank by difficulty, etc.)
                var recipe = recipes[0];
                var yields = Math.Max(1, recipe.AmountResult);

                // crafts needed = ceil(req / yields)
                var craftsNeeded = (int)Math.Ceiling((double)m.Quantity / yields);

                for (int i = 0; i < 10; i++)
                {
                    var ingItem = recipe.GetIngredientItemId(i);
                    var ingAmt = recipe.GetIngredientAmount(i);
                    if (ingItem == 0 || ingAmt == 0) continue;

                    Add(needed, ingItem, (long)ingAmt * craftsNeeded);
                }
            }
            else if (JobUtils.IsGatheringJob(m.JobId))
            {
                // PROVISIONING: raw item is itself
                Add(needed, itemRow.RowId, m.Quantity);
            }
            else
            {
                // Unknown classification, just add the item
                Add(needed, itemRow.RowId, m.Quantity);
            }
        }

        return needed;

        static void Add(Dictionary<uint, long> map, uint id, long amt)
        {
            if (map.TryGetValue(id, out var cur)) map[id] = cur + amt;
            else map[id] = amt;
        }
    }
}
