using System;
using System.Collections.Generic;
using System.Linq;

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
                return new Tuple<byte, List<HuffmanTable<byte>.HuffmanPath>>(entry.Key, entry.Value);
            }
            ).ToList();

            flattenTable.Sort((a, b) => a.Item1 - b.Item1);

            foreach (var entry in flattenTable)
            {
                Console.WriteLine("byte " + Convert.ToChar(entry.Item1) + " has a sequence of " + entry.Item2.Count);
            }

            Console.ReadLine();
        }
    }
}
