using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ruinas
{
    /// <summary>Escrita atômica com cópia de recuperação (tmp -> replace -> .bak).</summary>
    public static class SafeFile
    {
        public static bool WriteAtomic(string path, string content, string backupPath = null)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string tmp = path + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(content);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(tmp, path, backupPath, true);
                    }
                    catch (Exception)
                    {
                        // Alguns sistemas de arquivos não suportam Replace: faz a troca manualmente.
                        if (!string.IsNullOrEmpty(backupPath)) File.Copy(path, backupPath, true);
                        File.Delete(path);
                        File.Move(tmp, path);
                    }
                }
                else
                {
                    File.Move(tmp, path);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SafeFile] Falha ao gravar '{path}': {e.Message}");
                return false;
            }
        }

        public static string TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SafeFile] Falha ao ler '{path}': {e.Message}");
                return null;
            }
        }
    }
}
