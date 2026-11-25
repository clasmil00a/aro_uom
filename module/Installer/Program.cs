using System;
using System.IO;
using System.Reflection;
using System.Text;

[assembly: AssemblyTitle("Installer Module UOM")]
[assembly: AssemblyProduct("Aronium.Pos.MultiUom Installer")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]

namespace Aronium.Pos.MultiUom.Installer
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Installer Module UOM";
            Console.WriteLine("==========================================");
            Console.WriteLine(" Installer Module UOM untuk Aronium POS");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aronium", "Log", "ModuleUomInstaller.log");
            Action<string> log = message =>
            {
                Console.WriteLine(message);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                    File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
                }
                catch { }
            };

            if (!InstallerPayload.IsAdministrator())
            {
                log("Installer harus dijalankan sebagai administrator.");
                return 2;
            }
            if (!InstallerPayload.IsDotNetSupported())
            {
                log("PC ini belum mendukung .NET Framework 4.7.1 atau lebih baru.");
                return 3;
            }
            if (InstallerPayload.IsAroniumRunning())
            {
                log("Tutup Aronium POS sebelum melanjutkan.");
                return 4;
            }

            string executable = InstallerPayload.FindAronium();
            if (executable == null)
            {
                log("Aronium.Pos.exe tidak ditemukan.");
                return 5;
            }
            string version = InstallerPayload.GetAroniumVersion(executable);
            log("Aronium POS ditemukan: " + executable);
            log("Versi Aronium: " + version);
            if (version != InstallerPayload.RequiredAroniumVersion)
            {
                log("Versi Aronium harus " + InstallerPayload.RequiredAroniumVersion + ".");
                return 6;
            }

            Console.Write("Mulai Aronium POS setelah selesai? [Y/N]: ");
            bool launch = Console.ReadLine().Trim().ToUpperInvariant().StartsWith("Y");
            Console.WriteLine();

            InstallResult result = InstallerPayload.Install(log, launch);
            Console.WriteLine();
            Console.WriteLine(result.Message);
            if (result.BackupPath != null) Console.WriteLine("Backup database: " + result.BackupPath);
            Console.WriteLine("Log installer: " + logPath);
            Console.WriteLine("Tekan Enter untuk menutup...");
            Console.ReadLine();
            return result.Success ? 0 : 1;
        }
    }
}
