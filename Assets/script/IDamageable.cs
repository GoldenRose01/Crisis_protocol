// IDamageable.cs
public interface IDamageable
{
    /// <summary>
    /// Applica una quantità specifica di danno all'entità colpita.
    /// </summary>
    /// <param name="quantitaDanno">Punti vita da sottrarre.</param>
    void SubisciDanno(float quantitaDanno);
}