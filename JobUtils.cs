namespace SupplyMissionHelper;

public static class JobUtils
{
    // These are the classic DoH/DoL ranges; adjust if your enum differs.
    // Example job IDs: CRP=8,... CUL=15, MIN=16, BTN=17, FSH=18
    public static bool IsCraftingJob(uint jobId)
        => jobId is >= 8 and <= 15;

    public static bool IsGatheringJob(uint jobId)
        => jobId is >= 16 and <= 18;
}
