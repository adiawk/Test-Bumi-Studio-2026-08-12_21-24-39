/// <summary>
/// One card instance in a run. Points at static CardData; runtime-only state can be added later.
/// </summary>
public class RuntimeCard
{
    public CardData Data { get; }

    public string Name => Data != null ? Data.CardName : string.Empty;
    public int Cost => Data != null ? Data.Cost : 0;

    public RuntimeCard(CardData data)
    {
        Data = data;
    }

    public void Resolve(EffectContext context)
    {
        if (Data != null)
            Data.Resolve(context);
    }
}
