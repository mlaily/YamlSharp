using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;
using System.Diagnostics;
using YamlSharp.Model;

namespace YamlSharp.Parsing
{
    /// <summary>
    /// <para>A text parser for<br/>
    /// YAML Ain’t Markup Language (YAML™) Version 1.2<br/>
    /// 3rd Edition (2009-10-01)<br/>
    /// http://yaml.org/spec/1.2/spec.html </para>
    /// 
    /// <para>This class parse a YAML document and compose representing <see cref="YamlNode"/> graph.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// string yaml = LoadYamlSource();
    /// YamlParser parser = new YamlParser();
    /// Node[] result = null;
    /// try {
    ///     result = parser.Parse(yaml);
    ///     ...
    ///     // you can reuse parser as many times you want
    ///     ...
    ///     
    /// } catch( ParseErrorException e ) {
    ///     MessageBox.Show(e.Message);
    /// }
    /// if(result != null) {
    ///     ...
    /// 
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>Currently, this parser violates the YAML 1.2 specification in the following points.</para>
    /// <para>- line breaks are not normalized.</para>
    /// <para>- omission of the final line break is allowed in plain / literal / folded text.</para>
    /// <para>- ':' followed by ns-indicator is excluded from ns-plain-char.</para>
    /// </remarks>
    internal class YamlParser : Parser<YamlParser.ParserState>
    {
        /// <summary>
        /// Initialize a YAML parser.
        /// </summary>
        public YamlParser()
        {
            Anchors = new AnchorDictionary(Error);
            TagPrefixes = new YamlTagPrefixes(Error);
            Warnings = new List<string>();
        }

        private YamlConfig config;
        private List<YamlNode> ParseResult = new List<YamlNode>();
        /// <summary>
        /// Parse YAML text and returns a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text to be parsed.</param>
        /// <returns>A list of <see cref="YamlNode"/> parsed from the given text</returns>
        public List<YamlNode> Parse(string yaml)
        {
            return Parse(yaml, YamlNode.DefaultConfig);
        }
        /// <summary>
        /// Parse YAML text and returns a list of <see cref="YamlNode"/>.
        /// </summary>
        /// <param name="yaml">YAML text to be parsed.</param>
        /// <param name="config"><see cref="YamlConfig">YAML Configuration</see> to be used in parsing.</param>
        /// <returns>A list of <see cref="YamlNode"/> parsed from the given text</returns>
        public List<YamlNode> Parse(string yaml, YamlConfig config)
        {
            this.config = config;
            Warnings.Clear();
            ParseResult.Clear();
            AlreadyWarnedChars.Clear();
            if (base.Parse(lYamlStream, yaml + "\0")) // '\0' = guard char
                return ParseResult;
            return new List<YamlNode>();
        }

        internal bool IsValidPlainText(string plain, YamlConfig config)
        {
            this.config = config;
            Warnings.Clear();
            ParseResult.Clear();
            AlreadyWarnedChars.Clear();
            return base.Parse(() => nsPlain(0, YamlContext.BlockKey) && EndOfFile(), plain + "\0"); // '\0' = guard char
        }

        #region Warnings
        /// <summary>
        /// Warnings that are made while parsing a YAML text.
        /// This property is cleared by new call for <see cref="Parse(string)"/> method.
        /// </summary>
        public List<string> Warnings { get; private set; }
        private Dictionary<string, bool> WarningAdded = new Dictionary<string, bool>();
        /// <summary>
        /// Add message in <see cref="Warnings"/> property.
        /// </summary>
        /// <param name="message"></param>
        protected override void StoreWarning(string message)
        {
            // Warnings will not be rewound.
            // We have to avoid same warnings from being repeatedly reported.
            if (!WarningAdded.ContainsKey(message))
            {
                Warnings.Add(message);
                WarningAdded[message] = true;
            }
        }

        /// <summary>
        /// Invoked when unknown directive is found in YAML document.
        /// </summary>
        /// <param name="name">Name of the directive</param>
        /// <param name="args">Parameters for the directive</param>
        protected virtual void ReservedDirective(string name, params string[] args)
        {
            Warning($"Custom directive %{name} was ignored");
        }
        /// <summary>
        /// Invoked when YAML directive is found in YAML document.
        /// </summary>
        /// <param name="version">Given version</param>
        protected virtual void YamlDirective(string version)
        {
            if (version != "1.2")
                Warning($"YAML version %{version} was specified but ignored");
        }
        Dictionary<char, bool> AlreadyWarnedChars = new Dictionary<char, bool>();
        private void WarnIfCharWasBreakInYAML1_1()
        {
            if (Parsing.Charset.nbCharWithWarning(Text, P) && !AlreadyWarnedChars.ContainsKey(Text[P]))
            {
                var charValue = Text[P] < 0x100 ? $"\\x{(int)Text[P]:x2}" : $"\\u{(int)Text[P]:x4}";
                Warning($"{charValue} is treated as non-break character unlike YAML 1.1");
                AlreadyWarnedChars.Add(Text[P], true);
            }
        }
        #endregion

        #region Debug.Assert
#if DEBUG
        /// <summary>
        /// Since System.Diagnostics.Debug.Assert is too anoying while development,
        /// this class temporarily override Debug.Assert action.
        /// </summary>
        private class Debug
        {
            public static void Assert(bool condition)
            {
                Assert(condition, "");
            }
            public static void Assert(bool condition, string message)
            {
                if (!condition)
                    throw new Exception("assertion failed: " + message);
            }
        }
#endif
        #endregion

        #region Status / Value
        /// <summary>
        /// additional fields to be rewound
        /// </summary>
        public struct ParserState
        {
            /// <summary>
            /// tag for the next value (will be cleared when the next value is created)
            /// </summary>
            public string Tag;
            /// <summary>
            /// anchor for the next value (will be cleared when the next value is created)
            /// </summary>
            public string Anchor;
            /// <summary>
            /// current value
            /// </summary>
            public YamlNode Value;
            /// <summary>
            /// anchor rewinding position
            /// </summary>
            public int AnchorDepth;
        }

        /// <summary>
        /// rewinding action
        /// </summary>
        protected override void Rewind()
        {
            Anchors.RewindDepth = base.State.AnchorDepth;
        }

        private bool SetValue(YamlNode v)
        {
            if (base.State.Value != null && v != null)
                throw new Exception();
            base.State.Value = v;
            v.OnLoaded();
            return true;
        }
        private YamlNode GetValue()
        {
            var v = base.State.Value;
            base.State.Value = null;
            return v;
        }
        #endregion

        private YamlTagPrefixes TagPrefixes;

        /// <summary>
        /// set status.tag with tag resolution
        /// </summary>
        /// <param name="tagHandle"></param>
        /// <param name="tagSuffix"></param>
        /// <returns></returns>
        private bool SetTag(string tagHandle, string tagSuffix)
        {
            return SetTag(TagPrefixes.Resolve(tagHandle, tagSuffix));
        }
        /// <summary>
        /// set status.tag with verbatim tag value
        /// </summary>
        /// <param name="verbatimTag">verbatim tag</param>
        /// <returns></returns>
        private bool SetTag(string verbatimTag)
        {
            Debug.Assert(verbatimTag != "");
            // validate tag
            if (verbatimTag.StartsWith("!", StringComparison.Ordinal))
            {
                if (verbatimTag == "!")
                    Error("Empty local tag was found.");
            }
            else
            {
                if (!TagValidator.IsValid(verbatimTag))
                    Warning($"Invalid global tag name '{verbatimTag}' (c.f. RFC 4151) found");
            }
            base.State.Tag = verbatimTag;
            return true;
        }
        private YamlTagValidator TagValidator = new YamlTagValidator();

        private AnchorDictionary Anchors;
        private void RegisterAnchorFor(YamlNode value)
        {
            if (base.State.Anchor != null)
            {
                Anchors.Add(base.State.Anchor, value);
                base.State.Anchor = null;
                base.State.AnchorDepth = Anchors.RewindDepth;
            }
        }

        /// <summary>
        /// Used when the parser resolves a tag for a scalar node from its value.
        /// 
        /// New resolution rules can be add before calling <see cref="Parse(string)"/> method.
        /// </summary>
        private void AutoDetectTag(string fromStyle)
        {
            if (fromStyle != null)
                fromStyle = YamlNode.ExpandTag(fromStyle);

            if (base.State.Tag != null)
                return;

            if (fromStyle == null)
                fromStyle = config.TagResolver.Resolve(StringValue.ToString());

            if (fromStyle != null)
                base.State.Tag = fromStyle;
            return;
        }
        private YamlScalar CreateScalar(string autoDetectedTag, Position pos)
        {
            AutoDetectTag(autoDetectedTag);
            if (base.State.Tag == null || base.State.Tag == "" /* ! was specified */ )
                base.State.Tag = YamlNode.DefaultTagPrefix + "str";
            var value = new YamlScalar(base.State.Tag, StringValue.ToString());
            value.Raw = pos.Raw;
            value.Column = pos.Column;
            StringValue.Length = 0;
            RegisterAnchorFor(value);
            base.State.Tag = null;
            return value;
        }
        private YamlSequence CreateSequence(Position pos)
        {
            if (base.State.Tag == null || base.State.Tag == "" /* ! was specified */ )
                base.State.Tag = YamlNode.DefaultTagPrefix + "seq";
            var seq = new YamlSequence();
            seq.Tag = base.State.Tag;
            seq.Raw = pos.Raw;
            seq.Column = pos.Column;
            RegisterAnchorFor(seq);
            base.State.Tag = null;
            return seq;
        }
        private YamlMapping CreateMapping(Position pos)
        {
            if (base.State.Tag == null || base.State.Tag == "" /* ! was specified */ )
                base.State.Tag = YamlNode.DefaultTagPrefix + "map";
            var map = new YamlMapping();
            map.Tag = base.State.Tag;
            map.Raw = pos.Raw;
            map.Column = pos.Column;
            RegisterAnchorFor(map);
            base.State.Tag = null;
            return map;
        }

        #region The BNF syntax for YAML 1.2

        #region Chapter 5. Character Set

        bool nbChar() // [27] 
        {
            WarnIfCharWasBreakInYAML1_1();
            int length = 0;
            if (Parsing.Charset.nbChar(Text, P, out length))
            {
                P += length;
                return true;
            }
            return false;
        }
        bool bBreak() // [28] 
        {   // \r\n? | \n 
            if (Text[P] == '\r')
            {
                P++;
                if (Text[P] == '\n')
                    P++;
                return true;
            }
            if (Text[P] == '\n')
            {
                P++;
                return true;
            }
            return false;
        }
        bool bAsLineFeed() // [29] 
        {
            if (config.NormalizeLineBreaks)
            {
                if (bBreak())
                {
                    StringValue.Append(config.LineBreakForInput);
                    return true;
                }
                return false;
            }
            else
            {
                return Save(() => bBreak(), s => StringValue.Append(s));
            }
        }
        bool bNonContent() // [30] 
        {
            return bBreak();
        }
        bool sWhite() // [33] 
        {
            if (Text[P] == ' ' || Text[P] == '\t')
            {
                P++;
                return true;
            }
            return false;
        }
        bool Repeat_sWhiteAsString()
        {
            var start = P;
            while (Parsing.Charset.sWhite(Text, P))
                StringValue.Append(Text[P++]);
            return true;
        }
        bool nsChar() // [34] 
        {
            WarnIfCharWasBreakInYAML1_1();
            int length = 0;
            if (Parsing.Charset.nsChar(Text, P, out length))
            {
                P += length;
                return true;
            }
            return false;
        }
        bool nsUriChar() // [39] 
        {
            if (Parsing.Charset.nsUriCharSub(Text, P))
            {
                StringValue.Append(Text[P++]);
                return true;
            }
            return nsUriEscapedChar();
        }
        bool nsUriEscapedChar()
        {
            if (Text[P] == '+')
            {
                StringValue.Append(' ');
                P++;
                return true;
            }
            if (Text[P] != '%')
                return false;
            // http://www.cresc.co.jp/tech/java/URLencoding/JavaScript_URLEncoding.htm
            int v1 = -1, v2 = -1, v3 = -1, v4 = -1;
            ErrorUnless(
                HexValue(P + 1, out v1) &&
                (v1 < 0x80 || (Text[P + 3] == '%' && HexValue(P + 4, out v2))) &&
                (v1 < 0xe0 || (Text[P + 6] == '%' && HexValue(P + 7, out v3))) &&
                (v1 < 0xf1 || (Text[P + 9] == '%' && HexValue(P + 10, out v4))),
                "Invalid URI escape."
                );
            if (v2 == -1)
            { // 1 byte code
                StringValue.Append((char)v1);
                P += 3;
                return true;
            }
            if (v3 == -1)
            {
                StringValue.Append((char)(((v1 & 0x1f) << 6) + (v2 & 0x7f)));
                P += 6;
                return true;
            }
            if (v4 == -1)
            {
                StringValue.Append((char)(((v1 & 0x0f) << 12) + ((v2 & 0x7f) << 6) + (v3 & 0x7f)));
                P += 9;
                return true;
            }
            StringValue.Append(char.ConvertFromUtf32((((v1 & 0x07) << 18) + ((v2 & 0x7f) << 12) + ((v3 & 0x7f) << 6) + (v4 & 0x7f))));
            P += 12;
            return true;
        }
        bool nsTagChar() // [40] 
        {
            if (Parsing.Charset.nsTagCharSub(Text, P))
            {
                StringValue.Append(Text[P++]);
                return true;
            }
            return nsUriEscapedChar();
        }
        bool c_nsEscChar() // [62] 
        {
            if (Text[P] != '\\')
                return false;

            const int escapeIndicatorLength = 2;

            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            int v4 = 0;
            switch (Text[P + 1])
            {
                case '0':
                    StringValue.Append('\0');
                    break;
                case 'a':
                    StringValue.Append('\a');
                    break;
                case 'b':
                    StringValue.Append('\b');
                    break;
                case 't':
                case '\t':
                    StringValue.Append('\t');
                    break;
                case 'n':
                    StringValue.Append('\n');
                    break;
                case 'v':
                    StringValue.Append('\v');
                    break;
                case 'f':
                    StringValue.Append('\f');
                    break;
                case 'r':
                    StringValue.Append('\r');
                    break;
                case 'e':
                    StringValue.Append('\x1b');
                    break;
                case ' ':
                    StringValue.Append(' ');
                    break;
                case '"':
                    StringValue.Append('"');
                    break;
                case '/':
                    StringValue.Append('/');
                    break;
                case '\\':
                    StringValue.Append('\\');
                    break;
                case 'N':
                    StringValue.Append('\x85');
                    break;
                case '_':
                    StringValue.Append('\xa0');
                    break;
                case 'L':
                    StringValue.Append('\u2028');
                    break;
                case 'P':
                    StringValue.Append('\u2029');
                    break;
                case 'x':
                    if (!HexValue(P + 2, out v1))
                        InvalidEscapeSequence(escapeIndicatorLength + 2);
                    StringValue.Append((char)v1);
                    P += 2;
                    break;
                case 'u':
                    if (!(HexValue(P + 2, out v1) && HexValue(P + 4, out v2)))
                        InvalidEscapeSequence(escapeIndicatorLength + 4);
                    StringValue.Append((char)((v1 << 8) + v2));
                    P += 4;
                    break;
                case 'U':
                    if (!(HexValue(P + 2, out v1) && HexValue(P + 4, out v2) && HexValue(P + 6, out v3) && HexValue(P + 8, out v4)))
                        InvalidEscapeSequence(escapeIndicatorLength + 8);
                    StringValue.Append(char.ConvertFromUtf32((v1 << 24) + (v2 << 16) + (v3 << 8) + v4));
                    P += 8;
                    break;
                default:
                    // escaped line break or error
                    if (Text[P + 1] != '\n' && Text[P + 1] != '\r')
                        InvalidEscapeSequence(escapeIndicatorLength);
                    return false;
            }
            P += escapeIndicatorLength;
            return true;
        }
        void InvalidEscapeSequence(int n)
        {
            // n chars from the current point should be reported by not acrossing " nor EOF
            // P is on the beginning of the escape sequence ('\')
            // TODO: check it still works as expected with unicode support
            var s = "";
            int length;
            for (int i = 0; i < n; i += length)
                if (Text[P + i] != '"' && Parsing.Charset.nbJson(Text, P + i, out length))
                {
                    s += Text.Substring(P + i, length);
                }
                else
                    break;
            Error($"{s} is not a valid escape sequence.");
        }
        bool HexValue(int p, out int v)
        {
            v = 0;
            if (Text.Length <= p + 1 || !Parsing.Charset.nsHexDigit(Text, p) || !Parsing.Charset.nsHexDigit(Text, p + 1))
                return false;
            v = (HexNibble(Text[p]) << 4) + HexNibble(Text[p + 1]);
            return true;
        }
        int HexNibble(char c)
        {
            if (c <= '9')
                return c - '0';
            if (c < 'Z')
                return c - 'A' + 10;
            return c - 'a' + 10;
        }
        #endregion

        #region Chapter 6. Basic Structures 
        #region 6.1 Indentation Spaces
        bool TabCharFoundForIndentation = false;
        bool sIndent(int n) // [63] 
        {
            TabCharFoundForIndentation = false;
            Debug.Assert(StartOfLine() || EndOfFile());
            for (int i = 0; i < n; i++)
                if (Text[P + i] != ' ')
                {
                    if (Text[P + i] == '\t')
                        TabCharFoundForIndentation = true;
                    return false;
                }
            P += n;
            return true;
        }
        bool sIndentLT(int n) // [64] 
        {
            Debug.Assert(StartOfLine() || EndOfFile());
            int i = 0;
            while (Parsing.Charset.sSpace(Text, P + i))
                i++;
            if (i < n)
            {
                P += i;
                return true;
            }
            return false;
        }
        bool sIndentLE(int n) // [65] 
        {
            return sIndentLT(n + 1);
        }
        bool sIndentCounted(int n, out int m) // [185, 187]
        {
            m = 0;
            while (n < 0 || Text[P] == ' ')
            {
                n++;
                P++;
                m++;
            }
            return m > 0;
        }
        #endregion
        #region 6.2 Separation Spaces
        private bool sSeparateInLine() // [66] 
        {
            return OneAndRepeat(Parsing.Charset.sWhite) || StartOfLine();
        }
        private bool StartOfLine() // [66, 79, 206]
        {   // TODO: how about "---" ?
            return P == 0 || Text[P - 1] == '\n' || Text[P - 1] == '\r' || Text[P - 1] == '\ufeff';
        }
        #endregion
        #region 6.3 Line Prefixes
        private bool sLinePrefix(int n, YamlContext c) // [67] 
        {
            switch (c)
            {
                case YamlContext.Folded:
                case YamlContext.BlockOut:
                case YamlContext.BlockIn:
                    return sBlockLinePrefix(n);
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return sFlowLinePrefix(n);
                default:
                    throw new NotImplementedException();
            }
        }
        private bool sBlockLinePrefix(int n) // [68] 
        {
            return sIndent(n);
        }
        bool sFlowLinePrefix(int n) // [69] 
        {
            return sIndent(n) && Optional(sSeparateInLine);
        }
        #endregion
        #region 6.4 Empty Lines
        private bool lEmpty(int n, YamlContext c) // [70] 
        {
            return
                RewindUnless(() => (sLinePrefix(n, c) || sIndentLT(n)) && bAsLineFeed());
        }
        #endregion
        #region 6.5 Line Folding
        private bool b_lTrimmed(int n, YamlContext c) // [71] 
        {
            return RewindUnless(() =>
                bNonContent() && OneAndRepeat(() => lEmpty(n, c))
                );
        }
        bool bAsSpace() // [72] 
        {
            return
                bBreak() &&
                Action(() => StringValue.Append(' '));
        }
        private bool b_lFolded(int n, YamlContext c) // [73] 
        {
            return b_lTrimmed(n, c) || bAsSpace();
        }
        private bool sFlowFolded(int n) // [74] 
        {
            return RewindUnless(() =>
                Optional(sSeparateInLine) &&
                b_lFolded(n, YamlContext.FlowIn) &&
                !cForbidden() &&
                sFlowLinePrefix(n)
            );
        }
        #endregion
        #region 6.6 Comments
        private bool c_nbCommentText() // [75] 
        {
            return Text[P] == '#' && Repeat(nbChar);
        }
        bool bComment() // [76] 
        {
            return bNonContent() || EndOfFile();
        }
        bool EndOfFile() // [76, 206]
        {
            return P == Text.Length - 1; // text[text.Length-1] == '\0' /* guard char */
        }
        bool s_bComment() // [77] 
        {
            return RewindUnless(() =>
                  Optional(sSeparateInLine() && Optional(c_nbCommentText)) &&
                bComment()
            );
        }
        bool lComment() // [78] 
        {
            return RewindUnless(() =>
                sSeparateInLine() &&
                Optional(c_nbCommentText) &&
                bComment()
                );

        }
        bool s_lComments() // [79] 
        {
            return (s_bComment() || StartOfLine()) && Repeat(lComment);
        }
        #endregion
        #region 6.7 Separation Lines
        bool sSeparate(int n, YamlContext c) // [80] 
        {
            switch (c)
            {
                case YamlContext.BlockOut:
                case YamlContext.BlockIn:
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return sSeparateLines(n);
                case YamlContext.BlockKey:
                case YamlContext.FlowKey:
                    return sSeparateInLine();
                default:
                    throw new NotImplementedException();
            }
        }
        bool sSeparateLines(int n) // [81] 
        {
            return
                RewindUnless(() => s_lComments() && sFlowLinePrefix(n)) ||
                sSeparateInLine();
        }
        #endregion
        #region 6.8 Directives
        bool lDirective() // [82] 
        {
            return RewindUnless(() =>
                Text[P++] == '%' &&
                RewindUnless(() =>
                    nsYamlDirective() ||
                    nsTagDirective() ||
                    nsReservedDirective()) &&
                s_lComments()
                );
        }
        bool nsReservedDirective() // [83] 
        {
            var name = "";
            var args = new List<string>();
            return RewindUnless(() =>
                Save(() => OneAndRepeat(nsChar), ref name) &&
                Repeat(() =>
                    sSeparateInLine() && Save(() => OneAndRepeat(nsChar), s => args.Add(s))
                )
            ) &&
            Action(() => ReservedDirective(name, args.ToArray()));
        }
        bool YamlDirectiveAlreadyAppeared = false;
        bool nsYamlDirective() // [86] 
        {
            string version = "";
            return RewindUnless(() =>
                Accept("YAML") &&
                sSeparateInLine() &&
                Save(() =>
                    OneAndRepeat(Parsing.Charset.nsHexDigit) &&
                    Text[P++] == '.' &&
                    OneAndRepeat(Parsing.Charset.nsHexDigit),
                    ref version)
                ) &&
                Action(() =>
                {
                    if (YamlDirectiveAlreadyAppeared)
                        Error("The YAML directive must only be given at most once per document.");
                    YamlDirective(version);
                    YamlDirectiveAlreadyAppeared = true;
                });
        }
        bool nsTagDirective() // [88] 
        {
            string tag_handle = "";
            string tag_prefix = "";
            return RewindUnless(() =>
                Accept("TAG") && sSeparateInLine() &&
                ErrorUnless(() =>
                    Text[P++] == '!' &&
                    cTagHandle(out tag_handle) && sSeparateInLine() &&
                    nsTagPrefix(out tag_prefix),
                    "Invalid TAG directive found."
                )
            ) &&
            Action(() => TagPrefixes.Add(tag_handle, tag_prefix));
        }
        private bool cTagHandle(out string tag_handle) // [89]' 
        {
            var _tag_handle = tag_handle = "";
            if (Save(() => Optional(RewindUnless(() =>
                   Repeat(Parsing.Charset.nsWordChar) && Text[P++] == '!'
                   )),
                    s => _tag_handle = s))
            {
                tag_handle = "!" + _tag_handle;
                return true;
            }
            return false;
        }
        private bool nsTagPrefix(out string tag_prefix) // [93] 
        {
            return
                c_nsLocalTagPrefix(out tag_prefix) ||
                nsGlobalTagPrefix(out tag_prefix);
        }
        private bool c_nsLocalTagPrefix(out string tag_prefix) // [94] 
        {
            Debug.Assert(StringValue.Length == 0);
            if (RewindUnless(() =>
                   Text[P++] == '!' &&
                   Repeat(nsUriChar)
                ))
            {
                tag_prefix = "!" + StringValue.ToString();
                StringValue.Length = 0;
                return true;
            }
            tag_prefix = "";
            return false;
        }
        private bool nsGlobalTagPrefix(out string tag_prefix) // [95] 
        {
            Debug.Assert(StringValue.Length == 0);
            if (RewindUnless(() => nsTagChar() && Repeat(nsUriChar)))
            {
                tag_prefix = StringValue.ToString();
                StringValue.Length = 0;
                return true;
            }
            tag_prefix = "";
            return false;
        }
        #endregion
        #region 6.9 Node Properties
        bool c_nsProperties(int n, YamlContext c) // [96] 
        {
            base.State.Anchor = null;
            base.State.Tag = null;
            return
                (c_nsTagProperty() && Optional(RewindUnless(() => sSeparate(n, c) && c_nsAnchorProperty()))) ||
                (c_nsAnchorProperty() && Optional(RewindUnless(() => sSeparate(n, c) && c_nsTagProperty())));
        }
        bool c_nsTagProperty() // [97]' 
        {
            if (Text[P] != '!')
                return false;

            // reduce '!' here to improve perfomance
            P++;
            return
                cVerbatimTag() ||
                c_nsShorthandTag() ||
                cNonSpecificTag();
        }
        private bool cVerbatimTag() // [98]' 
        {
            return
                Text[P] == '<' &&
                ErrorUnless(
                    Text[P++] == '<' &&
                    OneAndRepeat(nsUriChar) &&
                    Text[P++] == '>',
                    "Invalid verbatim tag"
                ) &&
                SetTag(GetStringValue());
        }

        private bool c_nsShorthandTag() // [99]' 
        {
            var tag_handle = "";
            return RewindUnless(() =>
                cTagHandle(out tag_handle) &&
                ErrorUnlessWithAdditionalCondition(() =>
                    OneAndRepeat(nsTagChar),
                    tag_handle != "!",
                    $"The {tag_handle} handle has no suffix.") &&
                SetTag(tag_handle, GetStringValue())
            );
        }
        string GetStringValue()
        {
            var s = StringValue.ToString();
            StringValue.Length = 0;
            return s;
        }
        private bool cNonSpecificTag() // [100]' 
        {
            // disable tag resolution to restrict tag to be ( map | seq | str )
            base.State.Tag = "";
            return true; /* empty */
        }
        bool c_nsAnchorProperty() // [101] 
        {
            if (Text[P] != '&')
                return false;
            P++;
            return Save(this.nsAnchorName, s => base.State.Anchor = s);
        }
        private bool nsAnchorName() // [103] 
        {
            return OneAndRepeat(Parsing.Charset.nsAnchorChar);
        }
        #endregion
        #endregion

        #region Chapter 7. Flow Styles
        #region 7.1 Alias Nodes
        private bool c_nsAliasNode() // [104] 
        {
            string anchor_name = "";
            var pos = CurrentPosition;
            return RewindUnless(() =>
                Text[P++] == '*' &&
                Save(() => nsAnchorName(), s => anchor_name = s)
            ) &&
            SetValue(Anchors[anchor_name]);
        }
        #endregion
        #region 7.2 Empty Nodes
        /// <summary>
        /// [105]
        /// </summary>
        private bool eScalar()
        {
            Debug.Assert(StringValue.Length == 0);
            return SetValue(CreateScalar("!!null", CurrentPosition)); /* empty */
        }
        /// <summary>
        /// [106]
        /// </summary>
        private bool eNode()
        {
            return eScalar();
        }
        #endregion
        #region 7.3 Flow Scalar Styles
        #region 7.3.1 Double-Quoted Style
        private bool nbDoubleChar() // [107] 
        {
            int length;
            if (Text[P] != '\\' && Text[P] != '"' && Parsing.Charset.nbJson(Text, P, out length))
            {
                StringValue.Append(Text.Substring(P, length));
                P += length;
                return true;
            }
            return c_nsEscChar();
        }
        bool nsDoubleChar() // [108] 
        {
            return !Parsing.Charset.sWhite(Text, P) && nbDoubleChar();
        }
        private bool cDoubleQuoted(int n, YamlContext c) // [109] 
        {
            Position pos = CurrentPosition;
            Debug.Assert(StringValue.Length == 0);
            return Text[P] == '"' &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Text[P++] == '"' &&
                    nbDoubleText(n, c) &&
                    Text[P++] == '"',
                    c == YamlContext.FlowOut,
                    "Closing quotation \" was not found." +
                    (TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "")
                ) &&
                SetValue(CreateScalar("!!str", pos));
        }
        private bool nbDoubleText(int n, YamlContext c) // [110] 
        {
            switch (c)
            {
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return nbDoubleMultiLine(n);
                case YamlContext.BlockKey:
                case YamlContext.FlowKey:
                    return nbDoubleOneLine(n);
                default:
                    throw new NotImplementedException();
            }
        }
        private bool nbDoubleOneLine(int n) // [111] 
        {
            return Repeat(nbDoubleChar);
        }
        private bool sDoubleEscaped(int n) // [112] 
        {
            return RewindUnless(() =>
                Repeat_sWhiteAsString() &&
                Text[P++] == '\\' && bNonContent() &&
                Repeat(() => lEmpty(n, YamlContext.FlowIn)) &&
                sFlowLinePrefix(n)
                );
        }
        private bool sDoubleBreak(int n) // [113] 
        {
            return sDoubleEscaped(n) || sFlowFolded(n);
        }
        private bool nb_nsDoubleInLine() // [114] 
        {
            return Repeat(() => RewindUnless(() => Repeat_sWhiteAsString() && OneAndRepeat(nsDoubleChar)));
        }
        private bool sDoubleNextLine(int n) // [115] 
        {
            return
                sDoubleBreak(n) &&
                Optional(RewindUnless(() =>
                    nsDoubleChar() &&
                    nb_nsDoubleInLine() &&
                    (sDoubleNextLine(n) || Repeat(Repeat_sWhiteAsString))
                    ))
                ;
        }
        private bool nbDoubleMultiLine(int n) // [116] 
        {
            return nb_nsDoubleInLine() &&
                (sDoubleNextLine(n) || Repeat(Repeat_sWhiteAsString));
        }
        #endregion
        #region 7.3.2 Single-Quoted Style
        bool nbSingleChar() // [118] 
        {
            int length;
            if (Text[P] != '\'' && Parsing.Charset.nbJson(Text, P, out length))
            {
                StringValue.Append(Text.Substring(P, length));
                P += length;
                return true;
            }
            // [117] cQuotedQuote
            if (Text[P] == '\'' && Text[P + 1] == '\'')
            {
                StringValue.Append('\'');
                P += 2;
                return true;
            }
            return false;
        }
        bool nsSingleChar() // [119] 
        {
            return !Parsing.Charset.sWhite(Text, P) && nbSingleChar();
        }
        private bool cSingleQuoted(int n, YamlContext c) // [120] 
        {
            Debug.Assert(StringValue.Length == 0);
            Position pos = CurrentPosition;
            return Text[P] == '\'' &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Text[P++] == '\'' &&
                    nbSingleText(n, c) &&
                    Text[P++] == '\'',
                    c == YamlContext.FlowOut,
                    "Closing quotation \' was not found." +
                    (TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "")
                ) &&
                SetValue(CreateScalar("!!str", pos));
        }
        private bool nbSingleText(int n, YamlContext c) // [121] 
        {
            switch (c)
            {
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return nbSingleMultiLine(n);
                case YamlContext.BlockKey:
                case YamlContext.FlowKey:
                    return nbSingleOneLine(n);
                default:
                    throw new NotImplementedException();
            }
        }
        private bool nbSingleOneLine(int n) // [122] 
        {
            return Repeat(nbSingleChar);
        }
        private bool nb_nsSingleInLine() // [123] 
        {
            return Repeat(() => RewindUnless(() => Repeat_sWhiteAsString() && OneAndRepeat(nsSingleChar)));
        }
        private bool sSingleNextLine(int n) // [124] 
        {
            return RewindUnless(() =>
                sFlowFolded(n) && (
                    nsSingleChar() &&
                    nb_nsSingleInLine() &&
                    Optional(sSingleNextLine(n) || Repeat_sWhiteAsString())
                    )
                );
        }
        private bool nbSingleMultiLine(int n) // [125] 
        {
            return nb_nsSingleInLine() &&
                (sSingleNextLine(n) || Repeat_sWhiteAsString());
        }
        #endregion
        #region 7.3.3 Plain Style
        private bool nsPlainFirst(YamlContext c) // [126] 
        {
            int length;
            int dontCare;
            bool matchedNsPlainFirstSub;
            if ((matchedNsPlainFirstSub = Parsing.Charset.nsPlainFirstSub(Text, P, out length)) ||
                   ((Text[P] == '?' || Text[P] == ':' || Text[P] == '-') && Parsing.Charset.nsPlainSafe(Text, P + 1, out dontCare, c)))
            {
                WarnIfCharWasBreakInYAML1_1();
                if (matchedNsPlainFirstSub)
                {
                    StringValue.Append(Text.Substring(P, length));
                    P += length;
                }
                else
                {
                    StringValue.Append(Text[P++]);
                }
                return true;
            }
            return false;
        }
        private bool nsPlainSafe(YamlContext c) // [127] 
        {
            int length;
            if (!Parsing.Charset.nsPlainSafe(Text, P, out length, c))
                return false;
            WarnIfCharWasBreakInYAML1_1();
            StringValue.Append(Text.Substring(P, length));
            P += length;
            return true;
        }
        private bool nsPlainChar(YamlContext c) // [130] 
        {
            if (Text[P] != ':' && Text[P] != '#' && nsPlainSafe(c))
                return true;
            int lenght = 0;
            bool matchedNsPlainSafe = false;
            if (( /* An ns-char preceding '#' */
                    Parsing.Charset.PrecedingIsNsChar(Text, P) &&
                    Text[P] == '#')
                || ( /* ':' Followed by an ns-plain-safe */
                    Text[P] == ':' && (matchedNsPlainSafe = Parsing.Charset.nsPlainSafe(Text, P + 1, out lenght, c)))
                )
            {
                if (matchedNsPlainSafe)
                {
                    StringValue.Append(Text.Substring(P, lenght));
                    P += lenght;
                }
                else
                {
                    StringValue.Append(Text[P++]);
                }
                return true;
            }
            return false;
        }
        private bool nsPlain(int n, YamlContext c) // [131] 
        {
            if (cForbidden())
                return false;
            var pos = CurrentPosition;
            Debug.Assert(StringValue.Length == 0);
            switch (c)
            {
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return
                        nsPlainMultiLine(n, c) &&
                        SetValue(CreateScalar(null, pos));
                case YamlContext.BlockKey:
                case YamlContext.FlowKey:
                    return nsPlainOneLine(c) &&
                        SetValue(CreateScalar(null, pos));
                default:
                    throw new NotImplementedException();
            }
        }
        private bool nb_nsPlainInLine(YamlContext c) // [132] 
        {
            return Repeat(() => RewindUnless(() =>
                Repeat_sWhiteAsString() &&
                OneAndRepeat(() => nsPlainChar(c))
            ));
        }
        private bool nsPlainOneLine(YamlContext c) // [133] 
        {
            return nsPlainFirst(c) && nb_nsPlainInLine(c);
        }
        private bool s_nsPlainNextLine(int n, YamlContext c) // [134] 
        {
            return RewindUnless(() =>
                sFlowFolded(n) &&
                nsPlainChar(c) &&
                nb_nsPlainInLine(c)
            );
        }
        private bool nsPlainMultiLine(int n, YamlContext c) // [135] 
        {
            return
                nsPlainOneLine(c) &&
                Repeat(() => s_nsPlainNextLine(n, c));
        }
        #endregion
        #endregion
        #region 7.4 Flow Collection Styles
        private YamlContext InFlow(YamlContext c) // [136] 
        {
            switch (c)
            {
                case YamlContext.FlowOut:
                case YamlContext.FlowIn:
                    return YamlContext.FlowIn;
                case YamlContext.BlockKey:
                case YamlContext.FlowKey:
                    return YamlContext.FlowKey;
                default:
                    throw new NotImplementedException();
            }
        }
        #region 7.4.1 Flow Sequences
        private bool cFlowSequence(int n, YamlContext c) // [137] 
        {
            YamlSequence sequence = null;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                Text[P++] == '[' &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowSeqEntries(n, InFlow(c),
                                sequence = CreateSequence(pos))) &&
                    Text[P++] == ']',
                    c == YamlContext.FlowOut,
                    "Closing brace ] was not found." +
                    (TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "")
                )
            ) &&
            SetValue(sequence);
        }

        private bool ns_sFlowSeqEntries(int n, YamlContext c, YamlSequence sequence) // [138] 
        {
            return
                nsFlowSeqEntry(n, c) &&
                Action(() => sequence.Add(GetValue())) &&
                Optional(sSeparate(n, c)) &&
                Optional(RewindUnless(() =>
                    Text[P++] == ',' &&
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowSeqEntries(n, c, sequence))
                    ));
        }
        private bool nsFlowSeqEntry(int n, YamlContext c) // [139] 
        {
            YamlNode key = null;
            Position pos = CurrentPosition;
            return
                RewindUnless(() =>
                    nsFlowPair(n, c, ref key) &&
                    Action(() =>
                    {
                        var map = CreateMapping(pos);
                        map.Add(key, GetValue());
                        SetValue(map);
                    })
                ) ||
                nsFlowNode(n, c);
        }
        #endregion
        #region 7.4.2 Flow Mappings
        private bool cFlowMapping(int n, YamlContext c) // [140] 
        {
            Position pos = CurrentPosition;
            YamlMapping mapping = null;
            return RewindUnless(() =>
                Text[P++] == '{' &&
                Optional(sSeparate(n, c)) &&
                ErrorUnlessWithAdditionalCondition(() =>
                    Optional(ns_sFlowMapEntries(n, InFlow(c), mapping = CreateMapping(pos))) &&
                    Text[P++] == '}',
                    c == YamlContext.FlowOut,
                    "Closing brace }} was not found." +
                    (TabCharFoundForIndentation ? " Tab char \\t can not be used for indentation." : "")
                )
            ) &&
            SetValue(mapping);
        }
        private bool ns_sFlowMapEntries(int n, YamlContext c, YamlMapping mapping) // [141] 
        {
            YamlNode key = null;
            return
                nsFlowMapEntry(n, c, ref key) &&
                Action(() => mapping.Add(key, GetValue())) &&
                Optional(sSeparate(n, c)) &&
                Optional(RewindUnless(() =>
                    Text[P++] == ',' &&
                    Optional(sSeparate(n, c)) &&
                    Optional(ns_sFlowMapEntries(n, c, mapping))
                ));
        }
        private bool nsFlowMapEntry(int n, YamlContext c, ref YamlNode key) // [142] 
        {
            YamlNode _key = null;
            return (
                RewindUnless(() => Text[P++] == '?' && sSeparate(n, c) && nsFlowMapExplicitEntry(n, c, ref _key)) ||
                nsFlowMapImplicitEntry(n, c, ref _key)
            ) &&
            Assign(out key, _key);
        }
        private bool nsFlowMapExplicitEntry(int n, YamlContext c, ref YamlNode key) // [143] 
        {
            return nsFlowMapImplicitEntry(n, c, ref key) || (
                eNode() /* Key */ &&
                Assign(out key, GetValue()) &&
                eNode() /* Value */
            );
        }
        private bool nsFlowMapImplicitEntry(int n, YamlContext c, ref YamlNode key) // [144] 
        {
            return
                nsFlowMapYamlKeyEntry(n, c, ref key) ||
                c_nsFlowMapEmptyKeyEntry(n, c, ref key) ||
                c_nsFlowMapJsonKeyEntry(n, c, ref key);
        }
        private bool nsFlowMapYamlKeyEntry(int n, YamlContext c, ref YamlNode key) // [145] 
        {
            return
                nsFlowYamlNode(n, c) &&
                Assign(out key, GetValue()) && (
                    RewindUnless(() => (Optional(sSeparate(n, c)) && c_nsFlowMapSeparateValue(n, c))) ||
                    eNode()
                );
        }
        private bool c_nsFlowMapEmptyKeyEntry(int n, YamlContext c, ref YamlNode key) // [146] 
        {
            YamlNode _key = null;
            return RewindUnless(() =>
                eNode() /* Key */ &&
                Assign(out _key, GetValue()) &&
                c_nsFlowMapSeparateValue(n, c)
            ) &&
            Assign(out key, _key);
        }
        private bool c_nsFlowMapSeparateValue(int n, YamlContext c) // [147] 
        {
            int dontCare;
            return RewindUnless(() =>
                Text[P++] == ':'
                /* Not followed by an ns-plain-safe(c) */ && !Parsing.Charset.nsPlainSafe(Text, P, out dontCare, c) && (
                    RewindUnless(() => sSeparate(n, c) && nsFlowNode(n, c)) ||
                    eNode() /* Value */
                )
            );
        }
        private bool c_nsFlowMapJsonKeyEntry(int n, YamlContext c, ref YamlNode key) // [148] 
        {
            return
                cFlowJsonNode(n, c) &&
                Assign(out key, GetValue()) && (
                    RewindUnless(() => Optional(sSeparate(n, c)) && c_nsFlowMapAdjacentValue(n, c)) ||
                    eNode()
                );
        }
        private bool c_nsFlowMapAdjacentValue(int n, YamlContext c) // [149] 
        {
            return RewindUnless(() =>
                Text[P++] == ':' && (
                    RewindUnless(() => Optional(sSeparate(n, c)) && nsFlowNode(n, c)) ||
                    eNode() /* Value */
                    )
                );
        }
        private bool nsFlowPair(int n, YamlContext c, ref YamlNode key) // [150] 
        {
            YamlNode _key = null;
            return (
                RewindUnless(() => Text[P++] == '?' && sSeparate(n, c) && nsFlowMapExplicitEntry(n, c, ref _key)) ||
                nsFlowPairEntry(n, c, ref _key)
            ) &&
            Assign(out key, _key);
        }
        private bool nsFlowPairEntry(int n, YamlContext c, ref YamlNode key) // [151] 
        {
            return
                nsFlowPairYamlKeyEntry(n, c, ref key) ||
                c_nsFlowMapEmptyKeyEntry(n, c, ref key) ||
                c_nsFlowPairJsonKeyEntry(n, c, ref key);
        }
        private bool nsFlowPairYamlKeyEntry(int n, YamlContext c, ref YamlNode key) // [152] 
        {
            return
                ns_sImplicitYamlKey(YamlContext.FlowKey) &&
                Assign(out key, GetValue()) &&
                c_nsFlowMapSeparateValue(n, c);
        }
        private bool c_nsFlowPairJsonKeyEntry(int n, YamlContext c, ref YamlNode key) // [153] 
        {
            return
                c_sImplicitJsonKey(YamlContext.FlowKey) &&
                Assign(out key, GetValue()) &&
                c_nsFlowMapAdjacentValue(n, c);
        }
        private bool ns_sImplicitYamlKey(YamlContext c) // [154] 
        {
            /* At most 1024 characters altogether */
            int start = P;
            if (nsFlowYamlNode(-1 /* not used */, c) && Optional(sSeparateInLine))
            {
                ErrorUnless((P - start) < 1024, "The implicit key was too long.");
                return true;
            }
            return false;
        }
        private bool c_sImplicitJsonKey(YamlContext c) // [155] 
        {
            /* At most 1024 characters altogether */
            int start = P;
            if (cFlowJsonNode(-1 /* not used */, c) && Optional(sSeparateInLine))
            {
                ErrorUnless((P - start) < 1024, "The implicit key was too long.");
                return true;
            }
            return false;
        }
        #endregion
        #endregion
        #region 7.5 Flow Nodes
        private bool nsFlowYamlContent(int n, YamlContext c) // [156] 
        {
            return nsPlain(n, c);
        }
        private bool cFlowJsonContent(int n, YamlContext c) // [157] 
        {
            return cFlowSequence(n, c) || cFlowMapping(n, c) ||
                   cSingleQuoted(n, c) || cDoubleQuoted(n, c);
        }
        private bool nsFlowContent(int n, YamlContext c) // [158] 
        {
            return
                nsFlowYamlContent(n, c) ||
                cFlowJsonContent(n, c);
        }
        private bool nsFlowYamlNode(int n, YamlContext c) // [159] 
        {
            return
                c_nsAliasNode() ||
                nsFlowYamlContent(n, c) ||
                (c_nsProperties(n, c) && (
                    RewindUnless(() => sSeparate(n, c) && nsFlowYamlContent(n, c)) || eScalar()));
        }
        private bool cFlowJsonNode(int n, YamlContext c) // [160] 
        {
            return
                Optional(RewindUnless(() => c_nsProperties(n, c) && sSeparate(n, c))) &&
                cFlowJsonContent(n, c);
        }
        private bool nsFlowNode(int n, YamlContext c) // [161] 
        {
            if (c_nsAliasNode() ||
                nsFlowContent(n, c) ||
                RewindUnless(() => c_nsProperties(n, c) &&
                    (RewindUnless(() => sSeparate(n, c) && nsFlowContent(n, c)) || eScalar())))
                return true;
            if (Text[P] == '@' || Text[P] == '`')
                Error("Reserved indicators '@' and '`' can't start a plain scalar.");
            return false;
        }
        #endregion
        #endregion

        #region Chapter 8. Block Styles
        #region 8.1 Block Scalar Styles
        #region 8.1.1 Block Scalar Headers
        private bool c_bBlockHeader(out int m, out ChompingIndicator t) // [162] 
        {
            var _m = m = 0;
            var _t = t = ChompingIndicator.Clip;
            if (RewindUnless(() =>
                   ((cIndentationIndicator(ref _m) && Optional(cChompingIndicator(ref _t))) ||
                     (Optional(cChompingIndicator(ref _t)) && Optional(cIndentationIndicator(ref _m)))) &&
                   s_bComment()
                ))
            {
                m = _m;
                t = _t;
                return true;
            }
            return false;
        }
        bool cIndentationIndicator(ref int m) // [163] 
        {
            if (Parsing.Charset.nsDecDigit(Text, P))
            {
                m = Text[P] - '0';
                P++;
                return true;
            }
            return false;
        }
        bool cChompingIndicator(ref ChompingIndicator t) // [164] 
        {
            switch (Text[P])
            {
                case '-':
                    P++;
                    t = ChompingIndicator.Strip;
                    return true;
                case '+':
                    P++;
                    t = ChompingIndicator.Keep;
                    return true;
            }
            return false;
        }
        private bool bChompedLast(ChompingIndicator t) // [165] 
        {
            return EndOfFile() || (
                (t == ChompingIndicator.Strip) ? bNonContent() : bAsLineFeed()
            );
        }
        private bool lChompedEmpty(int n, ChompingIndicator t) // [166] 
        {
            return (t == ChompingIndicator.Keep) ? lKeepEmpty(n) : lStripEmpty(n);
        }
        private bool lStripEmpty(int n) // [167] 
        {
            return Repeat(() => RewindUnless(() => sIndentLE(n) && bNonContent())) &&
                   Optional(lTrailComments(n));
        }
        private bool lKeepEmpty(int n) // [168] 
        {
            return Repeat(() => lEmpty(n, YamlContext.BlockIn)) &&
                   Optional(lTrailComments(n));
        }
        private bool lTrailComments(int n) // [169] 
        {
            return RewindUnless(() =>
                sIndentLT(n) &&
                c_nbCommentText() &&
                bComment() &&
                Repeat(lComment)
            );
        }
        int AutoDetectIndentation(int n) // [170, 183]
        {
            int m = 0, max = 0, maxp = 0;
            RewindUnless(() =>
                Repeat(() => RewindUnless(() =>
                    Save(() => Repeat(Parsing.Charset.sSpace), s =>
                    {
                        if (s.Length > max)
                        {
                            max = s.Length;
                            maxp = P;
                        }
                    }) && bBreak())
                ) &&
                Save(() => Repeat(Parsing.Charset.sSpace), s => m = s.Length - n) &&
                Action(() => { if (Text[P] == '\t') TabCharFoundForIndentation = true; }) &&
                false // force Rewind
            );
            if (m < 1 && TabCharFoundForIndentation)
                Error("Tab character found for indentation.");
            if (m < max - n)
            {
                P = maxp;
                Error("Too many indentation was found.");
            }
            return m <= 1 ? 1 : m;
        }
        #endregion
        #region 8.1.2. Literal Style
        bool c_lLiteral(int n) // [170] 
        {
            Debug.Assert(StringValue.Length == 0);

            int m = 0;
            var t = ChompingIndicator.Clip;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                Text[P++] == '|' &&
                c_bBlockHeader(out m, out t) &&
                Action(() => { if (m == 0) m = AutoDetectIndentation(n); }) &&
                ErrorUnless(lLiteralContent(n + m, t), "Illegal literal text found.")
            ) &&
            SetValue(CreateScalar("!!str", pos));
        }
        bool l_nbLiteralText(int n) // [171] 
        {
            return RewindUnless(() =>
                Repeat(() => lEmpty(n, YamlContext.BlockIn)) &&
                sIndent(n) &&
                Save(() => Repeat(nbChar), s => StringValue.Append(s))
            );
        }
        bool b_nbLiteralNext(int n) // [172] 
        {
            return RewindUnless(() =>
                bAsLineFeed() &&
                !cForbidden() &&
                l_nbLiteralText(n)
            );
        }
        private bool lLiteralContent(int n, ChompingIndicator t) // [173] 
        {
            return RewindUnless(() =>
                Optional(RewindUnless(() => l_nbLiteralText(n) && Repeat(() => b_nbLiteralNext(n)) && bChompedLast(t))) &&
                lChompedEmpty(n, t)
            );
        }
        #endregion
        #region 8.1.3. Folded Style
        private bool c_lFolded(int n) // [174] 
        {
            Debug.Assert(StringValue.Length == 0);

            int m = 0;
            var t = ChompingIndicator.Clip;
            Position pos = CurrentPosition;
            return RewindUnless(() =>
                Text[P++] == '>' &&
                c_bBlockHeader(out m, out t) &&
                WarningIf(t == ChompingIndicator.Keep,
                  "Keep line breaks for folded text '>+' is invalid") &&
                Action(() => { if (m == 0) m = AutoDetectIndentation(n); }) &&
                ErrorUnless(lFoldedContent(n + m, t), "Illegal folded string found.")
            ) &&
            SetValue(CreateScalar("!!str", pos));
        }
        private bool s_nbFoldedText(int n) // [175] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                Save(() => nsChar() && Repeat(nbChar), s => StringValue.Append(s))
            );
        }
        private bool l_nbFoldedLines(int n) // [176] 
        {
            return s_nbFoldedText(n) &&
                Repeat(() => RewindUnless(() => b_lFolded(n, YamlContext.BlockIn) && s_nbFoldedText(n)));
        }
        private bool s_nbSpacedText(int n) // [177] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                Save(() => sWhite() && Repeat(nbChar), s => StringValue.Append(s))
            );
        }
        private bool b_lSpaced(int n) // [178] 
        {
            return
                bAsLineFeed() &&
                !cForbidden() &&
                Repeat(() => lEmpty(n, YamlContext.BlockIn));
        }
        private bool l_nbSpacedLines(int n) // [179] 
        {
            return RewindUnless(() =>
                s_nbSpacedText(n) &&
                Repeat(() => RewindUnless(() => b_lSpaced(n) && s_nbSpacedText(n)))
            );
        }
        private bool l_nbSameLines(int n) // [180] 
        {
            return RewindUnless(() =>
                Repeat(() => lEmpty(n, YamlContext.BlockIn)) &&
                (l_nbFoldedLines(n) || l_nbSpacedLines(n))
            );
        }
        private bool l_nbDiffLines(int n) // [181] 
        {
            return
                l_nbSameLines(n) &&
                Repeat(() => RewindUnless(() => bAsLineFeed() && !cForbidden() && l_nbSameLines(n)));
        }
        private bool lFoldedContent(int n, ChompingIndicator t) // [182] 
        {
            return RewindUnless(() =>
                Optional(RewindUnless(() => l_nbDiffLines(n) && bChompedLast(t))) &&
                lChompedEmpty(n, t)
            );
        }
        #endregion
        #endregion
        #region 8.2. Block Collection Styles
        #region 8.2.1 Block Sequences
        private bool lBlockSequence(int n) // [183] 
        {
            int m = AutoDetectIndentation(n);
            int dontCare;
            YamlSequence sequence = null;
            Position pos = new Position();
            return OneAndRepeat(() => RewindUnless(() =>
                sIndent(n + m) &&
                Action(() => { if (sequence == null) pos = CurrentPosition; }) &&
                Text[P] == '-' && !Parsing.Charset.nsChar(Text, P + 1, out dontCare) &&
                Action(() => { if (sequence == null) sequence = CreateSequence(pos); }) &&
                c_lBlockSeqEntry(n + m, sequence)
            )) &&
            SetValue(sequence);
        }
        private bool c_lBlockSeqEntry(int n, YamlSequence sequence) // [184] 
        {
            int dontCare;
            Debug.Assert(Text[P] == '-' && !Parsing.Charset.nsChar(Text, P + 1, out dontCare));
            P++;
            return
                s_lBlockIndented(n, YamlContext.BlockIn) &&
                Action(() => sequence.Add(GetValue()));
        }
        bool s_lBlockIndented(int n, YamlContext c) // [185] 
        {
            int m;
            return
                RewindUnless(() => sIndentCounted(n, out m) &&
                    (ns_lCompactSequence(n + 1 + m) || ns_lCompactMapping(n + 1 + m))) ||
                s_lBlockNode(n, c) ||
                (eNode() && s_lComments());
        }
        private bool ns_lCompactSequence(int n) // [186] 
        {
            YamlSequence sequence = null;
            Position pos = CurrentPosition;
            int dontCare;
            return
                Text[P] == '-' && !Parsing.Charset.nsChar(Text, P + 1, out dontCare) &&
                Action(() => sequence = CreateSequence(pos)) &&
                c_lBlockSeqEntry(n, sequence) &&
                Repeat(() => RewindUnless(() =>
                    sIndent(n) &&
                    Text[P] == '-' && !Parsing.Charset.nsChar(Text, P + 1, out dontCare) &&
                    c_lBlockSeqEntry(n, sequence))) &&
                SetValue(sequence);
        }
        #endregion
        #region 8.2.2 Block Mappings
        private bool lBlockMapping(int n) // [187] 
        {
            YamlMapping mapping = null;
            int m = 0;
            YamlNode key = null;
            return OneAndRepeat(() =>
                sIndent(n + m) &&
                (m > 0 || sIndentCounted(n, out m)) &&
                Action(() =>
                {
                    if (mapping == null)
                    {
                        mapping = CreateMapping(CurrentPosition);
                    }
                }) &&
                ns_lBlockMapEntry(n + m, ref key) &&
                Action(() => mapping.Add(key, GetValue()))
            ) &&
            SetValue(mapping);
        }
        private bool ns_lBlockMapEntry(int n, ref YamlNode key) // [188] 
        {
            return c_lBlockMapExplicitEntry(n, ref key) ||
                   ns_lBlockMapImplicitEntry(n, ref key);
        }
        private bool c_lBlockMapExplicitEntry(int n, ref YamlNode key) // [189] 
        {
            YamlNode _key = null;
            return RewindUnless(() =>
                c_lBlockMapExplicitKey(n, ref _key) &&
                ErrorUnless(
                    (lBlockMapExplicitValue(n) || eNode()),
                    "Illegal block mapping explicit entry"
                )
            ) &&
            Assign(out key, _key);
        }
        private bool c_lBlockMapExplicitKey(int n, ref YamlNode key) // [190] 
        {
            return RewindUnless(() =>
                Text[P++] == '?' &&
                s_lBlockIndented(n, YamlContext.BlockOut)
            ) &&
            Assign(out key, GetValue());
        }
        private bool lBlockMapExplicitValue(int n) // [191] 
        {
            return RewindUnless(() =>
                sIndent(n) &&
                Text[P++] == ':' &&
                s_lBlockIndented(n, YamlContext.BlockOut)
            );
        }
        private bool ns_lBlockMapImplicitEntry(int n, ref YamlNode key) // [192] 
        {
            YamlNode _key = null;
            return RewindUnless(() =>
                (ns_sBlockMapImplicitKey() || eNode()) &&
                Assign(out _key, GetValue()) &&
                c_lBlockMapImplicitValue(n)
            ) &&
            Assign(out key, _key);
        }
        private bool ns_sBlockMapImplicitKey() // [193] 
        {
            return c_sImplicitJsonKey(YamlContext.BlockKey) ||
                   ns_sImplicitYamlKey(YamlContext.BlockKey);
        }
        private bool c_lBlockMapImplicitValue(int n) // [194] 
        {
            return RewindUnless(() =>
                Text[P++] == ':' &&
                (s_lBlockNode(n, YamlContext.BlockOut) || (eNode() && s_lComments()))
            );
        }
        private bool ns_lCompactMapping(int n) // [195] 
        {
            var mapping = CreateMapping(CurrentPosition);
            YamlNode key = null;
            return RewindUnless(() =>
                ns_lBlockMapEntry(n, ref key) &&
                Action(() => mapping.Add(key, GetValue())) &&
                Repeat(() => RewindUnless(() =>
                    sIndent(n) &&
                    ns_lBlockMapEntry(n, ref key) &&
                    Action(() => mapping.Add(key, GetValue()))
                ))
            ) &&
            SetValue(mapping);
        }
        #endregion
        #region 8.2.3 Block Nodes
        bool s_lBlockNode(int n, YamlContext c) // [196] 
        {
            return
                s_lBlockInBlock(n, c) ||
                s_lFlowInBlock(n);
        }
        bool s_lFlowInBlock(int n) // [197] 
        {
            return RewindUnless(() =>
                sSeparate(n + 1, YamlContext.FlowOut) &&
                nsFlowNode(n + 1, YamlContext.FlowOut) &&
                s_lComments()
                );
        }
        bool s_lBlockInBlock(int n, YamlContext c) // [198] 
        {
            Debug.Assert(StringValue.Length == 0);
            return
                s_lBlockScalar(n, c) ||
                s_lBlockCollection(n, c);
        }
        bool s_lBlockScalar(int n, YamlContext c) // [199] 
        {
            return RewindUnless(() =>
                sSeparate(n + 1, c) &&
                Optional(RewindUnless(() => c_nsProperties(n + 1, c) && sSeparate(n + 1, c))) &&
                (c_lLiteral(n) || c_lFolded(n))
                );
        }
        bool s_lBlockCollection(int n, YamlContext c) // [200]
        {
            return RewindUnless(() =>
                Optional(RewindUnless(() => sSeparate(n + 1, c) && c_nsProperties(n + 1, c))) &&
                s_lComments() &&
                (lBlockSequence(SeqSpaces(n, c)) || lBlockMapping(n))
            ) ||
            RewindUnless(() =>
                s_lComments() &&
                (lBlockSequence(SeqSpaces(n, c)) || lBlockMapping(n))
            );
        }
        private int SeqSpaces(int n, YamlContext c) // [201]
        {
            switch (c)
            {
                case YamlContext.BlockOut:
                    return n - 1;
                case YamlContext.BlockIn:
                    return n;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion
        #endregion
        #endregion

        #region Chapter 9. YAML Character Stream
        #region 9.1. Documents
        private bool lDocumentPrefix() // [202] 
        {
            return Optional(Parsing.Charset.cByteOrdermark) && Repeat(this.lComment);
        }
        private bool cDirectivesEnd() // [203] 
        {
            return Accept("---");
        }
        private bool cDocumentEnd() // [204] 
        {
            return Accept("...");
        }
        bool lDocumentSuffix() // [205] 
        {
            return RewindUnless(() =>
                cDocumentEnd() &&
                s_lComments()
            );
        }
        bool cForbidden() // [206] 
        {
            if (!StartOfLine() || (Text.Length - P) < 3)
                return false;
            var s = Text.Substring(P, 3);
            if (s != "---" && s != "...")
                return false;
            return
                Text.Length - P == 3 + 1 ||
                Parsing.Charset.sWhite(Text, P + 3) ||
                Parsing.Charset.bChar(Text, P + 3);
        }
        bool lBareDocument() // [207] 
        {
            var length = StringValue.Length;
            var s = StringValue.ToString();
            StringValue.Length = 0;
            Debug.Assert(length == 0, $"stringValue should be empty but '{s}' was found");
            base.State.Value = null;

            TagPrefixes.SetupDefaultTagPrefixes();
            return
                s_lBlockNode(-1, YamlContext.BlockIn) &&
                Action(() => ParseResult.Add(GetValue()));
        }
        bool lExplicitDocument() // [208] 
        {
            return RewindUnless(() =>
                cDirectivesEnd() &&
                (lBareDocument() || eNode() && s_lComments() && Action(() => ParseResult.Add(GetValue())))
            );
        }
        bool lDirectiveDocument() // [209] 
        {
            YamlDirectiveAlreadyAppeared = false;
            return RewindUnless(() =>
                OneAndRepeat(lDirective) && lExplicitDocument()
            );
        }
        #endregion
        #region 9.2. Streams
        bool lAnyDocument() // [210] 
        {
            return
                lDirectiveDocument() ||
                lExplicitDocument() ||
                lBareDocument();
        }
        private bool lYamlStream() // [211] 
        {
            TagPrefixes.Reset();
            Anchors.RewindDepth = 0;
            base.State.AnchorDepth = 0;
            WarningAdded.Clear();
            Warnings.Clear();
            StringValue.Length = 0;
            bool BomReduced = false;
            if (Repeat(this.lDocumentPrefix) &&
                Optional(this.lAnyDocument) &&
                Repeat(() =>
                    TagPrefixes.Reset() &&
                    RewindUnless(() =>
                        OneAndRepeat(() => lDocumentSuffix() && Action(() => BomReduced = false)) &&
                        Repeat(this.lDocumentPrefix) && Optional(this.lAnyDocument)) ||
                    RewindUnless(() =>
                        Repeat(() => Action(() => BomReduced |= Parsing.Charset.cByteOrdermark(Text, P)) && lDocumentPrefix()) &&
                        Optional(lExplicitDocument() && Action(() => BomReduced = false)))
                    ) &&
                EndOfFile())
                return true;

            int dontCare;

            if (BomReduced)
            {
                Error("A BOM (\\ufeff) must not appear inside a document.");
            }
            else
            if (Parsing.Charset.cIndicator(Text, P))
            {
                Error("Plain text can not start with indicator characters -?:,[]{{}}#&*!|>'\"%@`");
            }
            else
            if (Text[P] == ' ' && StartOfLine())
            {
                Error("Extra line was found. Maybe indentation was incorrect.");
            }
            else
            if (Parsing.Charset.nbChar(Text, P, out dontCare))
            {
                Error("Extra content was found. Maybe indentation was incorrect.");
            }
            else
            {
                var charValue = (Text[P] < 0x100) ? $"'\\x{(int)Text[P]:x2}'" : $"'\\u{(int)Text[P]:x4}'";
                Error($"An illegal character {charValue} appeared.");
            }
            return false;
        }
        #endregion
        #endregion

        #endregion
    }
}
