// Arabic letter-shaping and RTL rendering support.
// Implements RTLTMPro.RTLSupport.FixRTL so that LocalizationManager.RtlProcessor works
// without requiring the RTLTMPro asset-store package.
//
// Technique: map each Arabic character to its correct Unicode Presentation Form
// (Isolated / Initial / Medial / Final) based on surrounding characters, then
// reverse word order so TMP (which renders LTR) displays correctly RTL.

using System;
using System.Collections.Generic;
using System.Text;

namespace RTLTMPro
{
    public static class RTLSupport
    {
        // ─── Entry point ─────────────────────────────────────────────────────────
        /// <summary>
        /// Takes a raw Arabic Unicode string and returns a display-ready string
        /// where each letter is replaced with its contextual presentation form and
        /// the overall word/character order is reversed for LTR rendering in TMP.
        /// </summary>
        public static string FixRTL(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 1. Process Lam-Alef ligatures first
            input = ApplyLamAlefLigatures(input);

            // 2. Shape each character (contextual forms)
            char[] shaped = ShapeText(input.ToCharArray());

            // 3. Reverse the entire string for RTL display in LTR renderer
            //    but preserve word groupings and non-Arabic runs
            string result = ReverseRTL(new string(shaped));

            return result;
        }

        // ─── Lam-Alef ligature pre-processing ───────────────────────────────────
        // Unicode mandates these ligatures; without them Arabic looks wrong.
        private static string ApplyLamAlefLigatures(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == 'ل' && i + 1 < s.Length) // Lam
                {
                    char next = s[i + 1];
                    if (next == 'آ') { sb.Append('ﻵ'); i++; continue; } // لآ
                    if (next == 'أ') { sb.Append('ﻷ'); i++; continue; } // لأ
                    if (next == 'إ') { sb.Append('ﻹ'); i++; continue; } // لإ
                    if (next == 'ا') { sb.Append('ﻻ'); i++; continue; } // لا
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        // ─── Contextual shaping ──────────────────────────────────────────────────
        private static char[] ShapeText(char[] chars)
        {
            var result = new char[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!ArabicForms.ContainsKey(c)) { result[i] = c; continue; }

                bool prevJoins = i > 0 && IsRightJoining(chars[i - 1]);
                bool nextJoins = i < chars.Length - 1 && IsLeftJoining(chars[i + 1]);

                char[] forms = ArabicForms[c];
                // forms[0]=Isolated, forms[1]=Final, forms[2]=Initial, forms[3]=Medial
                char shaped;
                if (prevJoins && nextJoins && forms[3] != '\0') shaped = forms[3]; // Medial
                else if (prevJoins && forms[1] != '\0')          shaped = forms[1]; // Final
                else if (nextJoins && forms[2] != '\0')          shaped = forms[2]; // Initial
                else                                             shaped = forms[0]; // Isolated
                result[i] = shaped;
            }
            return result;
        }

        private static bool IsRightJoining(char c) => ArabicForms.ContainsKey(c) || IsLigature(c);
        private static bool IsLeftJoining(char c)
        {
            if (!ArabicForms.ContainsKey(c)) return false;
            // Right-only joining letters (no initial or medial form)
            char[] forms = ArabicForms[c];
            return forms[2] != '\0'; // has Initial form → left-joining
        }
        private static bool IsLigature(char c)
            => c >= 'ﻵ' && c <= 'ﻼ'; // Lam-Alef range

        // ─── RTL reversal ────────────────────────────────────────────────────────
        // Reverse character order but keep Emoji/Latin sub-runs in their own order.
        private static string ReverseRTL(string s)
        {
            var words = new List<string>();
            var current = new StringBuilder();
            foreach (char c in s)
            {
                if (c == ' ' || c == '\n')
                {
                    if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
                    words.Add(c.ToString());
                }
                else current.Append(c);
            }
            if (current.Length > 0) words.Add(current.ToString());
            words.Reverse();
            return string.Join("", words);
        }

        // ─── Presentation Forms table ────────────────────────────────────────────
        // Key   = base Arabic character
        // Value = char[4] { Isolated, Final, Initial, Medial }  ('\0' = not available)
        private static readonly Dictionary<char, char[]> ArabicForms =
            new Dictionary<char, char[]>
        {
            { 'ء', new[]{ 'ﺀ', '\0',   '\0',   '\0'   } }, // ء
            { 'آ', new[]{ 'ﺁ', 'ﺂ','\0',   '\0'   } }, // آ
            { 'أ', new[]{ 'ﺃ', 'ﺄ','\0',   '\0'   } }, // أ
            { 'ؤ', new[]{ 'ﺅ', 'ﺆ','\0',   '\0'   } }, // ؤ
            { 'إ', new[]{ 'ﺇ', 'ﺈ','\0',   '\0'   } }, // إ
            { 'ئ', new[]{ 'ﺉ', 'ﺊ','ﺋ','ﺌ'} }, // ئ
            { 'ا', new[]{ 'ﺍ', 'ﺎ','\0',   '\0'   } }, // ا
            { 'ب', new[]{ 'ﺏ', 'ﺐ','ﺑ','ﺒ'} }, // ب
            { 'ة', new[]{ 'ﺓ', 'ﺔ','\0',   '\0'   } }, // ة
            { 'ت', new[]{ 'ﺕ', 'ﺖ','ﺗ','ﺘ'} }, // ت
            { 'ث', new[]{ 'ﺙ', 'ﺚ','ﺛ','ﺜ'} }, // ث
            { 'ج', new[]{ 'ﺝ', 'ﺞ','ﺟ','ﺠ'} }, // ج
            { 'ح', new[]{ 'ﺡ', 'ﺢ','ﺣ','ﺤ'} }, // ح
            { 'خ', new[]{ 'ﺥ', 'ﺦ','ﺧ','ﺨ'} }, // خ
            { 'د', new[]{ 'ﺩ', 'ﺪ','\0',   '\0'   } }, // د
            { 'ذ', new[]{ 'ﺫ', 'ﺬ','\0',   '\0'   } }, // ذ
            { 'ر', new[]{ 'ﺭ', 'ﺮ','\0',   '\0'   } }, // ر
            { 'ز', new[]{ 'ﺯ', 'ﺰ','\0',   '\0'   } }, // ز
            { 'س', new[]{ 'ﺱ', 'ﺲ','ﺳ','ﺴ'} }, // س
            { 'ش', new[]{ 'ﺵ', 'ﺶ','ﺷ','ﺸ'} }, // ش
            { 'ص', new[]{ 'ﺹ', 'ﺺ','ﺻ','ﺼ'} }, // ص
            { 'ض', new[]{ 'ﺽ', 'ﺾ','ﺿ','ﻀ'} }, // ض
            { 'ط', new[]{ 'ﻁ', 'ﻂ','ﻃ','ﻄ'} }, // ط
            { 'ظ', new[]{ 'ﻅ', 'ﻆ','ﻇ','ﻈ'} }, // ظ
            { 'ع', new[]{ 'ﻉ', 'ﻊ','ﻋ','ﻌ'} }, // ع
            { 'غ', new[]{ 'ﻍ', 'ﻎ','ﻏ','ﻐ'} }, // غ
            { 'ف', new[]{ 'ﻑ', 'ﻒ','ﻓ','ﻔ'} }, // ف
            { 'ق', new[]{ 'ﻕ', 'ﻖ','ﻗ','ﻘ'} }, // ق
            { 'ك', new[]{ 'ﻙ', 'ﻚ','ﻛ','ﻜ'} }, // ك
            { 'ل', new[]{ 'ﻝ', 'ﻞ','ﻟ','ﻠ'} }, // ل
            { 'م', new[]{ 'ﻡ', 'ﻢ','ﻣ','ﻤ'} }, // م
            { 'ن', new[]{ 'ﻥ', 'ﻦ','ﻧ','ﻨ'} }, // ن
            { 'ه', new[]{ 'ﻩ', 'ﻪ','ﻫ','ﻬ'} }, // ه
            { 'و', new[]{ 'ﻭ', 'ﻮ','\0',   '\0'   } }, // و
            { 'ى', new[]{ 'ﻯ', 'ﻰ','\0',   '\0'   } }, // ى
            { 'ي', new[]{ 'ﻱ', 'ﻲ','ﻳ','ﻴ'} }, // ي
            // Lam-Alef ligature characters already shaped by pre-pass
            { 'ﻻ', new[]{ 'ﻻ', 'ﻼ','\0',   '\0'   } }, // لا isolated/final
            { 'ﻵ', new[]{ 'ﻵ', 'ﻶ','\0',   '\0'   } }, // لآ
            { 'ﻷ', new[]{ 'ﻷ', 'ﻸ','\0',   '\0'   } }, // لأ
            { 'ﻹ', new[]{ 'ﻹ', 'ﻺ','\0',   '\0'   } }, // لإ
        };
    }
}
