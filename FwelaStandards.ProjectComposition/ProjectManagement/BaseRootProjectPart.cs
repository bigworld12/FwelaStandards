using Catel.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace FwelaStandards.ProjectComposition
{
    public class BaseRootProjectPart : BaseProjectPart, IRootProjectPart
    {
        public BaseRootProjectPart()
        {
            m_AllNodes = new Dictionary<string, ProjectNodeInfo>();
        }
        private Dictionary<string, ProjectNodeInfo> m_AllNodes;
        public IReadOnlyDictionary<string, ProjectNodeInfo> AllNodes => m_AllNodes;


        //use in list
        public bool SetNewNode(ProjectNodeInfo node, out ProjectNodeInfo? oldNode)
        {
            node.PropertyChanged += Node_PropertyChanged;
            var res = m_AllNodes.TryGetValue(node.FullPath, out oldNode);
            if (res)
            {
                oldNode.PropertyChanged -= Node_PropertyChanged;
            }
            m_AllNodes[node.FullPath] = node;
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

            var res = m_AllNodes.TryGetValue(node.FullPath, out var oldNode);
            if (res == false || !ReferenceEquals(node, oldNode)) //object isn't in the dictionary (already removed)
            {
                return;
            }
            node.PropertyChanged -= Node_PropertyChanged;
            m_AllNodes.Remove(node.FullPath);
        }
        public void SetNodeRename(ProjectNodeInfo node, string oldName)
        {


        }
    }
}
