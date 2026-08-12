using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO; // Necessario per gestire i percorsi di sistema

public class AutoCommitMenu
{
    // Aggiunge una voce nel menu in alto su Unity
    [MenuItem("GitHub/Salva e Chiudi Sessione")]
    public static void SalvaECommit()
    {
        // 1. Salva i cambiamenti alle scene e agli asset correnti
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("Progetto Unity salvato con successo.");

        // 2. Otteniamo il percorso principale del progetto (Root Directory)
        string projectPath = Path.GetDirectoryName(Application.dataPath);
        
        // Formattiamo il percorso usando gli slash corretti per Git
        string gitSafePath = projectPath.Replace("\\", "/");

        // 3. Esegue i comandi Git in sequenza
        string commitMessage = $"Sessione terminata il {System.DateTime.Now:dd/MM/yyyy HH:mm}";
        
        // Concateniamo la configurazione di sicurezza prima del commit
#if UNITY_EDITOR_WIN
        string command = $"/c git config --global --add safe.directory \"{gitSafePath}\" && git add . && git commit -m \"{commitMessage}\" && git push";
        EseguiComando("cmd.exe", command, projectPath);
#else
        string command = $"-c \"git config --global --add safe.directory '{gitSafePath}' && git add . && git commit -m '{commitMessage}' && git push\"";
        EseguiComando("/bin/bash", command, projectPath);
#endif
    }

    private static void EseguiComando(string filename, string arguments, string workingDirectory)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            WorkingDirectory = workingDirectory, // <-- Parametro fondamentale per il path di esecuzione
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output)) UnityEngine.Debug.Log($"Git Output: {output}");
            
            // Nota: Git spesso stampa informazioni sul flusso 'Error' anche se non ci sono problemi critici.
            if (!string.IsNullOrEmpty(error)) UnityEngine.Debug.LogWarning($"Git Nota/Errore: {error}");
        }
    }
}