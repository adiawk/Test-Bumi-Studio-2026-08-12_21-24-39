using UnityEngine;

/// <summary>
/// Linear run stages for the MVP. No procedural map.
/// </summary>
public enum StageId
{
    Stage1Combat,
    CardReward,
    Stage2Combat,
    BossCombat,
    Victory
}

/// <summary>
/// Tracks the current run stage. Owned by RunManager, not a scene singleton.
/// </summary>
public class StageManager
{
    public StageId CurrentStage { get; private set; }

    public void ResetToStart()
    {
        CurrentStage = StageId.Stage1Combat;
    }

    public void Advance()
    {
        if (CurrentStage == StageId.Victory)
            return;

        CurrentStage = (StageId)((int)CurrentStage + 1);
        Debug.Log($"[StageManager] Advanced to {CurrentStage}.");
    }
}
