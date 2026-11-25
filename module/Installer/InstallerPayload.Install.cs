using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Aronium.Pos.MultiUom.Installer
{
    internal static partial class InstallerPayload
    {
        public static InstallResult Install(Action<string> log, bool launchAronium)
        {
            var result = new InstallResult();
            string temp = Path.Combine(Path.GetTempPath(), "Aronium.Pos.MultiUom.Install." + Guid.NewGuid().ToString("N"));
            try
            {
                if (!IsAdministrator()) throw new InvalidOperationException("Installer harus dijalankan sebagai administrator.");
                if (!IsDotNetSupported()) throw new InvalidOperationException("PC ini belum mendukung .NET Framework 4.7.1 atau lebih baru.");
                if (IsAroniumRunning()) throw new InvalidOperationException("Tutup Aronium POS sebelum melanjutkan.");

                string executable = FindAronium();
                if (executable == null) throw new FileNotFoundException("Aronium.Pos.exe tidak ditemukan.");
                string version = GetAroniumVersion(executable);
                if (version != RequiredAroniumVersion)
                    throw new InvalidOperationException("Versi Aronium harus " + RequiredAroniumVersion + ". Tersedia: " + version);
                result.AroniumPath = executable;

                log("Mencadangkan database...");
                result.BackupPath = BackupDatabase();
                if (result.BackupPath != null) log("Backup: " + result.BackupPath);

                ExtractPayload(temp);
                string app = Path.GetDirectoryName(executable);
                string module = Path.Combine(app, "Modules", "Aronium.Pos.MultiUom");
                string migration = Path.Combine(app, "Migrations", "Lite", "Aronium.Pos.MultiUom");
                string tools = Path.Combine(app, "Tools");
                Directory.CreateDirectory(module);
                Directory.CreateDirectory(migration);
                Directory.CreateDirectory(tools);

                CopyAtomic(Path.Combine(temp, "Aronium.Pos.MultiUom.dll"), Path.Combine(module, "Aronium.Pos.MultiUom.dll"));
                CopyAtomic(Path.Combine(temp, "id.lang"), Path.Combine(module, "Lang", "id.lang"));
                CopyAtomic(Path.Combine(temp, "en.lang"), Path.Combine(module, "Lang", "en.lang"));
                CopyAtomic(Path.Combine(temp, "1_0__Add_MultiUom_Transaction_Details.sql"), Path.Combine(migration, "1_0__Add_MultiUom_Transaction_Details.sql"));
                CopyAtomic(Path.Combine(temp, "README_MultiUOM.txt"), Path.Combine(tools, "README_MultiUOM.txt"));

                VerifyChecksum(Path.Combine(temp, "Aronium.Pos.MultiUom.dll"), Path.Combine(module, "Aronium.Pos.MultiUom.dll"));
                log("Module UOM berhasil dipasang.");
                result.Success = true;
                result.Message = "Module UOM berhasil diinstal.";
                if (launchAronium) Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = app });
            }
            catch (Exception ex)
            {
                log("Gagal: " + ex.Message);
                result.Success = false;
                result.Message = ex.Message;
            }
            finally
            {
                try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
            }
            return result;
        }

        private static void CopyAtomic(string source, string target)
        {
            string directory = Path.GetDirectoryName(target);
            Directory.CreateDirectory(directory);
            string temporary = target + ".new";
            File.Copy(source, temporary, true);
            if (File.Exists(target))
            {
                try { File.Replace(temporary, target, null); }
                catch { File.Delete(target); File.Move(temporary, target); }
            }
            else File.Move(temporary, target);
        }

        private static void VerifyChecksum(string source, string target)
        {
            if (!FilesEqual(source, target)) throw new IOException("Verifikasi checksum DLL Module UOM gagal.");
        }

        private static bool FilesEqual(string left, string right)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream a = File.OpenRead(left))
            using (FileStream b = File.OpenRead(right))
                return BitConverter.ToString(hash.ComputeHash(a)) == BitConverter.ToString(hash.ComputeHash(b));
        }
    }
}
