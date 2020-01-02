using Catel.Collections;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FwelaStandards.ProjectComposition
{
    public class NodeObservableDictionary : FastObservableDictionary<string, ProjectNodeInfo>
    {
        public ProjectNodeInfo Owner { get; }
        public NodeObservableDictionary(ProjectNodeInfo owner)
        {
            Owner = owner;
        }

    }
}
