using System;
using System.IO;

namespace Aronium.Pos.MultiUom.Installer
{
    internal static partial class InstallerPayload
    {
        public const string ModuleResource = "UomPayload.Aronium.Pos.MultiUom.dll";
        public const string IndonesianResource = "UomPayload.id.lang";
        public const string EnglishResource = "UomPayload.en.lang";
        public const string MigrationResource = "UomPayload.1_0__Add_MultiUom_Transaction_Details.sql";
        public const string ReadmeResource = "UomPayload.README_MultiUOM.txt";
        public const string ReceiptPatchedResource = "ReceiptPatchPayload.Aronium.Pos.ReceiptPrinter.dll";

        public static string ExtractPayload(string tempDirectory)
        {
            Directory.CreateDirectory(tempDirectory);
            Write(tempDirectory, "Aronium.Pos.MultiUom.dll", ModuleResource);
            Write(tempDirectory, "id.lang", IndonesianResource);
            Write(tempDirectory, "en.lang", EnglishResource);
            Write(tempDirectory, "1_0__Add_MultiUom_Transaction_Details.sql", MigrationResource);
            Write(tempDirectory, "README_MultiUOM.txt", ReadmeResource);
            Write(tempDirectory, "Aronium.Pos.ReceiptPrinter.patched.dll", ReceiptPatchedResource);
            return tempDirectory;
        }

        private static void Write(string directory, string fileName, string resource)
        {
            File.WriteAllBytes(Path.Combine(directory, fileName), ReadResource(resource));
        }
    }
}
