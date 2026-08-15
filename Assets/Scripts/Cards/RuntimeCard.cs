/// <summary>
/// One card instance in a run. Points at static CardData; bonuses persist on this copy.
/// </summary>
public class RuntimeCard
{
    public const int UpgradeStep = 2;

    public CardData Data { get; }
    public int DamageBonus { get; private set; }
    public int BlockBonus { get; private set; }

    public string Name => Data != null ? Data.CardName : string.Empty;
    public int Cost => Data != null ? Data.Cost : 0;

    public string Description
    {
        get
        {
            string text = Data != null ? Data.Description : string.Empty;
            if (DamageBonus > 0)
                text += " (+" + DamageBonus + " damage)";
            if (BlockBonus > 0)
                text += " (+" + BlockBonus + " block)";
            return text;
        }
    }

    public bool CanUpgrade => HasDamageEffect() || HasBlockEffect();

    public RuntimeCard(CardData data)
    {
        Data = data;
    }

    public void Upgrade()
    {
        if (HasDamageEffect())
            DamageBonus += UpgradeStep;
        if (HasBlockEffect())
            BlockBonus += UpgradeStep;
    }

    public void Resolve(EffectContext context)
    {
        if (Data != null)
            Data.Resolve(context);
    }

    bool HasDamageEffect()
    {
        return HasEffect<DamageEffect>();
    }

    bool HasBlockEffect()
    {
        return HasEffect<BlockEffect>();
    }

    bool HasEffect<T>() where T : CardEffect
    {
        if (Data == null || Data.Effects == null)
            return false;

        for (int i = 0; i < Data.Effects.Count; i++)
        {
            if (Data.Effects[i] is T)
                return true;
        }

        return false;
    }
}
