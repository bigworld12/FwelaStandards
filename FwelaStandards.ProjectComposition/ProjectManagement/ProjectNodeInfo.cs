using Catel.Collections;
using Catel.Data;
using Catel.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FwelaStandards.ProjectComposition
{
    public class ProjectNodeInfo : ModelBase
    {
        public IProjectPart Part { get; }
        public T GetPartAs<T>() where T : IProjectPart
        {
            return (T)Part;
        }
        public T GetRootPartAs<T>() where T : IRootProjectPart
        {
            return (T)RootNode.Part;
        }

        public IRootProjectPart GetRootPart()
        {
            return (IRootProjectPart)RootNode.Part;
        }
        public ProjectNodeInfo RootNode { get; }
        public ProjectNodeInfo? Parent { get; set; }

        public NodeObservableDictionary AsDictionary { get; }
        public NodeObservableCollection AsList { get; }

        public ProjectPartDependency DependencyInfo { get; }
        protected override void RaisePropertyChanged(object sender, AdvancedPropertyChangedEventArgs e)
        {
            if (IsInitializing) return;
            base.RaisePropertyChanged(sender, e);
        }
        public IEnumerable<ProjectNodeInfo> AllChildren => AsDictionary.Values.Concat(AsList);

        public ObservableCollection<T> ChildrenToObservableCollection<T>() where T : IProjectPart
        {
            var res = new ObservableCollection<T>();
            ChildrenToObservableCollection(res);
            return res;
        }
        public void ChildrenToObservableCollection<T>(ObservableCollection<T> res) where T : IProjectPart
        {
            res.AddRange(AsList.Select(x => (T)x.Part));

            void OriginalChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                res.CollectionChanged -= CopyChanged;
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Add:
                        //modify res
                        if (args.NewItems != null)
                            for (int i = 0; i < args.NewItems.Count; i++)
                            {
                                var newItem = (ProjectNodeInfo)args.NewItems[i];
                                res.Insert(args.NewStartingIndex + i, (T)newItem.Part);
                            }
                        if (args.OldItems != null)
                            for (int i = 0; i < args.OldItems.Count; i++)
                            {
                                //var oldItem = (ProjectNodeInfo)args.OldItems[i];
                                res.RemoveAt(args.OldStartingIndex + i);
                            }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        res.Move(args.OldStartingIndex, args.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (args.NewItems != null)
                            for (int i = 0; i < args.NewItems.Count; i++)
                            {
                                res[args.OldStartingIndex + i] = (T)((ProjectNodeInfo)args.NewItems[i]).Part;
                            }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        res.Clear();
                        break;
                }
                res.CollectionChanged += CopyChanged;
            }
            void CopyChanged(object senderList, NotifyCollectionChangedEventArgs args)
            {

                AsList.CollectionChanged -= OriginalChanged;
                //modify Original
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Add:
                        if (args.NewItems != null)
                            for (int i = 0; i < args.NewItems.Count; i++)
                            {
                                var newItem = (T)args.NewItems[i];
                                var nodeInfo = newItem.NodeInfo;
                                if (nodeInfo is null)
                                {
                                    nodeInfo = newItem.InitFromParent(this, (args.NewStartingIndex + i).TransformIndex());
                                    //after initializing node, rebuild relative tree ?

                                    //void NewItemPropertyChanged(object senderItem, PropertyChangedEventArgs e)
                                    //{
                                    //    if (senderList is IList<T> copyList && senderItem is T Part && e.HasPropertyChanged(nameof(Part.NodeInfo)) && !(Part.NodeInfo is null))
                                    //    {
                                    //        Part.PropertyChanged -= NewItemPropertyChanged;
                                    //        ChildrenAsList.Insert(args.NewStartingIndex + i, Part.NodeInfo);
                                    //    }
                                    //}
                                    ////delay insert
                                    //newItem.PropertyChanged += NewItemPropertyChanged;
                                }
                                //else
                                AsList.Insert(args.NewStartingIndex + i, nodeInfo);
                            }
                        if (args.OldItems != null)
                            for (int i = 0; i < args.OldItems.Count; i++)
                            {
                                //var oldItem = (ProjectNodeInfo)args.OldItems[i];
                                AsList.RemoveAt(args.OldStartingIndex + i);
                            }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        AsList.Move(args.OldStartingIndex, args.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (args.NewItems != null)
                            for (int i = 0; i < args.NewItems.Count; i++)
                            {
                                var newItem = (T)args.NewItems[i];
                                var nodeInfo = newItem.NodeInfo;
                                if (nodeInfo is null)
                                {
                                    nodeInfo = newItem.InitFromParent(this, (args.OldStartingIndex + i).TransformIndex());
                                    //void NewItemPropertyChanged(object senderItem, PropertyChangedEventArgs e)
                                    //{
                                    //    if (senderList is IList<T> copyList && senderItem is T Part && e.HasPropertyChanged(nameof(Part.NodeInfo)) && !(Part.NodeInfo is null))
                                    //    {
                                    //        Part.PropertyChanged -= NewItemPropertyChanged;
                                    //        ChildrenAsList[args.NewStartingIndex + i] = Part.NodeInfo;
                                    //    }
                                    //}
                                    //newItem.PropertyChanged += NewItemPropertyChanged;
                                }
                                //else
                                AsList[args.OldStartingIndex + i] = nodeInfo;
                            }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        AsList.Clear();
                        break;
                }
                AsList.CollectionChanged += OriginalChanged;
            }

            AsList.CollectionChanged += OriginalChanged;
            res.CollectionChanged += CopyChanged;
        }

        private bool IsInitializing = true;
        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            if (IsInitializing) return;
            base.OnPropertyChanged(e);
        }

        public T GetParentPart<T>() where T : IProjectPart
        {
            if (Parent is null)
            {
                throw new InvalidOperationException("Can't get root's parent");
            }
            return (T)Parent.Part;
        }
        public T GetChildPart<T>([CallerMemberName] string name = "") where T : IProjectPart
        {
            return (T)AsDictionary[name].Part;
        }
        public ProjectNodeInfo GetChild(string name)
        {
            return AsDictionary[name];
        }
        public void RegisterChildNode(ProjectNodeInfo childInfo)
        {
            AsDictionary[childInfo.Name] = childInfo;
        }
        public void RegisterChildPart<T>(T childPart, string name, bool raisePropertyChangedOnPart = true) where T : IProjectPart
        {
            var cNI = childPart.InitFromParent(this, name);
            RegisterChildNode(cNI);
            if (raisePropertyChangedOnPart)
            {
                Part.RaisePropertyChanged(name);
            }
        }

        public void RegisterListChild<T>(T childPart) where T : IProjectPart
        {
            var cNI = childPart.InitFromParent(this, AsList.Count.TransformIndex());

            AsList.Add(cNI);
        }
        public void RegisterListChild<T>(IEnumerable<T> childrenParts) where T : IProjectPart
        {
            foreach (var item in childrenParts)
            {
                RegisterListChild(item);
            }
        }
        public ProjectNodeInfo(IProjectPart part, ProjectNodeInfo? parent = null, string? name = null)
        {
            Part = part;
            if (parent is null || name is null)
            {
                Name = "*";
                RootNode = this;
                Parent = null;
            }
            else
            {
                Name = name;
                RootNode = parent.RootNode;
                Parent = parent;
            }
            CleanName = GetCleanName(Name);
            FullPath = GetFullPath(Name, false);
            CleanFullPath = GetFullPath(Name, true);


            AsDictionary = new NodeObservableDictionary(this);
            AsList = new NodeObservableCollection(this);
            DependencyInfo = new ProjectPartDependency(this);
            
        }
        public void StartListeningToChanges()
        {
            PropertyChanged += ProjectNodeInfo_PropertyChanged;
            AsList.CollectionChanged += AsList_CollectionChanged;
            DependencyInfo.StartListeningToChanges();
            IsInitializing = false;
        }


        private void ProjectNodeInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine($"Entered Property Changed {e.PropertyName}");
            switch (e.PropertyName)
            {
                case nameof(Name):
                    if (e is AdvancedPropertyChangedEventArgs adv)
                    {
                        if (adv.IsOldValueMeaningful && adv.OldValue is string oldName && adv.IsNewValueMeaningful && adv.NewValue is string newName)
                        {
                            CleanName = GetCleanName(newName);
                            FullPath = GetFullPath(newName, false);
                            CleanFullPath = GetFullPath(newName, true);
                            Part.RaiseNameChanged(new AdvancedPropertyChangedEventArgs(adv.OriginalSender, Part, adv.PropertyName, oldName, newName));
                        }
                        else
                        {
                            throw new InvalidOperationException("Old value was meaningless");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Name is a registered property but it didn't raise advanced args");
                    }
                    break;
                case nameof(FullPath):
                    foreach (var item in AllChildren)
                    {
                        item.FullPath = GetFullPath(item.Name, false);
                        item.CleanFullPath = GetFullPath(item.Name, true);
                    }
                    break;
                default:
                    break;
            }
        }



        private void AsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //change children's paths based on collection changes
            for (int i = 0; i < AsList.Count; i++)
            {
                AsList[i].Name = i.TransformIndex();
            }
        }
        public bool IsListChild => Name.StartsWith("Item[");
        public string Name { get; set; }
        public string CleanName { get; private set; }

        public static Regex ItemRegex = new Regex(@"Item\[[0-9]+\]", RegexOptions.Compiled | RegexOptions.Singleline);
        private static string GetCleanName(string name) => ItemRegex.Replace(name, ProjectPartDependency.ItemIndexer);


        public string FullPath { get; private set; }
        public string CleanFullPath { get; private set; }
        public string GetFullPath(string name, bool isClean)
        {
            var n = isClean ? GetCleanName(name) : name;
            if (Parent is ProjectNodeInfo p)
            {
                var fp = isClean ? p.CleanFullPath : p.FullPath;
                return $"{fp}.{n}";
            }
            else
            {
                return n;
            }
        }

        public string ResolvePathUntilRoot(string prop = "")
        {
            return ResolvePathUntil(RootNode, prop);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stopAt"></param>
        /// <param name="prop"></param>
        /// <param name="useBracketsForParts">set to false to use the <see cref="WalkPathDownFrom"/> method</param>
        /// <returns></returns>
        public string ResolvePathUntil(ProjectNodeInfo stopAt, string prop = "")
        {
            StringBuilder sb = new StringBuilder();
            ProjectNodeInfo? temp = this;
            while (!(temp is null) && !ReferenceEquals(stopAt, temp))
            {
                var parent = temp.Parent;
                string rep;
                rep = $".{temp.Name}";
                sb.Insert(0, rep);
                temp = parent;
            }
            if (!string.IsNullOrWhiteSpace(prop))
            {
                sb.Append($".{prop}");
            }
            return sb.ToString().Trim('.');
        }

        public ProjectNodeInfo WalkPathDownFromRoot(string fullPath)
        {
            return WalkPathDownFrom(RootNode, fullPath);
        }
        private static readonly char[] seps = new char[] { '.' };
        public static ProjectNodeInfo WalkPathDownFrom(ProjectNodeInfo start, string fullPath)
        {
            var splitPath = fullPath.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            ProjectNodeInfo current = start;
            for (int i = 0; i < splitPath.Length; i++)
            {
                var subPath = splitPath[i];
                current = current.AsDictionary[subPath];
            }
            return current;
        }
        public ProjectPartDependency GetDependencyFromRoot(string fullPath)
        {
            var from = WalkPathDownFromRoot(fullPath);
            return from.DependencyInfo;
        }

        public void RegisterListItemDependency(ProjectNodeInfo from, string itemPropName, params string[] toPropNames)
        {
            RegisterDependency(from, $"Item[].{itemPropName}".TrimEnd('.'), toPropNames);
        }


        public void RegisterDependency(ProjectNodeInfo from, string fromPropName, params string[] toPropNames)
        {
            var dep = from.DependencyInfo;
            var (subActions, subProps) = dep.DirectOrRelativeDeps.GetOrAdd(fromPropName, (path) => (new HashSet<EventHandler>(), new HashSet<(ProjectNodeInfo, string prop)>()));

            foreach (var item in toPropNames)
            {
                subProps.Add((this, item));
            }
        }
        public void RegisterMultiFromDependency(ProjectNodeInfo from, string toPropName, params string[] fromPropNames)
        {
            var dep = from.DependencyInfo;
            foreach (var fromPropName in fromPropNames)
            {
                var (subActions, subProps) = dep.DirectOrRelativeDeps.GetOrAdd(fromPropName, (path) => (new HashSet<EventHandler>(), new HashSet<(ProjectNodeInfo, string prop)>()));
                subProps.Add((this, toPropName));
            }
        }
        public void RegisterAction(ProjectNodeInfo from, string fromPropName, EventHandler eventHandler)
        {
            var dep = from.DependencyInfo;
            var (subActions, subProps) = dep.DirectOrRelativeDeps.GetOrAdd(fromPropName, (path) => (new HashSet<EventHandler>(), new HashSet<(ProjectNodeInfo, string prop)>()));
            subActions.Add(eventHandler);

        }
    }

}
