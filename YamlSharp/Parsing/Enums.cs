using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YamlSharp.Parsing
{
    internal enum YamlContext
    {
        BlockIn,
        BlockOut,
        FlowIn,
        FlowOut,
        BlockKey,
        FlowKey,
        Folded,
    }

    internal enum ChompingIndicator
    {
        Strip,
        Keep,
        Clip
    }
}
