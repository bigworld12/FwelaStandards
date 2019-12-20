using Catel.Data;
using System;

namespace FwelaStandards.ProjectComposition
{
    public class BaseProjectPart : ModelBase, IProjectPart
    {
        public IProjectPart? this[string index]
        {
            get { return NodeInfo?.GetChildPart<IProjectPart>(index); }
        }
        public IProjectPart? Parent { get; set; }
        public ProjectNodeInfo? NodeInfo { get; set; }

        public string? Name => NodeInfo?.Name;

        /*/// <summary>
        /// Don't bind to this
        /// </summary>
        public UnitSystems GetUnitSystem()
        {
            var pd = NodeInfo?.GetRootPart().Details;
            if (pd is null)
                return UnitSystems.Imperial;
            return pd.UnitSystem;
        }*/
        public IRootProjectPart? GetRootPart()
        {
            return (IRootProjectPart?)NodeInfo?.Part;
        }
        public ProjectNodeInfo InitFromParent(ProjectNodeInfo? parentNode)
        {
            NodeInfo = new ProjectNodeInfo(this, parentNode);
            Parent = parentNode?.Part;
            RegisterAllChildren(NodeInfo);
            RegisterAllDeps(NodeInfo);
            return NodeInfo;
        }
        public virtual void RegisterAllChildren(ProjectNodeInfo nodeInfo) { }
        public virtual void RegisterAllDeps(ProjectNodeInfo nodeInfo) { }

        void ICanRaisePropertyChanged.RaisePropertyChanged(string propName)
        {
            RaisePropertyChanged(propName);
        }

        public T GetDirectPropertyValue<T>(string propName) where T : class
        {
            if (IsPropertyRegistered(propName))
            {
                return GetValue<T>(propName);
            }
            else
            {
                return (T)GetType().GetProperty(propName).GetValue(this);
            }

        }
    }

}
