using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yaml.Model;

namespace Yaml.Parsing
{
    /// <summary>
    /// Reset();
    /// SetupDefaultTagPrefixes();
    /// Add(tag_handle, tag_prefix);
    /// verbatim_tag = Resolve(tag_handle, tag_name);
    /// </summary>
    internal class YamlTagPrefixes
    {
        Dictionary<string, string> TagPrefixes = new Dictionary<string, string>();
        Func<string, bool> error;

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

        public YamlTagPrefixes(Func<string, bool> error)
        {
            this.error = error;
        }
        void Error(string message)
        {
            error(message);
        }
        public bool Reset()
        {
            TagPrefixes.Clear();
            return true;
        }
        public void SetupDefaultTagPrefixes()
        {
            if (!TagPrefixes.ContainsKey("!"))
                TagPrefixes.Add("!", "!");
            if (!TagPrefixes.ContainsKey("!!"))
                TagPrefixes.Add("!!", YamlNode.DefaultTagPrefix);
        }
        public void Add(string tag_handle, string tag_prefix)
        {
            if (TagPrefixes.ContainsKey(tag_handle))
            {
                switch (tag_handle)
                {
                    case "!":
                        Error($"Primary tag prefix is already defined as '{TagPrefixes["!"]}'.");
                        break;
                    case "!!":
                        Error($"Secondary tag prefix is already defined as '{TagPrefixes["!!"]}'.");
                        break;
                    default:
                        Error($"Tag prefix for the handle {tag_handle} is already defined as '{TagPrefixes[tag_handle]}'.");
                        break;
                }
            }
            TagPrefixes.Add(tag_handle, tag_prefix);
        }
        public string Resolve(string tag_handle, string tag_name)
        {
            if (!TagPrefixes.ContainsKey(tag_handle))
                Error($"Tag handle {tag_handle} is not registered.");
            var tag = TagPrefixes[tag_handle] + tag_name;
            return tag;
        }
    }
}
