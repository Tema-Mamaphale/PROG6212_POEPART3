using System.Collections.Generic;
using System.IO;

namespace ClaimSystem.Services
{
    public static class DocumentRules
    {
        private static readonly HashSet<string> AllowedExt =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".xlsx" };

        public const long MaxSizeBytes = 10 * 1024 * 1024; 

        public static bool IsAllowed(string fileName)
            => AllowedExt.Contains(Path.GetExtension(fileName));

        public static bool IsTooLarge(long length)
            => length > MaxSizeBytes;
    }
}
