﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UtilityLib.TreeNodes
{
    public class Tree
    {
        public interface ITreeNode<T>
        {
            T Value { get; set; }
            ITreeNode<T> Parent { get; set; }
            IList<ITreeNode<T>> Children { get; }

            void AddChild(ITreeNode<T> child);
            void RemoveChild(ITreeNode<T> child);
        }

        public interface ITree<T>
        {
            ITreeNode<T> Root { get; set; }

            void Traverse(ITreeNode<T> node, Action<ITreeNode<T>> visit);
        }

        public class TreeNode<T> : ITreeNode<T>
        {
            public T Value { get; set; }
            public ITreeNode<T> Parent { get; set; }
            public IList<ITreeNode<T>> Children { get; private set; }

            public TreeNode(T value)
            {
                Value = value;
                Children = new List<ITreeNode<T>>();
            }

            public void AddChild(ITreeNode<T> child)
            {
                child.Parent = this;
                Children.Add(child);
            }

            public void RemoveChild(ITreeNode<T> child)
            {
                child.Parent = null;
                Children.Remove(child);
            }
        }

        public class Treee<T> : ITree<T>
        {
            public ITreeNode<T> Root { get; set; }

            public Treee(T rootValue)
            {
                Root = new TreeNode<T>(rootValue);
            }

            public void Traverse(ITreeNode<T> node, Action<ITreeNode<T>> visit)
            {
                if (node == null)
                    return;

                visit(node);
                foreach (var child in node.Children)
                {
                    Traverse(child, visit);
                }
            }
        }
    }
}
