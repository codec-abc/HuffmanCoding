using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace HuffmanCoding
{
    public class HuffmanTree<T>
    {
        public HuffmanBinaryNode<T> root;

        public static HuffmanTree<T> BuildTree(List<HuffmanEntry<T>> items)
        {
            var nodes = items.Select(a => new HuffmanBinaryNode<T>()
            {
                weight = a.frequency,
                Value = a.value,
            }).ToList();

            sortNodes(nodes);

            while (nodes.Count >= 2)
            {
                var nodeLeft = nodes[0];
                var nodeRight = nodes[1];
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                var newNode = new HuffmanBinaryNode<T>()
                {
                    leftNode = nodeLeft,
                    rightNode = nodeRight,
                    weight = nodeLeft.weight + nodeRight.weight
                };

                nodes.Add(newNode);
                sortNodes(nodes);
            }

            return new HuffmanTree<T>()
            {
                root = nodes[0]
            };
        }

        private static void sortNodes(List<HuffmanBinaryNode<T>> nodes)
        {
            nodes.Sort((a, b) => 
                {
                    var diff = (a.weight - b.weight);
                    return (int) diff;
                }
            );
        }
    }

    public class HuffmanBinaryNode<T>
    {
        public BigInteger weight;
        public HuffmanBinaryNode<T> leftNode;
        public HuffmanBinaryNode<T> rightNode;
        public T Value;
    }

    public class HuffmanEntry<T>
    {
        public T value;
        public BigInteger frequency;
    }

    public class HuffmanTable<T>
    {
        public enum HuffmanPath
        {
            Left,
            Right,
        }

        public Dictionary<T, List<HuffmanPath>> Table = new Dictionary<T, List<HuffmanPath>>();

        public static HuffmanTable<T> BuildTableFromTree(HuffmanTree<T> tree)
        {
            var table = new HuffmanTable<T>();
            table.Step(tree.root, new List<HuffmanPath>());
            return table;
        }

        void Step(HuffmanBinaryNode<T> currentNode, List<HuffmanPath> currentPath)
        {
            if (currentNode.leftNode == null)
            {
                Table.Add(currentNode.Value, currentPath);
            }
            else
            {
                var leftList = new List<HuffmanPath>(currentPath);
                leftList.Add(HuffmanPath.Left);
                Step(currentNode.leftNode, leftList);

                var rightList = new List<HuffmanPath>(currentPath);
                rightList.Add(HuffmanPath.Right);
                Step(currentNode.rightNode, rightList);
            }
        }
    }
}
