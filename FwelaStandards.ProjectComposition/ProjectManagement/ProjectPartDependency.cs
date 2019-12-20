using Catel.Data;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FwelaStandards.ProjectComposition
{
    public class ProjectPartDependency
    {
        public ProjectPartDependency(ProjectNodeInfo from)
        {
            From = from;
            from.Part.PropertyChanged += DefaultListener; //handles direct ONLY A, B, ...            
            //from.Children.ItemsComponentPropertyChanged += Children_ComponentPropertyChanged; //dependency on child list component properties Item[].A , Item[].B, ...
            ((INotifyCollectionChanged)from.Children).CollectionChanged += ChildList_CollectionChanged; //dependency on child list properties Item[] , Item[].A , Item[].B , ...
        }

        public const string ItemIndexer = "Item[]";
        public void HandlePropInDirectOrRelativeDictionary(string name,bool checkStartsWith = false)
        {
            void loop(HashSet<EventHandler> subActions, HashSet<(ProjectNodeInfo targetObj, string targetPropName)> subProps)
            {
                foreach (var (nodeInfo, componentPropName) in subProps)
                {
                    nodeInfo.Part.RaisePropertyChanged(componentPropName);
                }
                foreach (var item in subActions)
                {
                    item(From.Part, EventArgs.Empty);
                }


            }
            if (checkStartsWith)
            {
                foreach (var propNamePair in DirectOrRelativeDeps)
                {
                    if (propNamePair.Key.StartsWith(name))
                    {
                        var (subActions, subProps) = propNamePair.Value;
                        loop(subActions, subProps);
                    }
                }
            }
            else
            {
                if (DirectOrRelativeDeps.TryGetValue(name, out (HashSet<EventHandler> Actions, HashSet<(ProjectNodeInfo nodeInfo, string componentPropName)> Props) sub))
                {
                    loop(sub.Actions, sub.Props);
                }
            }

            //walk the node tree, if parent has path that references current path
            if (From.Parent is ProjectNodeInfo currentParent)
            {
                currentParent.DependencyInfo.HandlePropInDirectOrRelativeDictionary($"{From.GetCleanName()}.{name}",true);
            }
        }

        private void DefaultListener(object sender, PropertyChangedEventArgs eOld)
        {
            if (!(eOld is AdvancedPropertyChangedEventArgs e)) return;
            HandlePropInDirectOrRelativeDictionary(e.PropertyName);
        }
        
        private void ChildList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Remove:
                    //Handle anything that starts with Item[]
                    HandlePropInDirectOrRelativeDictionary(ItemIndexer);
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }


        public ProjectNodeInfo From { get; }
        public IProjectPart Component => From.Part;
        
        /// <summary>
        /// Stores 
        /// Direct : Item[], A, B, Item[].A, Item[].B
        /// Relative : A.B.C, A.Item[].B, Item[].A.B
        /// </summary>
        public ConcurrentDictionary<string, (HashSet<EventHandler> subActions, HashSet<(ProjectNodeInfo nodeInfo, string componentPropName)> subProps)> DirectOrRelativeDeps { get; }
            = new ConcurrentDictionary<string, (HashSet<EventHandler> subActions, HashSet<(ProjectNodeInfo nodeInfo, string componentPropName)> subProps)>();


        //public ConcurrentDictionary<string, ObservableCollection<ErrorLog>> Errors { get; } = new ConcurrentDictionary<string, ObservableCollection<ErrorLog>>();
    }

}
