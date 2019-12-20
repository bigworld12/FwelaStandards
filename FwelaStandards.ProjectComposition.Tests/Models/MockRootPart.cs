using System.Linq;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class MockRootPart : BaseRootProjectPart
    {
        public MockRootPart()
        {
            
        }
        
        public override void RegisterAllChildren(ProjectNodeInfo nodeInfo)
        {
            base.RegisterAllChildren(nodeInfo);
            nodeInfo.RegisterChildPart(new PartAType(), nameof(PartA));
        }
        public override void RegisterAllDeps(ProjectNodeInfo nodeInfo)
        {
            base.RegisterAllDeps(nodeInfo);
            nodeInfo.RegisterDependency(nodeInfo, $"{nameof(PartA)}.Item[].Item[].Item[].Value", nameof(ValueSum));            
        }
        public PartAType? PartA => NodeInfo?.GetChildPart<PartAType>();
        public int ValueSum => PartA?.ListOfB.Sum(x => x.ListOfC.Sum(y => y.ListOfD.Sum(z => z.Value))) ?? 0;
    }
}
