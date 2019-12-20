using System.Collections.ObjectModel;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class PartAType : BaseProjectPart
    {
        public override void RegisterAllChildren(ProjectNodeInfo nodeInfo)
        {
            base.RegisterAllChildren(nodeInfo);
            nodeInfo.ChildrenToObservableCollection(ListOfB);
        }
        public ObservableCollection<ListPartB> ListOfB { get; } = new ObservableCollection<ListPartB>();

    }
}
