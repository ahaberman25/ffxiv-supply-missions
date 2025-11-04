using System;
using System.Collections.Generic;
using System.Linq;

namespace SupplyMissionHelper;

/// <summary>
/// Thin adapter over ITimerManager to isolate API-shape differences.
/// Adjust property names here if your ITimerManager differs.
/// </summary>
public static class GCMissionsReader
{
    public record GCMission(uint ItemId, uint JobId, int Quantity, bool IsHq);

    public static List<GCMission> TryGetGCMissions(object timerManager)
    {
        var list = new List<GCMission>();
        if (timerManager is null) return list;

        // Typical API 13 pattern: timerManager.GrandCompanySupplyMissions : IEnumerable<...>
        // We’ll use reflection so this file compiles even if local symbol names differ slightly.
        var type = timerManager.GetType();
        var prop = type.GetProperty("GrandCompanySupplyMissions")
                   ?? type.GetProperty("DailySupplyMissions")
                   ?? type.GetProperty("GCMissions")
                   ?? null;

        if (prop == null)
            return list;

        var enumerable = prop.GetValue(timerManager) as System.Collections.IEnumerable;
        if (enumerable == null)
            return list;

        foreach (var entry in enumerable)
        {
            try
            {
                var et = entry.GetType();

                // Expected fields (adjust if your build differs)
                uint itemId = (uint)(et.GetProperty("ItemId")?.GetValue(entry) ?? 0u);
                uint jobId  = (uint)(et.GetProperty("JobId")?.GetValue(entry) ?? 0u);
                int qty     = Convert.ToInt32(et.GetProperty("Quantity")?.GetValue(entry) ?? 0);
                bool isHq   = Convert.ToBoolean(et.GetProperty("IsHQ")?.GetValue(entry) ?? false);

                if (itemId != 0 && qty > 0)
                    list.Add(new GCMission(itemId, jobId, qty, isHq));
            }
            catch
            {
                // ignore this row if shape mismatch
            }
        }

        return list;
    }
}
