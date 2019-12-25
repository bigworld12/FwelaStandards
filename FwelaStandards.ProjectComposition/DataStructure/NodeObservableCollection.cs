using Catel.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FwelaStandards.ProjectComposition
{
    public class NodeObservableCollection : FastObservableCollection<ProjectNodeInfo>
    {
        public ProjectNodeInfo Owner { get; }
        public NodeObservableCollection(ProjectNodeInfo owner)
        {
            Owner = owner;
        }
    }

}
