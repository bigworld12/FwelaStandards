using Catel.Collections;
using Catel.Data;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FwelaStandards.ProjectComposition
{
    public class BaseRootProjectPart : BaseProjectPart, IRootProjectPart
    {
        public BaseRootProjectPart()
        {
            AllNodes = new FastObservableDictionary<string, ProjectNodeInfo>();
        }


        public FastObservableDictionary<string, ProjectNodeInfo> AllNodes { get; }


        //use in list
        public bool SetNewNode(ProjectNodeInfo node, out ProjectNodeInfo? oldNode)
        {
            node.PropertyChanged += Node_PropertyChanged;
            var res = AllNodes.TryGetValue(node.FullPath, out oldNode);
            if (res)
            {
                oldNode.PropertyChanged -= Node_PropertyChanged;
            }
            AllNodes[node.FullPath] = node;
            return res;
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ProjectNodeInfo ni)
            {
                switch (e.PropertyName)
                {
                    case nameof(ni.FullPath):
                        if (e is AdvancedPropertyChangedEventArgs adv)
                        {
                            if (adv.IsOldValueMeaningful && adv.OldValue is string oldFullPath)
                            {
                                SetNodeRename(ni, oldFullPath);
                            }
                            else
                            {
                                throw new InvalidOperationException("Old value was meaningless");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("No advanced property changed was sent");
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        //use in list
        public void SetOldNode(ProjectNodeInfo node)
        {
            var res = AllNodes.TryGetValue(node.FullPath, out var oldNode);
            if (res == false || !ReferenceEquals(node, oldNode)) //object isn't in the dictionary (already removed)
            {
                return;
            }
            node.PropertyChanged -= Node_PropertyChanged;
            AllNodes.Remove(node.FullPath);
        }
        public void SetNodeRename(ProjectNodeInfo node, string oldName)
        {
            AllNodes.Remove(oldName);
            AllNodes[node.FullPath] = node;
        }
    }
}
