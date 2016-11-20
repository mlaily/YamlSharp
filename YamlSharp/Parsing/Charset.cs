using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YamlSharp.Parsing
{
    /// <summary>
    /// Character matching rules.
    /// The methods in this class are static and will never consume any character (increment P)
    /// </summary>
    internal static class Charset
    {
        #region Only have to match 16 bits or less code points
        public static bool cByteOrdermark(string text, int p) // [3] 
        {
            char c = text[p];
            return c == '\uFEFF';
        }
        public static bool cIndicator(string text, int p) // [22]
        {
            //return c < 0x100 &&
            //     "-?:,[]{}#&*!|>'\"%@`".Contains(c);
            char c = text[p];
            switch (c)
            {
                case '-':
                case '?':
                case ':':
                case ',':
                case '[':
                case ']':
                case '{':
                case '}':
                case '#':
                case '&':
                case '*':
                case '!':
                case '|':
                case '>':
                case '\'':
                case '\"':
                case '%':
                case '@':
                case '`':
                    return true;
                default:
                    return false;
            }
        }
        public static bool cFlowIndicator(string text, int p) // [23]
        {
            //return c < 0x100 &&
            //        ",[]{}".Contains(c);
            char c = text[p];
            switch (c)
            {
                case ',':
                case '[':
                case ']':
                case '{':
                case '}':
                    return true;
                default:
                    return false;
            }
        }
        public static bool nsDecDigit(string text, int p)
        {
            char c = text[p];
            return ('0' <= c && c <= '9');
        }
        public static bool nsHexDigit(string text, int p)
        {
            char c = text[p];
            return (('0' <= c && c <= '9') // nsDecDigit
                || ('A' <= c && c <= 'F')
                || ('a' <= c && c <= 'f'));
        }
        public static bool nsAsciiLetter(string text, int p)
        {
            char c = text[p];
            return (('A' <= c && c <= 'Z')
                || ('a' <= c && c <= 'z'));
        }
        public static bool nsWordChar(string text, int p)
        {
            char c = text[p];
            return (('0' <= c && c <= '9')  // nsDecDigit
                || ('A' <= c && c <= 'Z')   // nsAsciiLetter
                || ('a' <= c && c <= 'z')   // nsAsciiLetter
                || c == '-');
        }
        public static bool sSpace(string text, int p)
        {
            char c = text[p];
            return c == ' ';
        }
        public static bool sWhite(string text, int p)
        {
            char c = text[p];
            return c == ' ' || c == '\t';
        }
        public static bool nsUriCharSub(string text, int p)
        {
            char c = text[p];
            if (('0' <= c && c <= '9')    // nsWordChar
                || ('A' <= c && c <= 'Z') // nsWordChar
                || ('a' <= c && c <= 'z') // nsWordChar
                || c == '-')              // nsWordChar
            {
                return true;
            }
            else
            {
                switch (c)
                {
                    case '#':
                    case ';':
                    case '/':
                    case '?':
                    case ':':
                    case '@':
                    case '&':
                    case '=':
                    case '$':
                    case ',':
                    case '_':
                    case '.':
                    case '!':
                    case '~':
                    case '*':
                    case '\'':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                        return true;
                    default:
                        return false;
                }
            }
        }
        public static bool nsTagCharSub(string text, int p)
        {
            char c = text[p];
            return nsUriCharSub(text, p) && !(c == '!' || cFlowIndicator(text, p));
        }
        public static bool bChar(string text, int p)
        {
            char c = text[p];
            return c == '\n' || c == '\r';
        }
        public static bool nbCharWithWarning(string text, int p)
        {
            char c = text[p];
            return c == 0x2029 ||  // paragraph separator
                   c == 0x2028 ||  // line separator
                   c == 0x85 ||    // next line
                   c == 0x0C;      // form feed
        }
        #endregion

        #region Have to match 32 bits code points
        private static bool IsHighSurrogate(char c) => 0xD800 <= c && c <= 0xDBFF;
        private static bool IsLowSurrogate(char c) => 0xDC00 <= c && c <= 0xDFFF;
        public static bool nbJson(string text, int p, out int length) // [2] 
        {
            char c = text[p];
            // Basic Multilingual Plane, minus chars < 0x20. (control chars)
            // (Surrogate code values are in the range U+D800 through U+DFFF and are matched below.)
            if ((0x20 <= c && c <= 0xD7FF)
                || c == 0x09 // tab
                || (0xE000 <= c && c <= 0xFFFF))
            {
                length = 1;
                return true;
            }
            // High surrogate
            else if (IsHighSurrogate(c))
            {
                // Try to match a low surrogate following the high surrogate
                char c2 = text[p + 1];
                if (IsLowSurrogate(c2))
                {
                    // Found a 32 bits code point
                    length = 2;
                    return true;
                }
                else
                {
                    // We didn't found a low surrogate following our high surrogate.
                    // Json seems to allow a high surrogate code point not followed by a low surrogate though,
                    // even if the unicode standard says this is not supposed to happen.
                    length = 1;
                    return true;
                }
            }
            else if (IsLowSurrogate(c))
            {
                // Low surrogate occuring first. Json seems to allow this,
                // but the unicode standard says this is not supposed to happen. Whatever... 
                length = 1;
                return true;
            }

            length = 0;
            return false;
        }
        //public static bool cPrintable(char c) // [1] 
        //{
        //    return
        //        /*  ( 0x10000 < c && c < 0x110000 ) || */
        //        (0xe000 <= c && c <= 0xfffd) ||
        //        (0xa0 <= c && c <= 0xd7ff) ||
        //        c == 0x85 ||
        //        (0x20 <= c && c <= 0x7e) ||
        //        c == 0x0d ||
        //        c == 0x0a ||
        //        c == 0x09;
        //}
        public static bool nbChar(string text, int p, out int length)
        {
            char c = text[p];
            // (Surrogate code values are in the range U+D800 through U+DFFF.)
            if ((0x20 <= c && c <= 0x7E)
                || (0xA0 <= c && c <= 0xD7FF)
                || (0xE000 <= c && c <= 0xFFFD && c != 0xFEFF /* - c_byte_order_mark */)
                || c == 0x85
                // || c == 0x0A // - b_char
                // || c == 0x0D // - b_char
                || c == 0x09)
            {
                length = 1;
                return true;
            }
            // 32 bits (U+10000 to U+10FFFF):
            // High surrogate ...
            else if (IsHighSurrogate(c))
            {
                char c2 = text[p + 1];
                // ...followed by low surrogate 
                if (IsLowSurrogate(c2))
                {
                    length = 2;
                    return true;
                }
            }

            length = 0;
            return false;
        }
        private static bool nsChar8And16BitsOny(char c)
        {
            // (Surrogate code values are in the range U+D800 through U+DFFF.)
            return (0x21 /* - s_white */ <= c && c <= 0x7E)
                || (0xA0 <= c && c <= 0xD7FF)
                || (0xE000 <= c && c <= 0xFFFD && c != 0xFEFF /* - c_byte_order_mark */)
                || c == 0x85;
            //  || c == 0x0A // - b_char
            //  || c == 0x0D // - b_char
            //  || c == 0x09 // - s_white
        }
        public static bool nsChar(string text, int p, out int length)
        {
            char c = text[p];
            if (nsChar8And16BitsOny(c))
            {
                length = 1;
                return true;
            }
            // 32 bits (U+10000 to U+10FFFF):
            // High surrogate ...
            else if (IsHighSurrogate(c))
            {
                char c2 = text[p + 1];
                // ...followed by low surrogate 
                if (IsLowSurrogate(c2))
                {
                    length = 2;
                    return true;
                }
            }

            length = 0;
            return false;
        }
        public static bool nsAnchorChar(string text, int p, out int length)
        {
            length = 0;
            char c = text[p];
            return !cFlowIndicator(text, p) && nsChar(text, p, out length);
        }
        public static bool nsPlainSafeIn(string text, int p, out int length)
        {
            length = 0;
            char c = text[p];
            return !cFlowIndicator(text, p) && nsChar(text, p, out length);
        }
        public static bool nsPlainSafeOut(string text, int p, out int length)
        {
            char c = text[p];
            return nsChar(text, p, out length);
        }
        public static bool nsPlainFirstSub(string text, int p, out int length)
        {
            length = 0;
            char c = text[p];
            return !cIndicator(text, p) && nsChar(text, p, out length);
        }
        public static bool nsPlainSafe(string text, int p, out int length, YamlContext c) // [127] 
        {
            switch (c)
            {
                case YamlContext.FlowOut:
                case YamlContext.BlockKey:
                    return nsPlainSafeOut(text, p, out length);
                case YamlContext.FlowIn:
                case YamlContext.FlowKey:
                    return nsPlainSafeIn(text, p, out length);
                default:
                    throw new NotImplementedException();
            }
        }
        public static bool PrecedingIsNsChar(string text, int p)
        {
            if (p == 0)
                return false;

            char c = text[p - 1];
            if (nsChar8And16BitsOny(c))
            {
                return true;
            }
            else
            {
                // We also have to try to match a 32 bits code point
                if (p < 2)
                {
                    return false;
                }
                else
                {
                    char cHigh = text[p - 2];
                    return IsHighSurrogate(cHigh) && IsLowSurrogate(c);
                }
            }
        }
        #endregion
    }
}
