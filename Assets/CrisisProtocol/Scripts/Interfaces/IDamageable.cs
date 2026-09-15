// ============================================================================
// Crisis Protocol / Sector Containment - Contratti gameplay
// File: .\Assets\CrisisProtocol\Scripts\Interfaces\IDamageable.cs
// Responsabilita': definisce interfacce condivise usate da player, nemici, UI e oggetti interagibili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
// IDamageable.cs
// Contratto minimo condiviso: ogni implementazione decide i dettagli, ma espone questa API.
public interface IDamageable
{
    /// <summary>
    /// Applica una quantità specifica di danno all'entità colpita.
    /// </summary>
    /// <param name="quantitaDanno">Punti vita da sottrarre.</param>
    void SubisciDanno(float quantitaDanno);
}