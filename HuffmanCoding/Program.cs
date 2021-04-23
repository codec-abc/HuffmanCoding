using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace HuffmanCoding
{
    class Program
    {
        // https://www.programiz.com/dsa/huffman-coding

        static void Main(string[] args)
        {
            CompressFile("./test.txt", "./compressed.bin", "./compressed-table.json");
            Console.WriteLine("Done, press enter to quit");
            Console.ReadLine();
        }

        private static void CompressFile(string inputFile, string outputFile, string outputTableFile)
        {
            var bytes = System.IO.File.ReadAllBytes(inputFile);
            var bytesFreq = new Dictionary<byte, int>();

            foreach (var b in bytes)
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

            flattenDict.Sort((a, b) => a.value - b.value);

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

            var huffmanBinaryTable = new Dictionary<byte, HuffmanTableEntry>();

            foreach (var entry in flattenTable)
            {
                var huffmanEntry = HuffmanPathToValueAndNbBits(entry.Item2);
                var encodedValue = huffmanEntry.value;
                var nbBitsEntry = huffmanEntry.nbBits;
                //Console.WriteLine(encodedValue + " " + Convert.ToString((int)encodedValue, 2) + " " + nbBits);
                Console.Write("byte " + Convert.ToChar(entry.Item1) + " has a sequence of " + entry.Item2.Count + " and is encoded ");
                for (var i = nbBitsEntry - 1; i >= 0; i--)
                {
                    var bitIndex = 1 << i;
                    var value = encodedValue & bitIndex;
                    var toPrint = value != 0 ? "1" : "0";
                    Console.Write(toPrint);
                }
                Console.WriteLine("");

                huffmanBinaryTable.Add(entry.Item1, huffmanEntry);
            }

            Console.WriteLine("----------");

            BigInteger toWrite = 0;
            int nbBits = 0;
            BigInteger nbBitsTotal = 0;

            FileStream fs = File.Create(outputFile);

            foreach (var b in bytes)
            {
                var entry = huffmanBinaryTable[b];
                toWrite = toWrite << entry.nbBits;
                toWrite = toWrite + entry.value;
                nbBits += entry.nbBits;
                nbBitsTotal += entry.nbBits;

                Console.Write("pushing ");

                for (var i = entry.nbBits - 1; i >= 0; i--)
                {
                    var bitIndex = 1 << i;
                    var value = entry.value & bitIndex;
                    var toPrint = value != 0 ? "1" : "0";
                    Console.Write(toPrint);
                }

                Console.WriteLine("");

                //Console.WriteLine(" on " + entry.nbBits + " bits. Total bits is now " + nbBitsTotal);
                Console.Write("toWrite ");
                debugPrintBinary(toWrite);

                if (nbBits >= 8)
                {
                    var copy = toWrite;
                    var deltaShift = nbBits - 8;
                    var shiftBack = copy >> deltaShift;
                    
                    Console.WriteLine("deltaShift " + deltaShift);
                    var bytesToWrite = shiftBack.ToByteArray();

                    Console.Write("Writing on " + bytesToWrite.Length + " byte(s) ");
                    debugPrintBinary(shiftBack);


                    fs.Write(bytesToWrite, 0, 1);
                    var mask = 0;
                    for (int i = 0; i < deltaShift; i++)
                    {
                        mask = mask << 1;
                        mask = mask + 1;
                    }
                    Console.WriteLine("mask " + Convert.ToString(mask, 2).PadLeft(8, '0'));
                    toWrite = toWrite & mask;
                    nbBits = nbBits - 8;
                    Console.Write("toWrite [2] ");
                    debugPrintBinary(toWrite);
                }
            }

            if (nbBits > 0)
            {
                var shift = 64 - nbBits;
                toWrite = toWrite << shift;
                var bytesToWrite = toWrite.ToByteArray();
                Console.WriteLine("writing byte array " + bytesToWrite);
                fs.Write(bytesToWrite, 0, 1);
            }

            fs.Flush();
            fs.Close();

            HuffmanDecodingData data = new HuffmanDecodingData()
            {
                nbBitsWritten = nbBitsTotal,
                table = huffmanBinaryTable
            };

            var jsonString = JsonConvert.SerializeObject(data);
            File.WriteAllText(outputTableFile, jsonString);
        }

        static void debugPrintBinary(BigInteger number)
        {
            var bytesToWrite = number.ToByteArray();
            //Console.WriteLine("bigInte encoded on " + bytesToWrite.Length + " bytes");
            var toWriteDebug = bytesToWrite.Select(a => Convert.ToString(a, 2).PadLeft(8, '0')).ToList();
            toWriteDebug.Reverse();
            var debugPrint = toWriteDebug.Aggregate((a, c) => a + "|" + c);
            Console.WriteLine("debug " + debugPrint);
        }

        static HuffmanTableEntry HuffmanPathToValueAndNbBits(List<HuffmanPath.Path> path)
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

            return new HuffmanTableEntry
            {
                value = value,
                nbBits = nbBits
            };
        }

        class HuffmanTableEntry
        {
            public int nbBits;
            public int value;
        }

        class HuffmanDecodingData
        {
            public BigInteger nbBitsWritten;
            public Dictionary<byte, HuffmanTableEntry> table;
        }
    }
}
