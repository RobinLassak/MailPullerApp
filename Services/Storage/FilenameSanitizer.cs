using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace MailPullerApp.Services.Storage
{
    // Zajišťuje, že název souboru neobsahuje neplatné znaky pro souborový systém
    internal static class FilenameSanitizer
    {
        // Seznam rezervovaných znaků pro windows
        private static readonly string[] reservedNames =
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2",
            "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        // vytvori bezpecny nazev slozky
        public static string SanitizeFolderName(string? inputName, int maxLength = 80)
        {
            var outputName = RemoveDiacritics(inputName);
            outputName = RemoveInvalidFileNameChars(outputName);
            outputName = CollapseSpaces(outputName);
            outputName = TrimDotsAndSpaces(outputName);
            outputName = PreventReservedName(outputName);
            outputName = EnsureNotEmpty(outputName);
            outputName = Truncate(outputName, maxLength);
            return outputName;
                
        }
        // vytvori bezpecny nazev souboru
        public static string SanitizeFileName(string? inputName, int maxLength = 120)
        {
            var s = RemoveDiacritics(inputName);
            var ext = Path.GetExtension(s);
            var baseName = Path.GetFileNameWithoutExtension(s);

            baseName = RemoveInvalidFileNameChars(baseName);
            baseName = CollapseSpaces(baseName);
            baseName = TrimDotsAndSpaces(baseName);
            baseName = PreventReservedName(baseName);
            baseName = EnsureNotEmpty(baseName);

            var allowed = Math.Max(1, maxLength - ext.Length);
            baseName = Truncate(baseName, allowed);

            var result = baseName + ext;
            return TrimDotsAndSpaces(result);
        }
        // -- Pomocne metody -- //

        // Odstraneni diakritiky
        private static string RemoveDiacritics(string? inputName)
        {
            var s = inputName?.Trim() ?? string.Empty;
            var formD = s.Normalize(NormalizationForm.FormD);   
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        // Odstraneni neplatnych znaku
        private static string RemoveInvalidFileNameChars(string inputName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(inputName.Length);
            foreach (var ch in inputName)
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            return sb.ToString();
        }
        // Odstranění nadbytečných mezer
        private static string CollapseSpaces(string inputName)
        {
            var parts = inputName.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        // Odstrani mezery a tecky na zacatku a konci
        private static string TrimDotsAndSpaces(string input) => input.Trim(' ', '.');

        // Zabrani pouziti rezervovanych jmen
        private static string PreventReservedName(string input)
        {
            foreach (var r in reservedNames)
                if (string.Equals(input, r, StringComparison.OrdinalIgnoreCase))
                    return input + "_";
            return input;
        }

        // Zajisti, ze neni prazdny
        private static string EnsureNotEmpty(string input)
            => string.IsNullOrWhiteSpace(input) ? "(bez predmetu)" : input;

        // Zkracuje retezec na max delku
        private static string Truncate(string input, int maxLength)
            => input.Length <= maxLength ? input : input.Substring(0, maxLength);

    }
}
