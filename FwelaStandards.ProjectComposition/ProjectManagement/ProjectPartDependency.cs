using Catel.Data;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace FwelaStandards.ProjectComposition
{
    public class ProjectPartDependency
    {
        public ProjectPartDependency(ProjectNodeInfo from)
        {
            From = from;
        }
        public void StartListeningToChanges()
        {
            From.Part.PropertyChanged += DefaultListener; //handles direct ONLY A, B, ...            
            //from.Children.ItemsPartPropertyChanged += Children_PartPropertyChanged; //dependency on child list Part properties Item[].A , Item[].B, ...
            From.AsList.CollectionChanged += ChildList_CollectionChanged; //dependency on child list properties Item[] , Item[].A , Item[].B , ...
        }
        public const string ItemIndexer = "Item[]";
        public void HandlePropInDirectOrRelativeDictionary(string name, bool checkStartsWith = false)
        {
            void loop(HashSet<PropertyChangedEventHandler> subActions, HashSet<(ProjectNodeInfo targetObj, string targetPropName, Func<string, bool>? ShouldRaise)> subProps)
            {
                foreach (var (nodeInfo, PartPropName, shouldRaise) in subProps)
                {
                    if (shouldRaise is Func<string, bool> shouldRaiseFunc)
                    {
                        if (shouldRaiseFunc(PartPropName))
                            nodeInfo.Part.RaisePropertyChanged(PartPropName);
                    }
                    else
                    {
                        nodeInfo.Part.RaisePropertyChanged(PartPropName);
                    }
                }
                foreach (var item in subActions)
                {
                    item(From.Part, new PropertyChangedEventArgs(name));
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
                if (DirectOrRelativeDeps.TryGetValue(name, out (HashSet<PropertyChangedEventHandler> Actions, HashSet<(ProjectNodeInfo nodeInfo, string PartPropName, Func<string, bool>? ShouldRaise)> Props) sub))
                {
                    loop(sub.Actions, sub.Props);
                }
            }

            //walk the node tree, if parent has path that references current path
            if (From.Parent is ProjectNodeInfo currentParent)
            {
                currentParent.DependencyInfo.HandlePropInDirectOrRelativeDictionary($"{From.CleanName}.{name}", true);
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
                    HandlePropInDirectOrRelativeDictionary(ItemIndexer,true);
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
        }


        public ProjectNodeInfo From { get; }
        public IProjectPart Part => From.Part;

        /// <summary>
        /// Stores 
        /// Direct : Item[], A, B, Item[].A, Item[].B
        /// Relative : A.B.C, A.Item[].B, Item[].A.B
        /// </summary>
        public ConcurrentDictionary<string, (HashSet<PropertyChangedEventHandler> subActions, HashSet<(ProjectNodeInfo nodeInfo, string PartPropName, Func<string, bool>? ShouldRaise)> subProps)> DirectOrRelativeDeps { get; }
            = new ConcurrentDictionary<string, (HashSet<PropertyChangedEventHandler> subActions, HashSet<(ProjectNodeInfo nodeInfo, string PartPropName, Func<string, bool>? ShouldRaise)> subProps)>();


        //public ConcurrentDictionary<string, ObservableCollection<ErrorLog>> Errors { get; } = new ConcurrentDictionary<string, ObservableCollection<ErrorLog>>();
    }

}
