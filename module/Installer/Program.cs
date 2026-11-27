using System;
using System.IO;
using System.Reflection;
using System.Text;

[assembly: AssemblyTitle("Installer Module UOM + Receipt Printer Patch")]
[assembly: AssemblyProduct("Aronium.Pos.MultiUom Installer")]
[assembly: AssemblyVersion("1.0.5.0")]
[assembly: AssemblyFileVersion("1.0.5.0")]

namespace Aronium.Pos.MultiUom.Installer
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Installer Module UOM + Receipt Printer Patch";

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

            try
            {
                if (HasArgument(args, "--help", "-h"))
                {
                    PrintUsage();
                    return 0;
                }

                bool enablePaymentTypes = HasArgument(args, "--enable-payment-types");
                bool disablePaymentTypes = HasArgument(args, "--disable-payment-types");
                if (enablePaymentTypes && disablePaymentTypes)
                    throw new ArgumentException("Gunakan hanya satu opsi --enable-payment-types atau --disable-payment-types.");
                bool? requestedPaymentTypes = enablePaymentTypes
                    ? (bool?)true
                    : disablePaymentTypes ? (bool?)false : null;

                bool restoreReceiptPatch = HasArgument(args, "--restore-receipt-patch");
                bool noLaunch = HasArgument(args, "--no-launch");
                bool noPause = HasArgument(args, "--no-pause");
                string appPathOverride = GetArgument(args, "--path");

                Console.WriteLine("=====================================================");
                Console.WriteLine(" Installer Module UOM + Patch ReceiptPrinter");
                Console.WriteLine("=====================================================");
                Console.WriteLine();

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

                string executable = InstallerPayload.FindAronium(appPathOverride);
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

                string app = Path.GetDirectoryName(executable);
                if (restoreReceiptPatch)
                {
                    log("Memulihkan module ReceiptPrinter asli...");
                    InstallerPayload.RestoreReceiptPatch(app, log);
                    bool launchAfterRestore = !noLaunch && AskYesNo("Mulai Aronium POS setelah selesai? [Y/N]: ");
                    if (launchAfterRestore)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable) { WorkingDirectory = app });
                    Console.WriteLine();
                    Console.WriteLine("Patch ReceiptPrinter berhasil dipulihkan. Module UOM tetap terpasang.");
                    Console.WriteLine("Log installer: " + logPath);
                    Pause(noPause);
                    return 0;
                }

                bool launch = !noLaunch && AskYesNo("Mulai Aronium POS setelah selesai? [Y/N]: ");
                InstallResult result = InstallerPayload.Install(log, launch, requestedPaymentTypes, appPathOverride);
                Console.WriteLine();
                Console.WriteLine(result.Message);
                if (result.BackupPath != null) Console.WriteLine("Backup database: " + result.BackupPath);
                if (result.ReceiptPatchBackupPath != null)
                    Console.WriteLine("Backup DLL ReceiptPrinter: " + result.ReceiptPatchBackupPath);
                Console.WriteLine("Log installer: " + logPath);
                if (result.Success)
                    Console.WriteLine("Restore patch: \"" + Assembly.GetExecutingAssembly().Location + "\" --restore-receipt-patch --path \"" + app + "\"");
                Pause(noPause);
                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                log("Gagal: " + ex.Message);
                Console.Error.WriteLine(ex.ToString());
                Pause(HasArgument(args, "--no-pause"));
                return 1;
            }
        }

        private static bool AskYesNo(string prompt)
        {
            Console.Write(prompt);
            string answer = Console.ReadLine();
            return !string.IsNullOrWhiteSpace(answer) && answer.Trim().ToUpperInvariant().StartsWith("Y");
        }

        private static void Pause(bool noPause)
        {
            if (noPause) return;
            Console.Write("Tekan Enter untuk menutup...");
            Console.ReadLine();
        }

        private static bool HasArgument(string[] args, params string[] names)
        {
            foreach (string argument in args)
                foreach (string name in names)
                    if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Installer Module UOM + Patch ReceiptPrinter untuk Aronium POS 1.51.1.0");
            Console.WriteLine();
            Console.WriteLine("Opsi:");
            Console.WriteLine("  --path \"C:\\Program Files\\Aronium\"  Path instalasi Aronium.");
            Console.WriteLine("  --enable-payment-types             Aktifkan tipe pembayaran pada receipt.");
            Console.WriteLine("  --disable-payment-types            Nonaktifkan tipe pembayaran pada receipt.");
            Console.WriteLine("  --restore-receipt-patch            Pulihkan DLL ReceiptPrinter asli.");
            Console.WriteLine("  --no-launch                        Jangan mulai Aronium setelah selesai.");
            Console.WriteLine("  --no-pause                         Jangan menunggu tombol Enter.");
            Console.WriteLine("  --help                             Tampilkan bantuan ini.");
        }
    }
}
