using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace SupplyMissionHelper;

public sealed class MaterialListWindow : Window
{
    private readonly Func<Dictionary<uint, long>> _aggregator;
    private readonly Action _refresh;
    private Dictionary<uint, long> _materials = new();
    private string _filter = string.Empty;

    public MaterialListWindow(Func<Dictionary<uint, long>> aggregator, Action refresh)
        : base("Supply Mission Materials")
    {
        _aggregator = aggregator;
        _refresh = refresh;
        RespectCloseHotkey = true;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(420, 250),
            MaximumSize = new System.Numerics.Vector2(4000, 4000)
        };
        IsOpen = true;
    }

    public void RefreshData()
    {
        _materials = _aggregator.Invoke();
    }

    public override void Draw()
    {
        if (ImGui.Button("Refresh")) _refresh();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(220f);
        ImGui.InputTextWithHint("##filter", "Filter by name...", ref _filter, 128);

        ImGui.Separator();

        if (_materials.Count == 0)
        {
            ImGui.TextWrapped("No missions found (or Timers data not loaded yet). Open Timers once, then click Refresh.");
            return;
        }

        var itemSheet = Plugin.DataManager.GetExcelSheet<Item>()!;
        var rows = _materials
            .Select(kv => (item: itemSheet.GetRow(kv.Key), qty: kv.Value))
            .Where(x => x.item != null)
            .Select(x => (Id: x.item!.RowId, Name: x.item!.Name.ToString(), Qty: x.qty))
            .OrderBy(x => x.Name);

        if (!string.IsNullOrWhiteSpace(_filter))
        {
            var f = _filter.Trim().ToLowerInvariant();
            rows = rows.Where(r => r.Name.ToLowerInvariant().Contains(f));
        }

        if (ImGui.BeginTable("mat_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Item");
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 90);
            ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            foreach (var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(row.Name);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(row.Id.ToString());

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(row.Qty.ToString());
            }

            ImGui.EndTable();
        }
    }
}
