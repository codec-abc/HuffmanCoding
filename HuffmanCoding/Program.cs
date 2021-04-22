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
            Console.WriteLine("Hello World!");

            var bytes = System.IO.File.ReadAllBytes("./words.txt");
            var count = new Dictionary<byte, int>();

            foreach(var b in bytes)
            {
                if (!count.ContainsKey(b))
                {
                    count.Add(b, 0);
                }

                count[b] = count[b] + 1;
            }

            var flatDict = count.Select(a => new Tuple<byte, int>(a.Key, a.Value)).ToList();

            flatDict.Sort((a, b) => a.Item2 - b.Item2);

            //foreach(var entry in flatDict)
            //{
            //    Console.WriteLine("byte " + entry.Item1 + " appears " + entry.Item2);
            //}

            Console.ReadLine();
        }
    }
}
