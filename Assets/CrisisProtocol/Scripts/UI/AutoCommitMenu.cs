// ============================================================================
// Crisis Protocol / Sector Containment - Interfaccia utente
// File: .\Assets\CrisisProtocol\Scripts\UI\AutoCommitMenu.cs
// Responsabilita': aggiorna HUD, menu, overlay, gauge, notifiche o schermate di supporto in base agli eventi gameplay.
// Note di manutenzione: i commenti in questo file chiariscono il ruolo dello
// script nel prototipo Unity; mantenere nomi pubblici e campi serializzati con
// attenzione, perche' scene, prefab e ScriptableObject possono dipendere da essi.
// ============================================================================
#if UNITY_EDITOR // prep ok // riga-ok
using UnityEngine; // usa lib // riga-ok
using UnityEditor; // usa lib // riga-ok
using System.Diagnostics; // usa lib // riga-ok
using System.IO; // Necessario per gestire i percorsi di sistema // usa lib // riga-ok

// blocco: classe x roba grossa
public class AutoCommitMenu // classe qui // riga-ok
{ // apre // riga-ok
    // Aggiunge una voce nel menu in alto su Unity (rinominata per non fare riferimento alla chiusura)
    [MenuItem("GitHub/Salva e Invia (Push)")] // nota unity // riga-ok
    // blocco: funzione fa cose
    public static void SalvaECommit() // roba pub // riga-ok
    { // apre // riga-ok
        // 1. Salva i cambiamenti alle scene e agli asset correnti senza interrompere il lavoro
        EditorApplication.ExecuteMenuItem("File/Save Project"); // chiama // riga-ok
        AssetDatabase.SaveAssets(); // chiama // riga-ok
        UnityEngine.Debug.Log("Progetto Unity salvato con successo."); // chiama // riga-ok

        // 2. Otteniamo il percorso principale del progetto (Root Directory)
        string projectPath = Path.GetDirectoryName(Application.dataPath); // setta // riga-ok
        
        // Formattiamo il percorso usando gli slash corretti per Git
        string gitSafePath = projectPath.Replace("\\", "/"); // setta // riga-ok

        // 3. Messaggio di commit aggiornato (indica un salvataggio/backup in corso d'opera)
        string commitMessage = $"Backup automatico del {System.DateTime.Now:dd/MM/yyyy HH:mm}"; // setta // riga-ok
        
        // Concateniamo la configurazione di sicurezza prima del commit
#if UNITY_EDITOR_WIN // prep ok // riga-ok
        string command = $"/c git config --global --add safe.directory \"{gitSafePath}\" && git add . && git commit -m \"{commitMessage}\" && git push"; // setta // riga-ok
        EseguiComando("cmd.exe", command, projectPath); // chiama // riga-ok
#else // prep ok // riga-ok
        string command = $"-c \"git config --global --add safe.directory '{gitSafePath}' && git add . && git commit -m '{commitMessage}' && git push\""; // setta // riga-ok
        EseguiComando("/bin/bash", command, projectPath); // chiama // riga-ok
#endif // prep ok // riga-ok
    } // chiude // riga-ok

    // blocco: funzione fa cose
    private static void EseguiComando(string filename, string arguments, string workingDirectory) // roba pub // riga-ok
    { // apre // riga-ok
        ProcessStartInfo startInfo = new ProcessStartInfo // setta // riga-ok
        { // apre // riga-ok
            FileName = filename, // setta // riga-ok
            Arguments = arguments, // setta // riga-ok
            WorkingDirectory = workingDirectory, // setta // riga-ok
            RedirectStandardOutput = true, // setta // riga-ok
            RedirectStandardError = true, // setta // riga-ok
            UseShellExecute = false, // setta // riga-ok
            CreateNoWindow = true // setta // riga-ok
        }; // ok qua // riga-ok

        using (Process process = Process.Start(startInfo)) // usa lib // riga-ok
        { // apre // riga-ok
            string output = process.StandardOutput.ReadToEnd(); // setta // riga-ok
            string error = process.StandardError.ReadToEnd(); // setta // riga-ok
            process.WaitForExit(); // chiama // riga-ok

            // blocco: controlla se va
            if (!string.IsNullOrEmpty(output)) UnityEngine.Debug.Log($"Git Output: {output}"); // se ok // riga-ok
            
            // Nota: Git spesso stampa informazioni sul flusso 'Error' anche se non ci sono problemi critici.
            // blocco: controlla se va
            if (!string.IsNullOrEmpty(error)) UnityEngine.Debug.LogWarning($"Git Nota/Errore: {error}"); // se ok // riga-ok
        } // chiude // riga-ok
    } // chiude // riga-ok
} // chiude // riga-ok
#endif // prep ok // riga-ok
