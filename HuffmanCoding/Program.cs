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

        const int NB_BITS_PACKED = 8;

        static void Main(string[] args)
        {
            RunCompression("./words.txt", "./compressed.bin", "./compressed-table.json");
            RunDecompression("./compressed.bin", "./compressed-table.json", "decompressed.txt");
            Console.WriteLine("Done, press enter to quit");
            Console.ReadLine();
        }

        private static void RunCompression(string inputFile, string outputFile, string outputTableFile)
        {
            var bytes = File.ReadAllBytes(inputFile);
            var bytesFreq = new Dictionary<byte, BigInteger>();

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
            WriteCompressedFiles(outputFile, outputTableFile, bytes, huffmanBinaryTable);
        }

        private static void WriteCompressedFiles
        (
            string outputFile, 
            string outputTableFile, 
            byte[] bytes, 
            Dictionary<byte, HuffmanTableEntry> huffmanBinaryTable
        )
        {
            BigInteger toWrite = 0;
            int nbBits = 0;
  
            BigInteger nbBitsTotal = 0;

            FileStream fs = File.Create(outputFile);
            foreach (var b in bytes)
            {
                var entry = huffmanBinaryTable[b];
                toWrite = toWrite << entry.nbBits;
                toWrite = toWrite + entry.value;

                //Console.WriteLine("writing " + b + " as " + entry.value + " " + Convert.ToString(entry.value, 2).PadLeft(entry.nbBits, '0') + " on " + entry.nbBits + " bits");
                nbBits += entry.nbBits;
                nbBitsTotal += entry.nbBits;
                if (nbBits >= NB_BITS_PACKED)
                {
                    var copy = toWrite;
                    var deltaShift = nbBits - NB_BITS_PACKED;
                    var shiftBack = copy >> deltaShift;
                    var bytesToWrite = shiftBack.ToByteArray();
                    fs.Write(bytesToWrite, 0, 1);
                    var mask = 0;
                    for (int i = 0; i < deltaShift; i++)
                    {
                        mask = mask << 1;
                        mask = mask + 1;
                    }
                    toWrite = toWrite & mask;
                    nbBits = nbBits - NB_BITS_PACKED;
                }
            }

            if (nbBits > 0)
            {
                var shift = NB_BITS_PACKED - nbBits;
                toWrite = toWrite << shift;
                var bytesToWrite = toWrite.ToByteArray();
                fs.Write(bytesToWrite, 0, 1);
            }

            fs.Flush();
            fs.Close();

            HuffmanDecodingData data = new HuffmanDecodingData()
            {
                nbElements = bytes.Length,
                table = huffmanBinaryTable
            };

            var jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(outputTableFile, jsonString);
        }

        static void RunDecompression(string compressedFilePath, string compressedDataFilePath, string outputPath)
        {
            var data = File.ReadAllText(compressedDataFilePath);
            var huffmanData = JsonConvert.DeserializeObject<HuffmanDecodingData>(data);

            var bytesToDecode = File.ReadAllBytes(compressedFilePath);
            var decoded = new List<byte>((int) huffmanData.nbElements);

            BigInteger symbolsRead = 0;
            BigInteger startingBitIndex = 0;
            BigInteger length = 1;

            while(symbolsRead < huffmanData.nbElements)
            {
                BigInteger currentValue = getNumberFromBitsArray(startingBitIndex, length, bytesToDecode);

                byte byteValue;

                if (IsValidHuffmanEntry(currentValue, length, huffmanData, out byteValue))
                {
                    symbolsRead++;
                    decoded.Add(byteValue);
                    startingBitIndex = startingBitIndex + length;
                    length = 1;
                }
                else
                {
                    length++;
                }
            }

            File.WriteAllBytes(outputPath, decoded.ToArray());
        }

        private static BigInteger getNumberFromBitsArray
        (
            BigInteger startingBitIndex, 
            BigInteger length, 
            byte[] bytesToDecode
        )
        {
            int startingByteIndex = (int) startingBitIndex / 8;
            int endingByteIndex = (int) (startingBitIndex + length) / 8;

            BigInteger sequence = 0;

            for (int i = startingByteIndex; i <= endingByteIndex ; i++)
            {
                sequence = sequence << 8;
                sequence = sequence + bytesToDecode[i];
            }

            var rightShift = 8 - ((startingBitIndex + length) % 8);

            sequence = sequence >> (int) rightShift;

            BigInteger mask = 0;

            for (int i = 0; i < length; i++)
            {
                mask = mask << 1;
                mask = mask + 1;
            }

            sequence = sequence & mask;

            return sequence;
        }

        private static bool IsValidHuffmanEntry
        (
            BigInteger currentValue, 
            BigInteger nbBits, 
            HuffmanDecodingData huffmanData, 
            out byte byteValue
        )
        {
            byteValue = 0;
            foreach (var entry in huffmanData.table)
            {
                if (entry.Value.nbBits == nbBits && currentValue == entry.Value.value)
                {
                    byteValue = entry.Key;
                    return true;
                }
            }

            return false;
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
            public BigInteger nbElements;
            public Dictionary<byte, HuffmanTableEntry> table;
        }
    }
}
