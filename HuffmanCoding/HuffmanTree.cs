using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HuffmanCoding
{
    public class HuffmanTree<T>
    {
        public HuffmanBinaryNode<T> Root;

        public static HuffmanTree<T> BuildTree(List<HuffmanEntry<T>> items)
        {
            var nodes = items.Select(a => new HuffmanBinaryNode<T>()
            {
                Weight = a.Frequency,
                Value = a.Value,
            }).ToList();

            SortNodes(nodes);

            while (nodes.Count >= 2)
            {
                var nodeLeft = nodes[0];
                var nodeRight = nodes[1];
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);

                var newNode = new HuffmanBinaryNode<T>()
                {
                    LeftNode = nodeLeft,
                    RightNode = nodeRight,
                    Weight = nodeLeft.Weight + nodeRight.Weight
                };

                nodes.Add(newNode);
                SortNodes(nodes);
            }

            return new HuffmanTree<T>()
            {
                Root = nodes[0]
            };
        }

        private static void SortNodes(List<HuffmanBinaryNode<T>> nodes)
        {
            nodes.Sort((a, b) => 
                {
                    var diff = (a.Weight - b.Weight);
                    return (int) diff;
                }
            );
        }
    }

    public class HuffmanBinaryNode<T>
    {
        public BigInteger Weight;
        public HuffmanBinaryNode<T> LeftNode;
        public HuffmanBinaryNode<T> RightNode;
        public T Value;
    }

    public class HuffmanEntry<T>
    {
        public T Value;
        public BigInteger Frequency;
    }

    public class HuffmanPath
    {
        public enum Path
        {
            Left,
            Right,
        }
    }

    public class HuffmanTable<T>
    {
        public Dictionary<T, List<HuffmanPath.Path>> Table = new Dictionary<T, List<HuffmanPath.Path>>();

        public static HuffmanTable<T> BuildTableFromTree(HuffmanTree<T> tree)
        {
            var table = new HuffmanTable<T>();
            table.Step(tree.Root, new List<HuffmanPath.Path>());
            return table;
        }

        void Step(HuffmanBinaryNode<T> currentNode, List<HuffmanPath.Path> currentPath)
        {
            if (currentNode.LeftNode == null)
            {
                Table.Add(currentNode.Value, currentPath);
            }
            else
            {
                var leftList = new List<HuffmanPath.Path>(currentPath);
                leftList.Add(HuffmanPath.Path.Left);
                Step(currentNode.LeftNode, leftList);

                var rightList = new List<HuffmanPath.Path>(currentPath);
                rightList.Add(HuffmanPath.Path.Right);
                Step(currentNode.RightNode, rightList);
            }
        }
    }
}
