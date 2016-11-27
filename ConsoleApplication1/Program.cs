using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlSharp.Model;
using YamlSharp.Parsing;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            ////            var content = @"

            ////# dfgdfg

            //// &a [*a]";

            ////            var content = @"
            ////? a :
            ////    - b
            ////    -  - c
            ////       - d
            ////";

            //var content = @"
            //a : &anchor foo
            //c : *anchor
            //b : &anchor bar
            //d : *anchor
            //";

            ////            var content = @"
            ////[
            ////""folded to a space,\n\
            ////      to a line feed, \
            ////      or \t \tnon - content""
            ////,
            ////""folded 
            ////to a space,	

            ////to a line feed, or 	\
            //// \ 	non - content""
            ////]";

            ////            var content = @"
            ////--- ""he\0lჿFFlo""
            ////";
            //// System.Text.Encoding.UTF8.
            //var x = char.ConvertFromUtf32(0x10FFFF);
            //var y = char.ConvertFromUtf32(0x20);
            //var z = char.ConvertFromUtf32(0x41);
            //var a = char.ConvertFromUtf32(0x2665);
            //string bla = x + y + z + a;

            // string content = "!hel%F4%8F%BF%BFlo%E2%99%A5";

            string content = "[a,[b,c],d]";

            var contentNodes = YamlNode.FromYaml(content);

            //var back = contentNodes.First().ToYaml();

            //var backContent = YamlNode.FromYaml(back);

            var seq = contentNodes.First() as YamlSequence;
            var a = seq[0];
            var bc = seq[1];
            var d = seq[2];

            string inMemoryString;
            using (var reader = new StreamReader(@"D:\Mes Documents\Dev\yaml\yamlreference\.stack-work\install\7282fce5\bin\gros.txt", Encoding.UTF8))
            {
                inMemoryString = reader.ReadToEnd();
            }

            //YamlSharp.Serialization.YamlSerializer serializer = new YamlSharp.Serialization.YamlSerializer();
            //var roundTrip = serializer.Serialize(nodes);



            var watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 7; i++)
            {
                var nodes = YamlNode.FromYaml(inMemoryString, YamlNode.DefaultConfig);
                yaml = nodes;
            }

            watch.Stop();
            Debug.WriteLine(watch.Elapsed);
            result = (watch.ElapsedMilliseconds / 7).ToString();
            // around 1500-1550 ms / iteration on my machine

        }
        static string result;
        static YamlNode[] yaml;
    }
}
