/// <summary>
/// Draw-pile access for DrawEffect. DeckManager implements this later.
/// </summary>
public interface ICardDrawer
{
    void DrawCards(int count);
}
