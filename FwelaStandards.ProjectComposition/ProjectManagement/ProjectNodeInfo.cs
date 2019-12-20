using Catel.Collections;
using Catel.Data;
using Catel.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
        /// <summary>
        /// [Root.]A.B.Item[10].C.D
        /// </summary>
        public NodeDictionaryList Children { get; }
        public ProjectPartDependency DependencyInfo { get; }

        public IMovableList<ProjectNodeInfo> ChildrenAsList => Children;

        public ObservableCollection<T> ChildrenToObservableCollection<T>() where T : IProjectPart
        {
            var res = new ObservableCollection<T>();
            ChildrenToObservableCollection(res);
            return res;
        }
        public void ChildrenToObservableCollection<T>(ObservableCollection<T> res) where T : IProjectPart
        {
            res.AddRange(ChildrenAsList.Select(x => (T)x.Part));

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

                Children.CollectionChanged -= OriginalChanged;
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
                                    nodeInfo = newItem.InitFromParent(this);
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
                                ChildrenAsList.Insert(args.NewStartingIndex + i, nodeInfo);
                            }
                        if (args.OldItems != null)
                            for (int i = 0; i < args.OldItems.Count; i++)
                            {
                                //var oldItem = (ProjectNodeInfo)args.OldItems[i];
                                ChildrenAsList.RemoveAt(args.OldStartingIndex + i);
                            }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        ChildrenAsList.Move(args.OldStartingIndex, args.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (args.NewItems != null)
                            for (int i = 0; i < args.NewItems.Count; i++)
                            {
                                var newItem = (T)args.NewItems[i];
                                var nodeInfo = newItem.NodeInfo;
                                if (nodeInfo is null)
                                {
                                    nodeInfo = newItem.InitFromParent(this);
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
                                ChildrenAsList[args.OldStartingIndex + i] = nodeInfo;
                            }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        ChildrenAsList.Clear();
                        break;
                }
                Children.CollectionChanged += OriginalChanged;
            }

            Children.CollectionChanged += OriginalChanged;
            res.CollectionChanged += CopyChanged;
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
            return (T)Children[name].Part;
        }
        public ProjectNodeInfo GetChild(string name)
        {
            return Children[name];
        }
        public void RegisterChildPart(ProjectNodeInfo childInfo, string name)
        {
            childInfo.Name = name;
            Children[name] = childInfo;
        }
        public void RegisterChildPart<T>(T childPart, string name, bool raisePropertyChangedOnPart = true) where T : IProjectPart
        {
            childPart.InitFromParent(this);
            if (childPart.NodeInfo is null)
            {
                throw new InvalidOperationException("Filed to initialize child Part");
            }
            RegisterChildPart(childPart.NodeInfo, name);
            if (raisePropertyChangedOnPart)
            {
                Part.RaisePropertyChanged(name);
            }
        }

        public void RegisterListChild<T>(T childPart) where T : IProjectPart
        {
            childPart.InitFromParent(this);
            if (childPart.NodeInfo is null)
            {
                throw new InvalidOperationException("Filed to initialize Part");
            }
            ChildrenAsList.Add(childPart.NodeInfo);
        }
        public void RegisterListChild<T>(IEnumerable<T> childrenParts) where T : IProjectPart
        {
            foreach (var item in childrenParts)
            {
                RegisterListChild(item);
            }
        }
        public ProjectNodeInfo(IProjectPart part, ProjectNodeInfo? parent = null)
        {
            Part = part;
            Children = new NodeDictionaryList(this);
            DependencyInfo = new ProjectPartDependency(this);
            PropertyChanged += ProjectNodeInfo_PropertyChanged;
            Children.CollectionChanged += Children_CollectionChanged;
            Part.PropertyChanged += Part_PropertyChanged;
            if (parent is null)
            {
                Name = "*";
                RootNode = this;
                Parent = null;
            }
            else
            {
                RootNode = parent.RootNode;
                Parent = parent;
            }
        }

        private void ProjectNodeInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    Part.RaisePropertyChanged(nameof(Part.Name));
                    RaisePropertyChanged(nameof(FullPath));
                    break;
                default:
                    break;
            }
        }

        private void Part_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FullPath):
                    foreach (var item in Children)
                    {
                        item.Value.RaisePropertyChanged(nameof(FullPath));
                    }
                    break;
                default:
                    break;
            }
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //change children's paths based on collection changes
            for (int i = 0; i < ChildrenAsList.Count; i++)
            {
                ChildrenAsList[i].Name = $"Item[{i}]";
            }
        }

        public string Name { get; set; } = "$null";
        public static Regex ItemRegex = new Regex(@"Item\[[0-9]+\]", RegexOptions.Compiled | RegexOptions.Singleline);
        public string GetCleanName() => ItemRegex.Replace(Name, ProjectPartDependency.ItemIndexer);
        public string FullPath => ResolvePathUntilRoot();
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
        public static ProjectNodeInfo WalkPathDownFrom(ProjectNodeInfo start, string fullPath)
        {
            var splitPath = fullPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            ProjectNodeInfo current = start;
            for (int i = 0; i < splitPath.Length; i++)
            {
                var subPath = splitPath[i];
                current = current.Children[subPath];
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
        public void RegisterAction(ProjectNodeInfo from, string fromPropName, params EventHandler[] eventHandlers)
        {
            var dep = from.DependencyInfo;
            var (subActions, subProps) = dep.DirectOrRelativeDeps.GetOrAdd(fromPropName, (path) => (new HashSet<EventHandler>(), new HashSet<(ProjectNodeInfo, string prop)>()));
            foreach (var item in eventHandlers)
            {
                subActions.Add(item);
            }
        }               
    }

}
