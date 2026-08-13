public enum IntentType
{
    Attack,
    Defend
}

/// <summary>
/// Next enemy action shown to the player before it resolves.
/// </summary>
[System.Serializable]
public struct EnemyIntent
{
    public IntentType Type;
    public int Value;

    public string Label
    {
        get
        {
            if (Type == IntentType.Attack)
                return "Attack " + Value;
            return "Defend " + Value;
        }
    }
}
