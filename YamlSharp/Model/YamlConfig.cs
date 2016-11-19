﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlSharp;

namespace YamlSharp.Model
{
    /// <summary>
    /// <para>Configuration to customize YAML serialization.</para>
    /// <para>An instance of this class can be passed to the serialization
    /// methods, such as <see cref="YamlNode.ToYaml(YamlConfig)">YamlNode.ToYaml(YamlConfig)</see> and
    /// <see cref="YamlNode.FromYaml(Stream,YamlConfig)">YamlNode.FromYaml(Stream,YamlConfig)</see> or
    /// it can be assigned to <see cref="YamlNode.DefaultConfig">YamlNode.DefaultConfig</see>.
    /// </para>
    /// </summary>
    public class YamlConfig
    {
        /// <summary>
        /// If true, all line breaks in the node value are normalized into "\r\n" 
        /// (= <see cref="LineBreakForOutput"/>) when serialize and line breaks 
        /// that are not escaped in YAML stream are normalized into "\n"
        /// (= <see cref="LineBreakForInput"/>.
        /// If false, the line breaks are preserved. Setting this option false violates 
        /// the YAML specification but sometimes useful. The default is true.
        /// </summary>
        /// <remarks>
        /// <para>The YAML sepcification requires a YAML parser to normalize every line break that 
        /// is not escaped in a YAML stream, into a single line feed "\n" when it parse a YAML stream. 
        /// But this is not convenient in some cases, especially under Windows environment, where 
        /// the system default line break 
        /// is "\r\n" instead of "\n".</para>
        /// 
        /// <para>This library provides two workarounds for this problem.</para>
        /// <para>One is setting <see cref="NormalizeLineBreaks"/> false. It disables the line break
        /// normalization. The line breaks are serialized into a YAML stream as is and 
        /// those in the YAML stream are deserialized as is.</para>
        /// <para>Another is setting <see cref="LineBreakForInput"/> "\r\n". Then, the YAML parser
        /// normalizes all line breaks into "\r\n" instead of "\n".</para>
        /// <para>Note that although these two options are useful in some cases,
        /// they makes the YAML parser violate the YAML specification. </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public bool NormalizeLineBreaks = true;
        /// <summary>
        /// If true, all <see cref="YamlScalar"/>s whose text expression contains line breaks 
        /// will be presented as double quoted texts, where the line break characters are escaped 
        /// by back slash as "\\n" and "\\r". The default is true.
        /// </summary>
        /// <remarks>
        /// <para>The escaped line breaks makes the YAML stream hard to read, but is required to 
        /// prevent the line break characters be normalized by the YAML parser; the YAML 
        /// sepcification requires a YAML parser to normalize all line breaks that are not escaped
        /// into a single line feed "\n" when it parse a YAML source.</para>
        /// 
        /// <para>
        /// If the preservation of line breaks are not required, set this value false.
        /// </para>
        /// 
        /// <para>Then, whenever it is possible, the <see cref="YamlNode"/>s are presented
        /// as literal style text, where the line breaks are not escaped. This results in
        /// a much prettier output in the YAML stream.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public bool ExplicitlyPreserveLineBreaks = true;
        /// <summary>
        /// Line break to be used when <see cref="YamlNode"/> is presented in YAML stream. 
        /// "\r", "\r\n", "\n" are allowed. "\r\n" is defalut.
        /// </summary>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public string LineBreakForOutput = "\r\n";
        /// <summary>
        /// <para>The YAML parser normalizes line breaks in a YAML stream to this value.</para>
        /// 
        /// <para>"\n" is default, and is the only valid value in the YAML specification. "\r" and "\r\n" are
        /// allowed in this library for convenience.</para>
        /// 
        /// <para>To suppress normalization of line breaks by YAML parser, set <see cref="NormalizeLineBreaks"/> 
        /// false, though it is also violate the YAML specification.</para>
        /// </summary>
        /// <remarks>
        /// <para>The YAML sepcification requires a YAML parser to normalize every line break that 
        /// is not escaped in a YAML stream, into a single line feed "\n" when it parse a YAML stream. 
        /// But this is not convenient in some cases, especially under Windows environment, where 
        /// the system default line break 
        /// is "\r\n" instead of "\n".</para>
        /// 
        /// <para>This library provides two workarounds for this problem.</para>
        /// <para>One is setting <see cref="NormalizeLineBreaks"/> false. It disables the line break
        /// normalization. The line breaks are serialized into a YAML stream as is and 
        /// those in the YAML stream are deserialized as is.</para>
        /// <para>Another is setting <see cref="LineBreakForInput"/> "\r\n". Then, the YAML parser
        /// normalizes all line breaks into "\r\n" instead of "\n".</para>
        /// <para>Note that although these two options are useful in some cases,
        /// they makes the YAML parser violate the YAML specification. </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // A string containing line breaks "\n\r" and "\r".
        /// YamlNode node = "a\r\n  b\rcde";
        /// 
        /// // By default conversion, line breaks are escaped in a double quoted string.
        /// var yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // "a\r\n\
        /// // \  b\r\
        /// // cde"
        /// // ...
        /// 
        /// // "%YAML 1.2\r\n---\r\n\"a\\r\\n\\\r\n\  b\\r\\\r\ncde\"\r\n...\r\n"
        /// 
        /// // Such a YAML stream is not pretty but is capable to preserve 
        /// // original line breaks even when the line breaks of the YAML stream
        /// // are changed (for instance, by some editor) between serialization 
        /// // and deserialization.
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        /// 
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // still equivalent to the original
        /// 
        /// // By setting ExplicitlyPreserveLineBreaks false, the output becomes
        /// // much prettier.
        /// YamlNode.DefaultConfig.ExplicitlyPreserveLineBreaks = false;
        /// yaml = node.ToYaml();
        /// // %YAML 1.2
        /// // ---
        /// // |-2
        /// //   a
        /// //     b
        /// //   cde
        /// // ...
        /// 
        /// // line breaks are nomalized to "\r\n" (= YamlNode.DefaultConfig.LineBreakForOutput)
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\r\ncde\r\n...\r\n"
        /// 
        /// // line breaks are nomalized to "\n" (= YamlNode.DefaultConfig.LineBreakForInput)
        /// var restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"
        /// 
        /// 
        /// // Disable line break normalization.
        /// YamlNode.DefaultConfig.NormalizeLineBreaks = false;
        /// yaml = node.ToYaml();
        /// 
        /// // line breaks are not nomalized
        /// // "%YAML 1.2\r\n---\r\n|-2\r\n  a\r\n    b\rcde\r\n...\r\n"
        /// 
        /// // Unless line breaks in YAML stream is preserved, original line
        /// // breaks can be restored.
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\r\n  b\rcde"      // equivalent to the original
        ///                     
        /// yaml = yaml.Replace("\r\n", "\n").Replace("\r", "\n");
        /// restored = YamlNode.FromYaml(yaml)[0];
        /// // "a\n  b\ncde"        // original line breaks are lost
        /// </code>
        /// </example>
        public string LineBreakForInput = "\n";
        /// <summary>
        /// If true, tag for the root node is omitted by <see cref="YamlSharp.Serialization.YamlSerializer"/>.
        /// </summary>
        public bool OmitTagForRootNode = false;
        /// <summary>
        /// If true, the verbatim style of a tag, i.e. !&lt; &gt; is avoided as far as possible.
        /// </summary>
        public bool DontUseVerbatimTag = false;

        /// <summary>
        /// Add a custom tag resolution rule.
        /// </summary>
        /// <example>
        /// <code>
        /// 
        /// </code>
        /// </example>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="tag">Tag for the value.</param>
        /// <param name="regex">Pattern to match the value.</param>
        /// <param name="decode">Method that decode value from <see cref="Match"/> 
        ///     data after matching by <paramref name="regex"/>.</param>
        /// <param name="encode">Method that encode value to <see cref="string"/>.</param>
        public void AddRule<T>(string tag, string regex, Func<Match, T> decode, Func<T, string> encode)
        {
            TagResolver.AddRule<T>(tag, regex, decode, encode);
        }
        internal YamlTagResolver TagResolver = new YamlTagResolver();

        /// <summary>
        /// Add an ability of instantiating an instance of a class that has no default constructer.
        /// </summary>
        /// <typeparam name="T">Type of the object that is activated by this <paramref name="activator"/>.</typeparam>
        /// <param name="activator">A delegate that creates an instance of <typeparamref name="T"/>.</param>
        /// <example>
        /// <code>
        /// var serializer= new YamlSerializer();
        /// 
        /// var yaml =
        ///   @"%YAML 1.2
        ///   ---
        ///   !System.Drawing.SolidBrush
        ///   Color: Red
        ///   ...
        ///   ";
        /// 
        /// SolidBrush b = null;
        /// try {
        ///   b = (SolidBrush)serializer.Deserialize(yaml)[0];
        /// } catch(MissingMethodException) {
        ///   // SolidBrush has no default constructor!
        /// }
        /// 
        /// YamlNode.DefaultConfig.AddActivator&lt;SolidBrush&gt;(() => new SolidBrush(Color.Black));
        /// 
        /// // Now the serializer knows how to activate an instance of SolidBrush.
        /// b = (SolidBrush)serializer.Deserialize(yaml)[0];
        /// 
        /// Assert.AreEqual(b.Color, Color.Red);
        /// </code>
        /// </example>
        public void AddActivator<T>(Func<object> activator)
            where T : class
        {
            Activator.Add<T>(activator);
        }
        internal YamlSharp.Serialization.ObjectActivator Activator =
            new YamlSharp.Serialization.ObjectActivator();

        /// <summary>
        /// Gets or sets CultureInfo with which the .NET native values are converted
        /// to / from string. Currently, this is not to be changed from CultureInfo.InvariantCulture.
        /// </summary>
        internal CultureInfo Culture
        {
            get { return TypeConverter.Culture; }
            set { TypeConverter.Culture = value; }
        }
        internal YamlSharp.Serialization.EasyTypeConverter TypeConverter =
            new YamlSharp.Serialization.EasyTypeConverter();
    }
}
