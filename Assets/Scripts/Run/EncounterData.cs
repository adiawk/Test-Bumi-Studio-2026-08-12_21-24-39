using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One combat encounter: which enemies spawn, in order.
/// Assign these per run stage instead of hardcoding fights on CombatManager.
/// </summary>
[CreateAssetMenu(fileName = "EncounterData", menuName = "Deckbuilder/Encounters/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [SerializeField] string displayName = "Encounter";
    [SerializeField] List<EnemyData> enemies = new List<EnemyData>();

    public string DisplayName => displayName;
    public IReadOnlyList<EnemyData> Enemies => enemies;
}
