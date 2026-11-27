using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Aronium.Pos.MultiUom.Installer
{
    internal enum ReceiptPatchState
    {
        Original,
        AlreadyPatched
    }

    internal static partial class InstallerPayload
    {
        public const string ReceiptModuleName = "Aronium.Pos.ReceiptPrinter";
        public const string ReceiptBackupFolderName = "ReceiptPrinterPaymentTypesPatch";
        public const string SupportedOriginalReceiptHash = "74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0";
        public const string PatchedReceiptHash = "2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9";

        public static ReceiptPatchState ValidateReceiptPatchTarget(string appPath, byte[] patchedAssembly)
        {
            if (patchedAssembly == null || patchedAssembly.Length == 0)
                throw new InvalidOperationException("Payload patch ReceiptPrinter kosong.");
            string embeddedHash = ComputeSha256(patchedAssembly);
            if (!string.Equals(embeddedHash, PatchedReceiptHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Hash payload patch ReceiptPrinter tidak valid: " + embeddedHash);

            string modulePath = GetReceiptModulePath(appPath);
            if (!File.Exists(modulePath))
                throw new FileNotFoundException("Module ReceiptPrinter tidak ditemukan: " + modulePath, modulePath);

            string currentHash = ComputeFileHash(modulePath);
            bool isPatched = string.Equals(currentHash, PatchedReceiptHash, StringComparison.OrdinalIgnoreCase);
            bool isOriginal = string.Equals(currentHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase);
            if (!isPatched && !isOriginal)
                throw new InvalidOperationException(
                    "Build ReceiptPrinter ini tidak didukung. Hash terpasang: " + currentHash +
                    ". File asli tidak diubah.");

            AssemblyName name = AssemblyName.GetAssemblyName(modulePath);
            if (!string.Equals(name.Name, ReceiptModuleName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Target DLL tidak cocok dengan " + ReceiptModuleName + ".");
            if (name.Version == null || name.Version.Major != 1)
                throw new InvalidOperationException("Versi module ReceiptPrinter tidak didukung: " + name.Version + ".");

            return isPatched ? ReceiptPatchState.AlreadyPatched : ReceiptPatchState.Original;
        }

        public static ReceiptPatchState InstallReceiptPatch(
            string appPath,
            byte[] patchedAssembly,
            bool? enablePaymentTypes,
            Action<string> log)
        {
            string modulePath = GetReceiptModulePath(appPath);
            ReceiptPatchState state = ValidateReceiptPatchTarget(appPath, patchedAssembly);
            string backupRoot = GetReceiptBackupRoot();
            string originalBackup = Path.Combine(backupRoot, ReceiptModuleName + ".original.dll");
            Directory.CreateDirectory(backupRoot);

            if (state == ReceiptPatchState.Original)
            {
                if (File.Exists(originalBackup))
                {
                    string backupHash = ComputeFileHash(originalBackup);
                    if (!string.Equals(backupHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Backup DLL ReceiptPrinter asli tidak valid: " + backupHash);
                }
                else
                {
                    File.WriteAllBytes(originalBackup, File.ReadAllBytes(modulePath));
                    log("Backup DLL ReceiptPrinter asli: " + originalBackup);
                }

                CopyAtomicBytes(patchedAssembly, modulePath);
                log("Patch ReceiptPrinter berhasil dipasang.");
            }
            else
            {
                if (File.Exists(originalBackup))
                {
                    string backupHash = ComputeFileHash(originalBackup);
                    if (!string.Equals(backupHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Backup DLL ReceiptPrinter asli tidak valid: " + backupHash);
                }
                else
                {
                    log("PERINGATAN: patch ReceiptPrinter sudah terpasang, tetapi backup DLL asli tidak ditemukan.");
                }
                log("Patch ReceiptPrinter sudah terpasang; DLL tidak perlu diganti.");
            }

            string installedHash = ComputeFileHash(modulePath);
            if (!string.Equals(installedHash, PatchedReceiptHash, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Verifikasi checksum DLL ReceiptPrinter gagal.");

            Dictionary<string, string> priorState = ReadState(GetReceiptStatePath());
            bool priorCreatedIndonesian = ReadBooleanState(priorState, "createdIndonesian");
            bool createdIndonesian = PatchTranslations(Path.Combine(appPath, "Modules", ReceiptModuleName, "Lang")) || priorCreatedIndonesian;
            bool? configuredValue = EnsurePaymentTypesConfig(GetReceiptConfigPath(), enablePaymentTypes);
            WriteState(appPath, originalBackup, createdIndonesian, configuredValue);

            log("Opsi 'Tampilkan tipe pembayaran' tersedia di Pengaturan > Print options > Customize receipt.");
            return state;
        }

        public static void RestoreReceiptPatch(string appPath, Action<string> log)
        {
            string statePath = GetReceiptStatePath();
            Dictionary<string, string> state = ReadState(statePath);
            string backupPath = state.ContainsKey("backupPath")
                ? state["backupPath"]
                : Path.Combine(GetReceiptBackupRoot(), ReceiptModuleName + ".original.dll");
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Backup DLL ReceiptPrinter asli tidak ditemukan: " + backupPath, backupPath);

            string backupHash = ComputeFileHash(backupPath);
            if (!string.Equals(backupHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Backup DLL ReceiptPrinter asli tidak valid: " + backupHash);

            string modulePath = GetReceiptModulePath(appPath);
            if (!File.Exists(modulePath))
                throw new FileNotFoundException("Module ReceiptPrinter tidak ditemukan: " + modulePath, modulePath);
            string currentHash = ComputeFileHash(modulePath);
            if (!string.Equals(currentHash, PatchedReceiptHash, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(currentHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Module ReceiptPrinter saat ini memiliki hash yang tidak didukung: " + currentHash);

            CopyAtomic(backupPath, modulePath);
            string restoredHash = ComputeFileHash(modulePath);
            if (!string.Equals(restoredHash, SupportedOriginalReceiptHash, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Verifikasi restore DLL ReceiptPrinter gagal.");

            RemoveTranslationValue(Path.Combine(appPath, "Modules", ReceiptModuleName, "Lang"));
            bool createdIndonesian = ReadBooleanState(state, "createdIndonesian");
            if (createdIndonesian)
            {
                string indonesian = Path.Combine(appPath, "Modules", ReceiptModuleName, "Lang", "id.lang");
                if (File.Exists(indonesian)) File.Delete(indonesian);
            }

            RemovePaymentTypesConfig(GetReceiptConfigPath());
            if (File.Exists(statePath)) File.Delete(statePath);
            log("Module ReceiptPrinter asli dan pengaturan payment types berhasil dipulihkan.");
            log("Module UOM tidak dihapus oleh proses restore ini.");
        }

        public static string[] GetReceiptPatchAffectedFiles(string appPath)
        {
            var files = new List<string>();
            files.Add(GetReceiptModulePath(appPath));
            string langDirectory = Path.Combine(appPath, "Modules", ReceiptModuleName, "Lang");
            if (Directory.Exists(langDirectory))
                files.AddRange(Directory.GetFiles(langDirectory, "*.lang", SearchOption.TopDirectoryOnly));
            files.Add(GetReceiptConfigPath());
            files.Add(GetReceiptStatePath());
            return files.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static string GetReceiptModulePath(string appPath)
        {
            return Path.Combine(appPath, "Modules", ReceiptModuleName, ReceiptModuleName + ".dll");
        }

        public static string GetReceiptBackupRoot()
        {
            return Path.Combine(GetPatchLocalDataRoot(), "Aronium", ReceiptBackupFolderName);
        }

        public static string GetReceiptConfigPath()
        {
            return Path.Combine(GetPatchLocalDataRoot(), "Aronium", ReceiptModuleName, "Aronium.ReceiptPrinter.properties");
        }

        public static string GetReceiptStatePath()
        {
            return Path.Combine(GetReceiptBackupRoot(), "state.txt");
        }

        private static string GetPatchLocalDataRoot()
        {
            string overridePath = Environment.GetEnvironmentVariable("ARONIUM_PATCH_DATA_ROOT");
            return string.IsNullOrWhiteSpace(overridePath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : overridePath;
        }

        private static bool PatchTranslations(string langDirectory)
        {
            Directory.CreateDirectory(langDirectory);
            foreach (string file in Directory.GetFiles(langDirectory, "*.lang", SearchOption.TopDirectoryOnly))
            {
                XmlDocument document = LoadXml(file);
                XmlElement root = document.DocumentElement;
                if (root == null) continue;
                bool changed = false;

                XmlElement receiptPrinter = root.SelectSingleNode("ReceiptPrinter") as XmlElement;
                if (receiptPrinter == null)
                {
                    receiptPrinter = document.CreateElement("ReceiptPrinter");
                    root.AppendChild(receiptPrinter);
                    changed = true;
                }

                XmlElement receipt = receiptPrinter.SelectSingleNode("Receipt") as XmlElement;
                if (receipt == null)
                {
                    receipt = document.CreateElement("Receipt");
                    receiptPrinter.AppendChild(receipt);
                    changed = true;
                }

                XmlElement customize = receipt.SelectSingleNode("Customize") as XmlElement;
                if (customize == null)
                {
                    customize = document.CreateElement("Customize");
                    receipt.AppendChild(customize);
                    changed = true;
                }

                XmlElement item = customize.SelectSingleNode("PrintPaymentTypes") as XmlElement;
                if (item == null)
                {
                    item = document.CreateElement("PrintPaymentTypes");
                    customize.AppendChild(item);
                    changed = true;
                }

                string code = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                string desiredText = code == "id" ? "Tampilkan tipe pembayaran" : "Print payment types";
                if (!string.Equals(item.GetAttribute("Text"), desiredText, StringComparison.Ordinal))
                {
                    item.SetAttribute("Text", desiredText);
                    changed = true;
                }

                if (changed) SaveXml(document, file);
            }

            string indonesian = Path.Combine(langDirectory, "id.lang");
            if (!File.Exists(indonesian))
            {
                const string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Language name=\"Indonesia\" code=\"id\">\r\n  <ReceiptPrinter Text=\"Printer struk\">\r\n    <Receipt>\r\n      <Customize>\r\n        <PrintPaymentTypes Text=\"Tampilkan tipe pembayaran\" />\r\n      </Customize>\r\n    </Receipt>\r\n  </ReceiptPrinter>\r\n</Language>\r\n";
                File.WriteAllText(indonesian, content, new UTF8Encoding(false));
                return true;
            }
            return false;
        }

        private static void RemoveTranslationValue(string langDirectory)
        {
            if (!Directory.Exists(langDirectory)) return;
            foreach (string file in Directory.GetFiles(langDirectory, "*.lang", SearchOption.TopDirectoryOnly))
            {
                XmlDocument document = LoadXml(file);
                XmlNode item = document.SelectSingleNode("/Language/ReceiptPrinter/Receipt/Customize/PrintPaymentTypes");
                if (item == null) continue;
                item.ParentNode.RemoveChild(item);
                SaveXml(document, file);
            }
        }

        private static bool? EnsurePaymentTypesConfig(string path, bool? requestedValue)
        {
            XmlDocument document = File.Exists(path) ? LoadXml(path) : new XmlDocument();
            if (document.DocumentElement == null) document.AppendChild(document.CreateElement("Config"));
            XmlElement root = document.DocumentElement;
            XmlElement item = root.SelectSingleNode("PrintPaymentTypes") as XmlElement;
            bool changed = false;
            if (item == null)
            {
                item = document.CreateElement("PrintPaymentTypes");
                root.AppendChild(item);
                changed = true;
            }

            bool effectiveValue;
            if (requestedValue.HasValue)
            {
                effectiveValue = requestedValue.Value;
                if (!string.Equals(item.InnerText, effectiveValue ? "true" : "false", StringComparison.OrdinalIgnoreCase))
                {
                    item.InnerText = effectiveValue ? "true" : "false";
                    changed = true;
                }
            }
            else
            {
                bool parsed;
                if (!TryParseBoolean(item.InnerText, out parsed))
                {
                    effectiveValue = false;
                    item.InnerText = "false";
                    changed = true;
                }
                else effectiveValue = parsed;
            }

            if (changed) SaveXml(document, path);
            return effectiveValue;
        }

        private static void RemovePaymentTypesConfig(string path)
        {
            if (!File.Exists(path)) return;
            XmlDocument document = LoadXml(path);
            XmlNode item = document.SelectSingleNode("/Config/PrintPaymentTypes");
            if (item == null) return;
            item.ParentNode.RemoveChild(item);
            SaveXml(document, path);
        }

        private static void WriteState(string appPath, string backupPath, bool createdIndonesian, bool? configuredValue)
        {
            string path = GetReceiptStatePath();
            string content = "appPath=" + appPath + Environment.NewLine +
                             "backupPath=" + backupPath + Environment.NewLine +
                             "createdIndonesian=" + createdIndonesian.ToString() + Environment.NewLine +
                             "patchedAssemblyHash=" + PatchedReceiptHash + Environment.NewLine +
                             "printPaymentTypes=" + (configuredValue.HasValue ? configuredValue.Value.ToString() : "preserve") + Environment.NewLine;
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static Dictionary<string, string> ReadState(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return result;
            foreach (string line in File.ReadAllLines(path))
            {
                int separator = line.IndexOf('=');
                if (separator > 0) result[line.Substring(0, separator)] = line.Substring(separator + 1);
            }
            return result;
        }

        private static bool ReadBooleanState(Dictionary<string, string> state, string key)
        {
            bool value;
            return state.ContainsKey(key) && TryParseBoolean(state[key], out value) && value;
        }

        private static bool TryParseBoolean(string value, out bool result)
        {
            return bool.TryParse(value == null ? null : value.Trim(), out result);
        }

        private static XmlDocument LoadXml(string path)
        {
            var document = new XmlDocument();
            document.PreserveWhitespace = true;
            document.Load(path);
            return document;
        }

        private static void SaveXml(XmlDocument document, string path)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = false
            };
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using (XmlWriter writer = XmlWriter.Create(path, settings)) document.Save(writer);
        }

        private static string ComputeFileHash(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
        }

        private static string ComputeSha256(byte[] data)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", "");
        }
    }
}
