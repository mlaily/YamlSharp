using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlSharp.Model;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var content = @"

# dfgdfg

 &a [*a]";

            //            var content = @"
            //? a :
            //    - b
            //    -  - c
            //       - d
            //";

            //            var content = @"
            //a : &anchor foo
            //c : *anchor
            //b : &anchor bar
            //d : *anchor
            //";

            //            var content = @"
            //[
            //""folded to a space,\n\
            //      to a line feed, \
            //      or \t \tnon - content""
            //,
            //""folded 
            //to a space,	

            //to a line feed, or 	\
            // \ 	non - content""
            //]";

            //            var content = @"
            //--- ""he\0lჿFFlo""
            //";
            // System.Text.Encoding.UTF8.
            var x = char.ConvertFromUtf32(0x10FFFF);
            var y = char.ConvertFromUtf32(0x20);
            var z = char.ConvertFromUtf32(0x41);
            var a = char.ConvertFromUtf32(0x2665);
            string bla = x + y + z + a;
            var contentNodes = YamlNode.FromYaml(content);

            var back = contentNodes.First().ToYaml();

            using (var reader = new StreamReader(@"D:\Mes Documents\Dev\yaml\yamlreference\.stack-work\install\7282fce5\bin\gros.txt",
                Encoding.UTF8))
            {
                var nodes = YamlNode.FromYaml(reader);
                YamlSharp.Serialization.YamlSerializer serializer = new YamlSharp.Serialization.YamlSerializer();
                var roundTrip = serializer.Serialize(nodes);
            }

        }
    }
}
