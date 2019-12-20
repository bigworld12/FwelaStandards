using Catel.Data;
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
        string? Name { get; }
        ProjectNodeInfo InitFromParent(ProjectNodeInfo? parentNode);
        void RegisterAllDeps(ProjectNodeInfo nodeInfo);
        void RegisterAllChildren(ProjectNodeInfo nodeInfo);
        IProjectPart? Parent { get; }
    }
    public interface IRootProjectPart : IProjectPart
    {
    }
    public delegate void NodeListItemPropertyChanged(ProjectNodeInfo list, IProjectPart item, PropertyChangedEventArgs args);

}
