using Catel.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        public string Name => NI.Name;

        protected bool IsInitializing = true;
        public ProjectNodeInfo InitFromParent(ProjectNodeInfo? parentNode, string? name)
        {
            NodeInfo = new ProjectNodeInfo(this, parentNode, name);
            Parent = parentNode?.Part;
            RegisterAllChildren(NodeInfo);
            RegisterAllDeps(NodeInfo);
            foreach (var depHandler in extraDeps)
            {
                depHandler(NodeInfo);
            }
            extraDeps.Clear();
            NodeInfo.StartListeningToChanges();
            IsInitializing = false;
            return NodeInfo;
        }

        public virtual void RegisterAllChildren(ProjectNodeInfo nodeInfo) { }
        public virtual void RegisterAllDeps(ProjectNodeInfo nodeInfo) { }
        public void RegisterExtraDependencies(Action<ProjectNodeInfo> action)
        {
            if (NodeInfo == null)
            {
                extraDeps.Add(action);
            }
            else
            {
                action(NodeInfo);
            }
        }
        
        private HashSet<Action<ProjectNodeInfo>> extraDeps { get; } = new HashSet<Action<ProjectNodeInfo>>();
        void ICanRaisePropertyChanged.RaisePropertyChanged(string propName)
        {
            if (IsInitializing) return;
            RaisePropertyChanged(propName);
        }
        protected override void RaisePropertyChanged(object sender, AdvancedPropertyChangedEventArgs e)
        {
            if (IsInitializing) return;
            base.RaisePropertyChanged(sender, e);
        }

        public T GetDirectPropertyValue<T>(string propName)
        {   
            return ObjectAdapter.GetMemberValue<T>(this, propName, out var res) ? res : throw new PropertyNotRegisteredException(propName, GetType());
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            if (IsInitializing) return;
            base.OnPropertyChanged(e);
        }
        public void RaiseNameChanged(AdvancedPropertyChangedEventArgs args)
        {
            if (IsInitializing) return;
            base.RaisePropertyChanged(this, args);
        }
    }

}
