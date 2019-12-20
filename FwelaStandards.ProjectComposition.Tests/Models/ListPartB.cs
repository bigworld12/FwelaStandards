using System.Collections.ObjectModel;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class ListPartB : BaseProjectPart
    {
        public override void RegisterAllChildren(ProjectNodeInfo nodeInfo)
        {
            base.RegisterAllChildren(nodeInfo);
            nodeInfo.ChildrenToObservableCollection(ListOfC);
        }
        public ObservableCollection<ListPartC> ListOfC { get; } = new ObservableCollection<ListPartC>();
    }
}
