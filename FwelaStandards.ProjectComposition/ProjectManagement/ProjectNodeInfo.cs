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
                                    //    if (senderList is IList<T> copyList && senderItem is T component && e.HasPropertyChanged(nameof(component.NodeInfo)) && !(component.NodeInfo is null))
                                    //    {
                                    //        component.PropertyChanged -= NewItemPropertyChanged;
                                    //        ChildrenAsList.Insert(args.NewStartingIndex + i, component.NodeInfo);
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
                                    //    if (senderList is IList<T> copyList && senderItem is T component && e.HasPropertyChanged(nameof(component.NodeInfo)) && !(component.NodeInfo is null))
                                    //    {
                                    //        component.PropertyChanged -= NewItemPropertyChanged;
                                    //        ChildrenAsList[args.NewStartingIndex + i] = component.NodeInfo;
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
        public void RegisterChildPart<T>(T childComponent, string name, bool raisePropertyChangedOnComponent = true) where T : IProjectPart
        {
            childComponent.InitFromParent(this);
            if (childComponent.NodeInfo is null)
            {
                throw new InvalidOperationException("Filed to initialize child component");
            }
            RegisterChildPart(childComponent.NodeInfo, name);
            if (raisePropertyChangedOnComponent)
            {
                Part.RaisePropertyChanged(name);
            }
        }

        public void RegisterListChild<T>(T childComponent) where T : IProjectPart
        {
            childComponent.InitFromParent(this);
            if (childComponent.NodeInfo is null)
            {
                throw new InvalidOperationException("Filed to initialize component");
            }
            ChildrenAsList.Add(childComponent.NodeInfo);
        }
        public void RegisterListChild<T>(IEnumerable<T> childrenComponents) where T : IProjectPart
        {
            foreach (var item in childrenComponents)
            {
                RegisterListChild(item);
            }
        }
        public ProjectNodeInfo(IProjectPart component, ProjectNodeInfo? parent = null)
        {
            Part = component;
            Children = new NodeDictionaryList(this);
            DependencyInfo = new ProjectPartDependency(this);
            PropertyChanged += ProjectNodeInfo_PropertyChanged;
            Children.CollectionChanged += Children_CollectionChanged;
            component.PropertyChanged += Component_PropertyChanged;
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

        private void Component_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
        /// <param name="useBracketsForComponents">set to false to use the <see cref="WalkPathDownFrom"/> method</param>
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
        /*public void RegisterUnitDependency(params string[] toPropNames)
        {
            var pd = GetRootPartAs<IEntityProject>().Details;
            if (pd?.NodeInfo is null)
            {
                throw new InvalidOperationException("Project must be initialized first");
            }
            RegisterDependency(pd.NodeInfo, nameof(pd.UnitSystem), toPropNames);
        }
        public void RegisterUnitAction(params EventHandler[] eventHandlers)
        {
            var pd = GetRootPartAs<IEntityProject>().Details;
            if (pd?.NodeInfo is null)
            {
                throw new InvalidOperationException("Project must be initialized first");
            }
            RegisterAction(pd.NodeInfo, nameof(pd.UnitSystem), eventHandlers);
        }*/
        //when a parent's full path changes, notify its children

        //Error logging
        /*public static void RaiseError(ProjectNodeInfo from, string prop, string message)
        {
            var dep = from.DependencyInfo;
            var errs = dep.Errors.GetOrAdd(prop, (path) => new ObservableCollection<ErrorLog>());
            errs.Add(LogMessageEventArgs message));
        }
        public void RaiseError(string prop, string message)
        {
            RaiseError(this, prop, message);
        }

        public static bool HasErrors(ProjectNodeInfo from, params string[] props)
        {
            var dep = GetDependency(from);
            foreach (var uid in props)
            {
                var list = dep.Errors.GetOrAdd(uid, (path) => new ObservableCollection<ErrorLog>());
                if (list.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasErrors(params string[] props)
        {
            return HasErrors(this, props);
        }
        public bool HasAnyErrors()
        {
            return GetDependency(this).Errors.Any(x => x.Value.Any());
        }


        public static bool CalcIfNoErrors(ProjectNodeInfo from, Action doCalc, IEnumerable<string> uids)
        {
            var uds = uids.ToArray();
            if (uds.Length == 0)
            {
                return from.HasAnyErrors();
            }
            if (uids.Any(x => HasErrors(from, x)))
                return false;
            doCalc();
            return true;
        }
        public bool CalcIfNoErrors(Action doCalc, IEnumerable<string> uids)
        {
            return CalcIfNoErrors(this, doCalc, uids);
        }

        public static bool CalcIfNoErrors(ProjectNodeInfo from, Action doCalc, params string[] uids)
        {
            return CalcIfNoErrors(from, doCalc, uids.AsEnumerable());
        }
        public bool CalcIfNoErrors(Action doCalc, params string[] uids)
        {
            return CalcIfNoErrors(doCalc, uids.AsEnumerable());
        }

        public T CalcIfNoErrors<T>(Func<T> doCalc, T defaultValue = default)
        {
            if (HasAnyErrors())
            {
                return doCalc();
            }
            return defaultValue;
        }


        public static void ResetErrors(ProjectNodeInfo from)
        {
            var dep = GetDependency(from);
            dep.Errors.Values.ForEach(x => x.Clear());
        }
        public void ResetErrors()
        {
            ResetErrors(this);
        }

        public static void ResetErrors(ProjectNodeInfo from, IEnumerable<string> uids)
        {
            var dep = GetDependency(from);

            foreach (var uid in uids)
            {
                var list = dep.Errors.GetOrAdd(uid, (path) => new ObservableCollection<ErrorLog>());
                list.Clear();
            }
        }
        public static void ResetErrors(ProjectNodeInfo from, params string[] uids)
        {
            ResetErrors(from, uids.AsEnumerable());
        }

        public void ResetErrors(IEnumerable<string> uids)
        {
            var dep = GetDependency(this);

            foreach (var uid in uids)
            {
                var list = dep.Errors.GetOrAdd(uid, (path) => new ObservableCollection<ErrorLog>());
                list.Clear();
            }
        }
        public void ResetErrors(params string[] uids)
        {
            ResetErrors(uids.AsEnumerable());
        }

        public bool ResetAndAssertNotDefault(params string[] propNames)
        {
            ResetErrors();
            return AssertNotDefault(propNames);
        }
        public bool AssertNotDefault(params string[] propNames)
        {
            bool hasErrors = false;
            foreach (var propName in propNames)
            {
                var val = GetValue(propName);
                if (val is null || (val is double d && d == 0d) || (val is int i && i == 0))
                {
                    RaiseError(propName, $"{propName} must be assigned");
                    hasErrors = true;
                }
            }
            return hasErrors;
        }
        public bool AssertPositive(params string[] propNames)
        {
            bool hasErrors = false;
            foreach (var propName in propNames)
            {
                var val = GetValue(propName);
                if (!(val is double d) || d <= 0)
                {
                    RaiseError(propName, $"{propName} must be positive");
                    hasErrors = true;
                }
            }
            return hasErrors;
        }



        public bool AssertNot0<T>(string propName, double value)
        {
            if (value == 0)
            { RaiseError(propName, $"{propName} must not be 0"); return false; }
            return true;
        }
        public bool AssertNotNull(string uid, double? value)
        {
            if (!value.HasValue)
            { RaiseError(uid, $"{uid} must have a value"); return false; }
            return true;
        }

        public bool AssertPositive(string uid, double value, string variableName)
        {
            if (!(value > 0))
            {
                RaiseError(uid, $"{variableName} must be positive"); return false;
            }
            return true;
        }
        public bool AssertTrue(string uid, bool value, string msg)
        {
            if (!value)
            {
                RaiseError(uid, msg); return false;
            }
            return true;
        }
        public bool AssertFalse(string path, bool value, string msg)
        {
            if (value)
            {
                RaiseError(path, msg); return false;
            }
            return true;
        }
        */
    }

}
