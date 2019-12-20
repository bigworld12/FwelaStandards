using System.Collections.ObjectModel;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class ListPartC : BaseProjectPart
    {
        public override void RegisterAllChildren(ProjectNodeInfo nodeInfo)
        {
            base.RegisterAllChildren(nodeInfo);
            nodeInfo.ChildrenToObservableCollection(ListOfD);
        }
        public ObservableCollection<ListPartD> ListOfD { get; } = new ObservableCollection<ListPartD>();
    }
}
