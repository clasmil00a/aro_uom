using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Aronium.Pos.MultiUom.Installer
{
    internal static partial class InstallerPayload
    {
        private sealed class FileSnapshot
        {
            public string Path;
            public bool Existed;
            public byte[] Content;
        }

        public static InstallResult Install(
            Action<string> log,
            bool launchAronium,
            bool? enablePaymentTypes,
            string appPathOverride)
        {
            var result = new InstallResult();
            List<FileSnapshot> snapshots = null;
            string temp = Path.Combine(Path.GetTempPath(), "Aronium.Pos.MultiUom.Install." + Guid.NewGuid().ToString("N"));
            try
            {
                if (!IsAdministrator()) throw new InvalidOperationException("Installer harus dijalankan sebagai administrator.");
                if (!IsDotNetSupported()) throw new InvalidOperationException("PC ini belum mendukung .NET Framework 4.7.1 atau lebih baru.");
                if (IsAroniumRunning()) throw new InvalidOperationException("Tutup Aronium POS sebelum melanjutkan.");

                string executable = FindAronium(appPathOverride);
                if (executable == null) throw new FileNotFoundException("Aronium.Pos.exe tidak ditemukan.");
                string version = GetAroniumVersion(executable);
                if (version != RequiredAroniumVersion)
                    throw new InvalidOperationException("Versi Aronium harus " + RequiredAroniumVersion + ". Tersedia: " + version);
                result.AroniumPath = executable;
                string app = Path.GetDirectoryName(executable);

                ExtractPayload(temp);
                string uomPayload = Path.Combine(temp, "Aronium.Pos.MultiUom.dll");
                string receiptPayload = Path.Combine(temp, "Aronium.Pos.ReceiptPrinter.patched.dll");
                ReceiptPatchState receiptState = ValidateReceiptPatchTarget(app, File.ReadAllBytes(receiptPayload));

                string module = Path.Combine(app, "Modules", "Aronium.Pos.MultiUom");
                string migration = Path.Combine(app, "Migrations", "Lite", "Aronium.Pos.MultiUom");
                string tools = Path.Combine(app, "Tools");
                string[] uomTargets = GetUomTargetFiles(app);
                snapshots = CaptureSnapshots(uomTargets);
                snapshots.AddRange(CaptureSnapshots(GetReceiptPatchAffectedFiles(app)));

                result.UomAlreadyInstalled = FileExistsWithSameHash(
                    Path.Combine(module, "Aronium.Pos.MultiUom.dll"), uomPayload);
                result.ReceiptPatchAlreadyApplied = receiptState == ReceiptPatchState.AlreadyPatched;
                result.ReceiptPatchUpgraded = receiptState == ReceiptPatchState.PreviousPatched;

                log("Mencadangkan database...");
                result.BackupPath = BackupDatabase();
                if (result.BackupPath != null) log("Backup: " + result.BackupPath);

                InstallUomPayload(temp, app, log);
                InstallReceiptPatch(app, File.ReadAllBytes(receiptPayload), enablePaymentTypes, log);
                result.ReceiptPatchBackupPath = Path.Combine(GetReceiptBackupRoot(), ReceiptModuleName + ".original.dll");

                VerifyChecksum(uomPayload, Path.Combine(module, "Aronium.Pos.MultiUom.dll"));
                string installedReceiptHash = ComputeFileHash(GetReceiptModulePath(app));
                if (!string.Equals(installedReceiptHash, PatchedReceiptHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Verifikasi checksum DLL ReceiptPrinter gagal.");

                result.Success = true;
                result.Message = "Module UOM dan patch ReceiptPrinter berhasil diinstal.";
                if (result.UomAlreadyInstalled)
                    log("Module UOM sudah terpasang dengan payload yang sama; tidak ada perubahan modul.");
                if (result.ReceiptPatchAlreadyApplied)
                    log("Patch ReceiptPrinter sudah terpasang; setting yang ada dipertahankan.");
                if (result.ReceiptPatchUpgraded)
                    log("Patch ReceiptPrinter sebelumnya berhasil diperbarui; setting yang ada dipertahankan.");
                if (launchAronium) Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = app });
            }
            catch (Exception ex)
            {
                log("Gagal: " + ex.Message);
                if (snapshots != null)
                {
                    string rollbackError = RestoreSnapshots(snapshots);
                    if (string.IsNullOrEmpty(rollbackError))
                        log("Perubahan file dibatalkan; kondisi sebelumnya dipulihkan.");
                    else
                        log("PERINGATAN: rollback tidak lengkap: " + rollbackError);
                }
                result.Success = false;
                result.Message = snapshots == null
                    ? ex.Message
                    : ex.Message + " Perubahan file telah dicoba dikembalikan.";
            }
            finally
            {
                try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
            }
            return result;
        }

        private static void InstallUomPayload(string temp, string app, Action<string> log)
        {
            string module = Path.Combine(app, "Modules", "Aronium.Pos.MultiUom");
            string migration = Path.Combine(app, "Migrations", "Lite", "Aronium.Pos.MultiUom");
            string tools = Path.Combine(app, "Tools");
            Directory.CreateDirectory(module);
            Directory.CreateDirectory(migration);
            Directory.CreateDirectory(tools);

            bool changed = false;
            changed |= CopyAtomicIfChanged(Path.Combine(temp, "Aronium.Pos.MultiUom.dll"), Path.Combine(module, "Aronium.Pos.MultiUom.dll"));
            changed |= CopyAtomicIfChanged(Path.Combine(temp, "id.lang"), Path.Combine(module, "Lang", "id.lang"));
            changed |= CopyAtomicIfChanged(Path.Combine(temp, "en.lang"), Path.Combine(module, "Lang", "en.lang"));
            changed |= CopyAtomicIfChanged(Path.Combine(temp, "1_0__Add_MultiUom_Transaction_Details.sql"), Path.Combine(migration, "1_0__Add_MultiUom_Transaction_Details.sql"));
            changed |= CopyAtomicIfChanged(Path.Combine(temp, "README_MultiUOM.txt"), Path.Combine(tools, "README_MultiUOM.txt"));
            log(changed
                ? "Module UOM berhasil dipasang atau diperbarui."
                : "Module UOM sudah menggunakan payload terbaru.");
        }

        private static string[] GetUomTargetFiles(string app)
        {
            return new[]
            {
                Path.Combine(app, "Modules", "Aronium.Pos.MultiUom", "Aronium.Pos.MultiUom.dll"),
                Path.Combine(app, "Modules", "Aronium.Pos.MultiUom", "Lang", "id.lang"),
                Path.Combine(app, "Modules", "Aronium.Pos.MultiUom", "Lang", "en.lang"),
                Path.Combine(app, "Migrations", "Lite", "Aronium.Pos.MultiUom", "1_0__Add_MultiUom_Transaction_Details.sql"),
                Path.Combine(app, "Tools", "README_MultiUOM.txt")
            };
        }

        private static List<FileSnapshot> CaptureSnapshots(IEnumerable<string> paths)
        {
            var snapshots = new List<FileSnapshot>();
            foreach (string path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var snapshot = new FileSnapshot { Path = path, Existed = File.Exists(path) };
                if (snapshot.Existed) snapshot.Content = File.ReadAllBytes(path);
                snapshots.Add(snapshot);
            }
            return snapshots;
        }

        private static string RestoreSnapshots(IEnumerable<FileSnapshot> snapshots)
        {
            var errors = new List<string>();
            var ordered = new List<FileSnapshot>(snapshots);
            ordered.Reverse();
            foreach (FileSnapshot snapshot in ordered)
            {
                try
                {
                    if (snapshot.Existed)
                    {
                        CopyAtomicBytes(snapshot.Content, snapshot.Path);
                    }
                    else if (File.Exists(snapshot.Path))
                    {
                        File.Delete(snapshot.Path);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(snapshot.Path + ": " + ex.Message);
                }
            }
            return string.Join("; ", errors.ToArray());
        }

        private static bool FileExistsWithSameHash(string path, string comparisonPath)
        {
            return File.Exists(path) && File.Exists(comparisonPath) && FilesEqual(path, comparisonPath);
        }

        private static bool CopyAtomicIfChanged(string source, string target)
        {
            if (File.Exists(target) && FilesEqual(source, target)) return false;
            CopyAtomic(source, target);
            return true;
        }

        private static void CopyAtomic(string source, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".new";
            try
            {
                File.Copy(source, temporary, true);
                if (File.Exists(target))
                {
                    try { File.Replace(temporary, target, null); }
                    catch { File.Delete(target); File.Move(temporary, target); }
                }
                else File.Move(temporary, target);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            }
        }

        private static void CopyAtomicBytes(byte[] content, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".rollback";
            try
            {
                File.WriteAllBytes(temporary, content);
                if (File.Exists(target))
                {
                    try { File.Replace(temporary, target, null); }
                    catch { File.Delete(target); File.Move(temporary, target); }
                }
                else File.Move(temporary, target);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            }
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
