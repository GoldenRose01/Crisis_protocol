#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO; // Necessario per gestire i percorsi di sistema

public class AutoCommitMenu
{
    // Aggiunge una voce nel menu in alto su Unity (rinominata per non fare riferimento alla chiusura)
    [MenuItem("GitHub/Salva e Invia (Push)")]
    public static void SalvaECommit()
    {
        // 1. Salva i cambiamenti alle scene e agli asset correnti senza interrompere il lavoro
        EditorApplication.ExecuteMenuItem("File/Save Project");
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("Progetto Unity salvato con successo.");

        // 2. Otteniamo il percorso principale del progetto (Root Directory)
        string projectPath = Path.GetDirectoryName(Application.dataPath);
        
        // Formattiamo il percorso usando gli slash corretti per Git
        string gitSafePath = projectPath.Replace("\\", "/");

        // 3. Messaggio di commit aggiornato (indica un salvataggio/backup in corso d'opera)
        string commitMessage = $"Backup automatico del {System.DateTime.Now:dd/MM/yyyy HH:mm}";
        
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
            WorkingDirectory = workingDirectory,
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
#endif
