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
            //Console.WriteLine("Done, press enter to quit");
            //Console.ReadLine();
        }

        private static void RunCompression(string inputFile, string outputFile, string outputTableFile)
        {
            var bytes = File.ReadAllBytes(inputFile);
            var bytesFreq = new Dictionary<byte, ulong>();

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
                Value = dictEntry.Key,
                Frequency = dictEntry.Value
            }).ToList();

            flattenDict.Sort((a, b) => a.Value - b.Value);

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
                var encodedValue = huffmanEntry.EncodedValue;
                var nbBitsEntry = huffmanEntry.NbBits;

                huffmanBinaryTable.Add(entry.Item1, huffmanEntry);
            }
            
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
            int toWrite = 0;
            int nbBits = 0;

            int nbBitsTotal = 0;

            FileStream fs = File.Create(outputFile);
            foreach (var b in bytes)
            {
                var entry = huffmanBinaryTable[b];
                toWrite = toWrite << entry.NbBits;
                toWrite = toWrite + entry.EncodedValue;

                nbBits += entry.NbBits;
                nbBitsTotal += entry.NbBits;
                if (nbBits >= NB_BITS_PACKED)
                {
                    var copy = toWrite;
                    int deltaShift = nbBits - NB_BITS_PACKED;
                    BigInteger shiftBack = copy >> deltaShift;
                    var bytesToWrite = shiftBack.ToByteArray();
                    fs.Write(bytesToWrite, 0, 1);
                    int mask = 0;
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
                var bytesToWrite = (new BigInteger(toWrite)).ToByteArray();
                fs.Write(bytesToWrite, 0, 1);
            }

            fs.Flush();
            fs.Close();

            HuffmanDecodingData data = new HuffmanDecodingData()
            {
                NbElements = bytes.Length,
                Table = huffmanBinaryTable
            };

            var jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(outputTableFile, jsonString);
        }

        static void RunDecompression(string compressedFilePath, string compressedDataFilePath, string outputPath)
        {
            var data = File.ReadAllText(compressedDataFilePath);
            var huffmanData = JsonConvert.DeserializeObject<HuffmanDecodingData>(data);

            var optimizedTable = BuildOptmizedTable(huffmanData);

            var bytesToDecode = File.ReadAllBytes(compressedFilePath);
            var decoded = new List<byte>(huffmanData.NbElements);

            int symbolsRead = 0;
            int startingBitIndex = 0;
            int length = 1;

            while(symbolsRead < huffmanData.NbElements)
            {
                int currentValue = GetNumberFromBitsArray(startingBitIndex, length, bytesToDecode);

                byte byteValue;

                if (IsValidHuffmanEntry(currentValue, length, optimizedTable, out byteValue))
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

        private static Dictionary<int, Dictionary<int, byte>> BuildOptmizedTable(HuffmanDecodingData huffmanData)
        {
            var returned = new Dictionary<int, Dictionary<int, byte>>();

            foreach(var entry in huffmanData.Table)
            {
                if (!returned.ContainsKey(entry.Value.NbBits))
                {
                    returned.Add(entry.Value.NbBits, new Dictionary<int, byte>());
                }

                returned[entry.Value.NbBits].Add(entry.Value.EncodedValue, entry.Key);
            }

            return returned;
        }

        private static int GetNumberFromBitsArray
        (
            int startingBitIndex,
            int length, 
            byte[] bytesToDecode
        )
        {
            var endingBitIndex = startingBitIndex + length;
            int startingByteIndex = (int) startingBitIndex / 8;
            int endingByteIndex = (int) (endingBitIndex) / 8;

            int sequence = 0;

            for (int i = startingByteIndex; i <= endingByteIndex ; i++)
            {
                sequence = sequence << 8;
                sequence = sequence + bytesToDecode[i];
            }

            var rightShift = 8 - (endingBitIndex % 8);

            sequence = sequence >> (int) rightShift;

            int mask = 0;

            if (length < 32)
            {
                uint mask2 = 0xffffffff;
                mask2 >>= (32 - length);
                mask = (int) mask2;
            }
            else
            {
                // Really bad performance
                for (int i = 0; i < length; i++)
                {
                    mask = mask << 1;
                    mask = mask + 1;
                }
            }

            sequence = sequence & mask;

            return sequence;
        }

        private static bool IsValidHuffmanEntry
        (
            int currentValue,
            int nbBits,
            Dictionary<int, Dictionary<int, byte>> huffmanData, 
            out byte byteValue
        )
        {
            byteValue = 0;
            Dictionary<int, byte> innerDict;
            if (huffmanData.TryGetValue(nbBits, out innerDict))
            {
                return innerDict.TryGetValue(currentValue, out byteValue);
            }

            return false;
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
                EncodedValue = value,
                NbBits = nbBits
            };
        }

        class HuffmanTableEntry
        {
            public int NbBits;
            public int EncodedValue;
        }

        class HuffmanDecodingData
        {
            public int NbElements;
            public Dictionary<byte, HuffmanTableEntry> Table;
        }
    }
}
