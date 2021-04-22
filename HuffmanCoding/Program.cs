using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HuffmanCoding
{
    class Program
    {
        // https://www.programiz.com/dsa/huffman-coding

        static void Main(string[] args)
        {
            var bytes = System.IO.File.ReadAllBytes("./words.txt");
            var bytesFreq = new Dictionary<byte, int>();

            foreach(var b in bytes)
            {
                if (!bytesFreq.ContainsKey(b))
                {
                    bytesFreq.Add(b, 0);
                }

                bytesFreq[b] = bytesFreq[b] + 1;
            }

            var flattenDict = bytesFreq.Select(dictEntry => new HuffmanEntry<byte>()
            {
                value = dictEntry.Key,
                frequency = dictEntry.Value
            }).ToList();

            flattenDict.Sort((a,b) => a.value - b.value);

            foreach (var entry in flattenDict)
            {
                Console.WriteLine("byte " + Convert.ToChar(entry.value) + " appear " + entry.frequency + " times");
            }

            Console.WriteLine("----------");

            var tree = HuffmanTree<byte>.BuildTree(flattenDict);
            var table = HuffmanTable<byte>.BuildTableFromTree(tree);

            var flattenTable = table.Table.Select(entry =>
            {
                return new Tuple<byte, List<HuffmanPath.Path>>(entry.Key, entry.Value);
            }
            ).ToList();

            flattenTable.Sort((a, b) => a.Item1 - b.Item1);

            foreach (var entry in flattenTable)
            {
                var valAndNbBytes = HuffmanPathToValueAndNbBits(entry.Item2);
                var encodedValue = valAndNbBytes.Item1;
                var nbBits = valAndNbBytes.Item2;
                //Console.WriteLine(encodedValue + " " + Convert.ToString((int)encodedValue, 2) + " " + nbBits);
                Console.Write("byte " + Convert.ToChar(entry.Item1) + " has a sequence of " + entry.Item2.Count + " and is encoded ");
                for (var i = nbBits - 1; i >= 0; i--)
                {
                    var bitIndex = 1 << i;
                    var value = encodedValue & bitIndex;
                    var toPrint = value != 0 ? "1" : "0";
                    Console.Write(toPrint);
                }
                Console.WriteLine("");
            }
            
            Console.ReadLine();
        }

        static Tuple<int, int> HuffmanPathToValueAndNbBits(List<HuffmanPath.Path> path)
        {
            int value = 0;
            int nbBits = 0;

            foreach(var dir in path)
            {
                value = value << 1;
                var newBit = dir == HuffmanPath.Path.Left ? 0 : 1;
                value = value | newBit;
                nbBits += 1;
            }
            return new Tuple<int, int>(value, nbBits);
        }
    }
}
