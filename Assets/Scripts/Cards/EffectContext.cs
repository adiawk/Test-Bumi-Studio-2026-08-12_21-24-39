/// <summary>
/// Runtime arguments passed into every CardEffect.
/// Built by combat code; effects stay unaware of Player/Enemy/Deck types.
/// </summary>
public sealed class EffectContext
{
    public IEffectTarget Source { get; }
    public IEffectTarget Target { get; }
    public ICardDrawer Drawer { get; }

    public EffectContext(IEffectTarget source, IEffectTarget target, ICardDrawer drawer)
    {
        Source = source;
        Target = target;
        Drawer = drawer;
    }
}
