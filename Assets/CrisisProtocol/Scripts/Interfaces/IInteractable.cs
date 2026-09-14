// ============================================================================
// Crisis Protocol / Sector Containment - Contratti gameplay
// File: .\Assets\CrisisProtocol\Scripts\Interfaces\IInteractable.cs
// Responsabilita': definisce interfacce condivise usate da player, nemici, UI e oggetti interagibili.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
using UnityEngine;

public interface IInteractable
{
    // La "I" di Interact deve essere rigorosamente maiuscola
    void Interact();
}