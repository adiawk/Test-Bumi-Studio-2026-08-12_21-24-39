using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

/// <summary>
/// Static enemy definition. Runtime HP and intent live on Enemy.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Deckbuilder/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] string displayName = "Enemy";
    [SerializeField] int maxHp = 40;
    [SerializeField] int attackDamage = 8;
    
    [SerializeField] List<Sprite> visualLayers = new List<Sprite>();
[SerializeField] int defendBlock = 5;

    public string DisplayName => displayName;
    public int MaxHp => maxHp;
    public int AttackDamage => attackDamage;
    public int DefendBlock => defendBlock;


public IReadOnlyList<Sprite> VisualLayers => visualLayers;
}
