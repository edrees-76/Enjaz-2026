using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enjaz.Helpers
{
    public static class ArabicTextShaper
    {
        private static readonly Dictionary<char, char[]> Forms = new()
        {
            { '\u0621', new[] { '\uFE80', '\uFE80', '\uFE80', '\uFE80' } }, 
            { '\u0622', new[] { '\uFE81', '\uFE82', '\uFE81', '\uFE82' } }, 
            { '\u0623', new[] { '\uFE83', '\uFE84', '\uFE83', '\uFE84' } }, 
            { '\u0624', new[] { '\uFE85', '\uFE86', '\uFE85', '\uFE86' } }, 
            { '\u0625', new[] { '\uFE87', '\uFE88', '\uFE87', '\uFE88' } }, 
            { '\u0626', new[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' } }, 
            { '\u0627', new[] { '\uFE8D', '\uFE8E', '\uFE8D', '\uFE8E' } }, 
            { '\u0628', new[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' } }, 
            { '\u0629', new[] { '\uFE93', '\uFE94', '\uFE93', '\uFE94' } }, 
            { '\u062A', new[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' } }, 
            { '\u062B', new[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' } }, 
            { '\u062C', new[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' } }, 
            { '\u062D', new[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' } }, 
            { '\u062E', new[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' } }, 
            { '\u062F', new[] { '\uFEA9', '\uFEAA', '\uFEA9', '\uFEAA' } }, 
            { '\u0630', new[] { '\uFEAB', '\uFEAC', '\uFEAB', '\uFEAC' } }, 
            { '\u0631', new[] { '\uFEAD', '\uFEAE', '\uFEAD', '\uFEAE' } }, 
            { '\u0632', new[] { '\uFEAF', '\uFEB0', '\uFEAF', '\uFEB0' } }, 
            { '\u0633', new[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' } }, 
            { '\u0634', new[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' } }, 
            { '\u0635', new[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' } }, 
            { '\u0636', new[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' } }, 
            { '\u0637', new[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' } }, 
            { '\u0638', new[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' } }, 
            { '\u0639', new[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' } }, 
            { '\u063A', new[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' } }, 
            { '\u0641', new[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' } }, 
            { '\u0642', new[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' } }, 
            { '\u0643', new[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' } }, 
            { '\u0644', new[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' } }, 
            { '\u0645', new[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' } }, 
            { '\u0646', new[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' } }, 
            { '\u0647', new[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' } }, 
            { '\u0648', new[] { '\uFEED', '\uFEEE', '\uFEED', '\uFEEE' } }, 
            { '\u0649', new[] { '\uFEEF', '\uFEF0', '\uFEEF', '\uFEF0' } }, 
            { '\u064A', new[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' } },
        };

        private static readonly HashSet<char> RightJoinOnly = new()
        {
            '\u0622', '\u0623', '\u0624', '\u0625', '\u0627', '\u062F', '\u0630', '\u0631', '\u0632', '\u0648', '\u0629', '\u0649', '\u0621'
        };

        public static bool IsArabic(char c) => 
            (c >= '\u0600' && c <= '\u06FF') || 
            (c >= '\u0750' && c <= '\u077F') ||
            (c >= '\u08A0' && c <= '\u08FF') ||
            (c >= '\uFE70' && c <= '\uFEFF') || 
            (c >= '\uFB50' && c <= '\uFDFF');

        private static bool CanConnectLeft(char c) => IsArabic(c) && Forms.ContainsKey(c) && !RightJoinOnly.Contains(c);
        private static bool CanConnectRight(char c) => IsArabic(c) && Forms.ContainsKey(c);

        private static string GetCharType(char c)
        {
            if (IsArabic(c)) return "AR";
            if (char.IsDigit(c)) return "NUM";
            if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) return "LAT";
            return "SYM";
        }

        private static List<(string text, string type)> SegmentText(string input)
        {
            var segments = new List<(string, string)>();
            if (string.IsNullOrEmpty(input)) return segments;

            var current = new StringBuilder();
            string currentType = GetCharType(input[0]);

            foreach (var c in input)
            {
                var type = GetCharType(c);
                if (type != currentType)
                {
                    segments.Add((current.ToString(), currentType));
                    current.Clear();
                    currentType = type;
                }
                current.Append(c);
            }
            if (current.Length > 0) segments.Add((current.ToString(), currentType));
            return segments;
        }

        private static string ApplyShaping(string text)
        {
            var shapedText = new StringBuilder();
            var chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!Forms.ContainsKey(c))
                {
                    // Handle Lam-Alef ligature
                    if (c == '\u0644' && i + 1 < chars.Length)
                    {
                        char next = chars[i + 1];
                        char? ligature = next switch
                        {
                            '\u0622' => i > 0 && CanConnectLeft(chars[i - 1]) ? '\uFEF6' : '\uFEF5',
                            '\u0623' => i > 0 && CanConnectLeft(chars[i - 1]) ? '\uFEF8' : '\uFEF7',
                            '\u0625' => i > 0 && CanConnectLeft(chars[i - 1]) ? '\uFEFA' : '\uFEF9',
                            '\u0627' => i > 0 && CanConnectLeft(chars[i - 1]) ? '\uFEFC' : '\uFEFB',
                            _ => null
                        };
                        if (ligature.HasValue) { shapedText.Append(ligature.Value); i++; continue; }
                    }
                    shapedText.Append(c);
                    continue;
                }

                bool hasPrev = i > 0 && CanConnectLeft(chars[i - 1]);
                bool hasNext = i + 1 < chars.Length && CanConnectRight(chars[i + 1]);

                var forms = Forms[c];
                if (hasPrev && hasNext) shapedText.Append(forms[3]); // Medial
                else if (hasPrev) shapedText.Append(forms[1]); // Final
                else if (hasNext) shapedText.Append(forms[2]); // Initial
                else shapedText.Append(forms[0]); // Isolated
            }
            return shapedText.ToString();
        }

        public static string Shape(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var segments = SegmentText(text);

            var result = new StringBuilder();
            foreach (var segment in segments)
            {
                if (segment.type == "AR")
                {
                    string shaped = ApplyShaping(segment.text);
                    // Reversing for Visual Order (needed for SkiaSharp/LiveCharts)
                    char[] arr = shaped.ToCharArray();
                    Array.Reverse(arr);
                    result.Append(new string(arr));
                }
                else
                {
                    result.Append(segment.text);
                }
            }

            return result.ToString();
        }

        public static string ShapeMixed(string text) => Shape(text);
    }
}
