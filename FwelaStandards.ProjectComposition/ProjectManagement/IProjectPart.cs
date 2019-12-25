using Catel.Data;
using System.Collections.Generic;
using System.ComponentModel;

namespace FwelaStandards.ProjectComposition
{
    public interface ICanRaisePropertyChanged : INotifyPropertyChanged
    {
        void RaisePropertyChanged(string propName);
    }
    public interface ICanGetValueFromPropName
    {   
        T? GetDirectPropertyValue<T>(string propName) where T : class;
    }
    public interface IProjectPart : ICanRaisePropertyChanged, ICanGetValueFromPropName, IAdvancedNotifyPropertyChanged
    {
        ProjectNodeInfo? NodeInfo { get; } //equivalent to linked list node
        
        ProjectNodeInfo InitFromParent(ProjectNodeInfo? parentNode,string name);
        void RegisterAllDeps(ProjectNodeInfo nodeInfo);
        void RegisterAllChildren(ProjectNodeInfo nodeInfo);
        IProjectPart? Parent { get; }
    }
    public interface IRootProjectPart : IProjectPart
    {
        IReadOnlyDictionary<string,ProjectNodeInfo> AllNodes { get; }
        /// <summary>
        /// adds new node and removes the old one
        /// </summary>
        /// <param name="node"></param>
        /// <param name="oldNode"></param>
        /// <returns><c>true</c> if it found an old node, <c>false</c> if not</returns>
        bool SetNewNode(ProjectNodeInfo node, out ProjectNodeInfo? oldNode);
        /// <summary>
        /// removes the node from the dictionary
        /// </summary>
        /// <param name="node"></param>
        void SetOldNode(ProjectNodeInfo node);
    }
}
