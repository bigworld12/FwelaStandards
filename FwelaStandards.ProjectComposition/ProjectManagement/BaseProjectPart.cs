using Catel.Data;
using System;

namespace FwelaStandards.ProjectComposition
{
    public class BaseProjectPart : ValidatableModelBase, IProjectPart
    {
        public IProjectPart? this[string index]
        {
            get { return NodeInfo?.GetChildPart<IProjectPart>(index); }
        }
        public IProjectPart? Parent { get; set; }
        public ProjectNodeInfo? NodeInfo { get; set; }

        public string? Name => NodeInfo?.Name;

        
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
            return (T)GetDirectPropertyValue(propName);
        }

        public object GetDirectPropertyValue(string propName)
        {
            if (IsPropertyRegistered(propName))
            {
                return GetValue(propName);
            }
            else
            {
                return GetType().GetProperty(propName).GetValue(this);
            }
        }
    }

}
