using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

internal static class Program
{
    private const string ModuleName = "Aronium.Pos.ReceiptPrinter";
    private const string BackupFolderName = "ReceiptPrinterPaymentTypesPatch";
    private const string SupportedOriginalHash = "74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0";
    private const string SupportedPreviousPatchedHash = "2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9";
    private static byte[] patchedAssembly;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Aronium Receipt Printer Payment Types Patcher";
            patchedAssembly = LoadResource("AroniumPatch." + ModuleName + ".dll");

            bool restore = args.Any(a => a.Equals("--restore", StringComparison.OrdinalIgnoreCase));
            string appPath = GetArgument(args, "--path");
            appPath = FindAroniumPath(appPath);
            if (appPath == null) return 2;

            Console.WriteLine("Aronium: " + appPath);
            if (restore)
            {
                Restore(appPath);
            }
            else
            {
                Install(appPath, args.Any(a => a.Equals("--enable-payment-types", StringComparison.OrdinalIgnoreCase)), args.Any(a => a.Equals("--force", StringComparison.OrdinalIgnoreCase)));
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static byte[] LoadResource(string name)
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) throw new InvalidOperationException("Embedded patched module is missing.");
            using (MemoryStream memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }

    private static string GetArgument(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }

    private static string FindAroniumPath(string supplied)
    {
        List<string> candidates = new List<string>();
        if (!String.IsNullOrWhiteSpace(supplied)) candidates.Add(supplied.Trim().Trim('"'));
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        candidates.Add(Path.Combine(programFiles, "Aronium"));
        if (!String.Equals(programFiles, programFilesX86, StringComparison.OrdinalIgnoreCase))
            candidates.Add(Path.Combine(programFilesX86, "Aronium"));

        foreach (string candidate in candidates)
            if (IsAroniumPath(candidate)) return Path.GetFullPath(candidate);

        Console.Write("Masukkan path instalasi Aronium (contoh: C:\\Program Files\\Aronium): ");
        string entered = Console.ReadLine();
        if (!String.IsNullOrWhiteSpace(entered) && IsAroniumPath(entered.Trim().Trim('"')))
            return Path.GetFullPath(entered.Trim().Trim('"'));
        return null;
    }

    private static bool IsAroniumPath(string path)
    {
        if (String.IsNullOrWhiteSpace(path)) return false;
        return File.Exists(Path.Combine(path, "Modules", ModuleName, ModuleName + ".dll"));
    }

    private static string GetModulePath(string appPath)
    {
        return Path.Combine(appPath, "Modules", ModuleName, ModuleName + ".dll");
    }

    private static string GetLocalDataRoot()
    {
        string overridePath = Environment.GetEnvironmentVariable("ARONIUM_PATCH_DATA_ROOT");
        return String.IsNullOrWhiteSpace(overridePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : overridePath;
    }

    private static string GetBackupRoot()
    {
        return Path.Combine(GetLocalDataRoot(), "Aronium", BackupFolderName);
    }

    private static string GetConfigPath()
    {
        return Path.Combine(GetLocalDataRoot(), "Aronium", ModuleName, "Aronium.ReceiptPrinter.properties");
    }

    private static void Install(string appPath, bool enablePaymentTypes, bool force)
    {
        string modulePath = GetModulePath(appPath);
        AssemblyName name = AssemblyName.GetAssemblyName(modulePath);
        if (!String.Equals(name.Name, ModuleName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Target DLL does not match " + ModuleName + ".");
        if (name.Version == null || name.Version.Major != 1)
            throw new InvalidOperationException("Unsupported Aronium module version: " + name.Version + ".");

        if (IsRunning("Aronium.Pos"))
            throw new InvalidOperationException("Close Aronium first, then run this patch again.");

        string backupRoot = GetBackupRoot();
        Directory.CreateDirectory(backupRoot);
        string originalBackup = Path.Combine(backupRoot, ModuleName + ".original.dll");
        byte[] current = File.ReadAllBytes(modulePath);
        byte[] patched = patchedAssembly;
        bool alreadyPatched = current.SequenceEqual(patched);
        string currentHash = ComputeHash(current);
        bool previousPatched = String.Equals(currentHash, SupportedPreviousPatchedHash, StringComparison.OrdinalIgnoreCase);
        if (!alreadyPatched && !previousPatched && !String.Equals(currentHash, SupportedOriginalHash, StringComparison.OrdinalIgnoreCase) && !force)
            throw new InvalidOperationException("This Aronium build is not the tested version. Use --force only after making a backup.");
        if (!alreadyPatched && !File.Exists(originalBackup))
        {
            if (previousPatched)
                throw new InvalidOperationException("The previous patched module is installed, but the original DLL backup was not found: " + originalBackup);
            File.WriteAllBytes(originalBackup, current);
            Console.WriteLine("Backup: " + originalBackup);
        }
        if (File.Exists(originalBackup))
        {
            string backupHash = ComputeHash(File.ReadAllBytes(originalBackup));
            if (!String.Equals(backupHash, SupportedOriginalHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The original DLL backup is invalid: " + backupHash);
        }

        string tempPath = modulePath + ".patch-new";
        File.WriteAllBytes(tempPath, patched);
        File.Copy(tempPath, modulePath, true);
        File.Delete(tempPath);
        Console.WriteLine("Patched module installed.");

        Dictionary<string, string> priorState = ReadState(Path.Combine(backupRoot, "state.txt"));
        bool priorCreatedIndonesian = priorState.ContainsKey("createdIndonesian") && Boolean.Parse(priorState["createdIndonesian"]);
        bool createdIndonesian = PatchTranslations(Path.Combine(appPath, "Modules", ModuleName, "Lang")) || priorCreatedIndonesian;
        SetConfigValue(enablePaymentTypes);
        WriteState(appPath, originalBackup, createdIndonesian);

        Console.WriteLine();
        Console.WriteLine("SUCCESS. Payment type detail lines are " + (enablePaymentTypes ? "ENABLED" : "DISABLED") + ".");
        Console.WriteLine("Open Aronium > Settings > Print options > Customize receipt to change the toggle.");
        Console.WriteLine("Restore command: \"" + Assembly.GetExecutingAssembly().Location + "\" --restore --path \"" + appPath + "\"");
    }

    private static bool IsRunning(string processName)
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }

    private static string ComputeHash(byte[] data)
    {
        using (SHA256 algorithm = SHA256.Create())
            return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", "");
    }

    private static bool PatchTranslations(string langDirectory)
    {
        Directory.CreateDirectory(langDirectory);
        foreach (string file in Directory.GetFiles(langDirectory, "*.lang"))
        {
            XmlDocument document = LoadXml(file);
            XmlElement root = document.DocumentElement;
            if (root == null) continue;
            XmlElement receiptPrinter = EnsureElement(document, root, "ReceiptPrinter");
            XmlElement receipt = EnsureElement(document, receiptPrinter, "Receipt");
            XmlElement customize = EnsureElement(document, receipt, "Customize");
            XmlElement item = customize.SelectSingleNode("PrintPaymentTypes") as XmlElement;
            if (item == null)
            {
                item = document.CreateElement("PrintPaymentTypes");
                customize.AppendChild(item);
            }
            string code = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            item.SetAttribute("Text", code == "id" ? "Tampilkan tipe pembayaran" : "Print payment types");
            SaveXml(document, file);
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

    private static XmlElement EnsureElement(XmlDocument document, XmlNode parent, string name)
    {
        XmlElement element = parent.SelectSingleNode(name) as XmlElement;
        if (element == null)
        {
            element = document.CreateElement(name);
            parent.AppendChild(element);
        }
        return element;
    }

    private static XmlDocument LoadXml(string path)
    {
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.Load(path);
        return document;
    }

    private static void SaveXml(XmlDocument document, string path)
    {
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false
        };
        using (XmlWriter writer = XmlWriter.Create(path, settings)) document.Save(writer);
    }

    private static void SetConfigValue(bool enabled)
    {
        string path = GetConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        XmlDocument document = File.Exists(path) ? LoadXml(path) : new XmlDocument();
        if (document.DocumentElement == null) document.AppendChild(document.CreateElement("Config"));
        XmlElement root = document.DocumentElement;
        XmlElement item = root.SelectSingleNode("PrintPaymentTypes") as XmlElement;
        if (item == null)
        {
            item = document.CreateElement("PrintPaymentTypes");
            root.AppendChild(item);
        }
        item.InnerText = enabled ? "true" : "false";
        SaveXml(document, path);
    }

    private static void WriteState(string appPath, string backupPath, bool createdIndonesian)
    {
        string path = Path.Combine(GetBackupRoot(), "state.txt");
        string content = "appPath=" + appPath + Environment.NewLine +
                         "backupPath=" + backupPath + Environment.NewLine +
                         "createdIndonesian=" + createdIndonesian.ToString() + Environment.NewLine;
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static void Restore(string appPath)
    {
        if (IsRunning("Aronium.Pos"))
            throw new InvalidOperationException("Close Aronium first, then run the restore command.");

        string statePath = Path.Combine(GetBackupRoot(), "state.txt");
        Dictionary<string, string> state = ReadState(statePath);
        string backupPath = state.ContainsKey("backupPath") ? state["backupPath"] : Path.Combine(GetBackupRoot(), ModuleName + ".original.dll");
        if (!File.Exists(backupPath)) throw new InvalidOperationException("Original module backup was not found: " + backupPath);

        File.Copy(backupPath, GetModulePath(appPath), true);
        Console.WriteLine("Original module restored.");

        string langDirectory = Path.Combine(appPath, "Modules", ModuleName, "Lang");
        foreach (string file in Directory.GetFiles(langDirectory, "*.lang"))
        {
            XmlDocument document = LoadXml(file);
            XmlNode item = document.SelectSingleNode("/Language/ReceiptPrinter/Receipt/Customize/PrintPaymentTypes");
            if (item != null)
            {
                item.ParentNode.RemoveChild(item);
                SaveXml(document, file);
            }
        }
        bool createdIndonesian = state.ContainsKey("createdIndonesian") && Boolean.Parse(state["createdIndonesian"]);
        if (createdIndonesian)
        {
            string indonesian = Path.Combine(langDirectory, "id.lang");
            if (File.Exists(indonesian)) File.Delete(indonesian);
        }

        RemoveConfigValue();
        if (File.Exists(statePath)) File.Delete(statePath);
        Console.WriteLine("SUCCESS. The original Aronium module and settings were restored.");
    }

    private static Dictionary<string, string> ReadState(string path)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return result;
        foreach (string line in File.ReadAllLines(path))
        {
            int separator = line.IndexOf('=');
            if (separator > 0) result[line.Substring(0, separator)] = line.Substring(separator + 1);
        }
        return result;
    }

    private static void RemoveConfigValue()
    {
        string path = GetConfigPath();
        if (!File.Exists(path)) return;
        XmlDocument document = LoadXml(path);
        XmlNode item = document.SelectSingleNode("/Config/PrintPaymentTypes");
        if (item != null)
        {
            item.ParentNode.RemoveChild(item);
            SaveXml(document, path);
        }
    }
}
