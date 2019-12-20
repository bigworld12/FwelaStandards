using System.ComponentModel;
using System.Linq;

namespace FwelaStandards.ProjectComposition
{
    public class NodeDictionaryList : DictionaryList<ProjectNodeInfo>
    {
        public ProjectNodeInfo Owner { get; }
        public NodeDictionaryList(ProjectNodeInfo owner)
        {
            Owner = owner;
            CollectionChanged += NodeDictionaryList_CollectionChanged;
            PreReset += NodeDictionaryList_PreReset;
        }

        private void NodeDictionaryList_PreReset(object sender, System.EventArgs e)
        {
            foreach (var item in AsList)
            {
                item.Part.PropertyChanged -= Item_PropertyChanged;
            }
        }

        private void NodeDictionaryList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems.Cast<ProjectNodeInfo>())
                        {
                            item.Part.PropertyChanged += Item_PropertyChanged;
                        }
                    }
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems.Cast<ProjectNodeInfo>())
                        {
                            item.Part.PropertyChanged -= Item_PropertyChanged;
                        }
                    }
                    break;                
                default:
                    break;
            }

        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is IProjectPart component)
            {
                ItemsComponentPropertyChanged?.Invoke(Owner, component, e);
            }
        }
        /// <summary>
        /// Occurs when the child component property changes
        /// </summary>
        public event NodeListItemPropertyChanged? ItemsComponentPropertyChanged;
    }

}
