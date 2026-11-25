using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;

namespace Aronium.Pos.MultiUom.Installer
{
    internal sealed class InstallResult
    {
        public bool Success;
        public string Message;
        public string BackupPath;
        public string AroniumPath;
    }

    internal static partial class InstallerPayload
    {
        public const string RequiredAroniumVersion = "1.51.1.0";
        public const int MinimumDotNetRelease = 461808;

        public static byte[] ReadResource(string name)
        {
            using (Stream stream = typeof(InstallerPayload).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Payload installer tidak ditemukan: " + name);
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool IsAroniumRunning()
        {
            return Process.GetProcessesByName("Aronium.Pos").Any(p => !p.HasExited);
        }

        public static bool IsDotNetSupported()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key == null) return false;
                    object release = key.GetValue("Release");
                    return release != null && Convert.ToInt32(release) >= MinimumDotNetRelease;
                }
            }
            catch { return false; }
        }

        public static string FindAronium()
        {
            var candidates = new List<string>();
            foreach (string root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
                if (!string.IsNullOrEmpty(root)) candidates.Add(Path.Combine(root, "Aronium", "Aronium.Pos.exe"));

            foreach (string candidate in candidates)
                if (File.Exists(candidate)) return candidate;

            using (RegistryKey uninstall = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (uninstall != null)
                    foreach (string name in uninstall.GetSubKeyNames())
                    {
                        using (RegistryKey item = uninstall.OpenSubKey(name))
                        {
                            string display = item == null ? null : item.GetValue("DisplayName") as string;
                            string location = item == null ? null : item.GetValue("InstallLocation") as string;
                            if (display != null && display.IndexOf("Aronium", StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrEmpty(location))
                            {
                                string path = Path.Combine(location, "Aronium.Pos.exe");
                                if (File.Exists(path)) return path;
                            }
                        }
                    }
            }
            return null;
        }

        public static string GetAroniumVersion(string executable)
        {
            return FileVersionInfo.GetVersionInfo(executable).FileVersion;
        }

        public static string BackupDatabase()
        {
            string db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aronium", "Data", "pos.db");
            if (!File.Exists(db)) return null;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string backup = db + ".bak_multiuom_install_" + stamp;
            File.Copy(db, backup, true);
            return backup;
        }
    }
}
