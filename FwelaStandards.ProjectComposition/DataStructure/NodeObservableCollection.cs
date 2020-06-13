using Catel.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FwelaStandards.ProjectComposition
{
    public class NodeObservableCollection : ObservableCollection<ProjectNodeInfo>
    {
        public ProjectNodeInfo Owner { get; }
        public NodeObservableCollection(ProjectNodeInfo owner)
        {
            Owner = owner;            
        }

        public NodeObservableCollection(ProjectNodeInfo owner, IEnumerable<ProjectNodeInfo> collection) : base(collection)
        {
            Owner = owner;
        }

        public NodeObservableCollection(ProjectNodeInfo owner, List<ProjectNodeInfo> list) : base(list)
        {
            Owner = owner;
        }
    }

}
