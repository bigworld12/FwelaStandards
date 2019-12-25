using Catel.Data;
using System;

namespace FwelaStandards.ProjectComposition
{
    public class BaseProjectPart : ValidatableModelBase, IProjectPart
    {
        public BaseProjectPart()
        {
            AutomaticallyValidateOnPropertyChanged = false;
        }
        public IProjectPart? Parent { get; set; }
        public ProjectNodeInfo? NodeInfo { get; private set; }
        /// <summary>
        /// quick use for node info
        /// </summary>
        public ProjectNodeInfo NI => NodeInfo ?? throw new InvalidOperationException("Node info can't be used before initialization");

        public ProjectNodeInfo InitFromParent(ProjectNodeInfo? parentNode, string? name)
        {
            NodeInfo = new ProjectNodeInfo(this, parentNode, name);
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
            return ObjectAdapter.GetMemberValue<T>(this, propName, out var res) ? res : throw new PropertyNotRegisteredException(propName, GetType());
        }


    }

}
