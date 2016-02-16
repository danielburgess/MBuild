using System;
using System.Collections.Generic;
using System.IO;
using Extensions;

public class LZSS
    {
        /* Decompress */
        public static MemoryStream Decompress(byte[] data)
        {
            try
            {
                // Compressed & Decompressed Data Information
                var compressedSize = (uint)data.Length;
                var decompressedSize = (uint)(data[0] + data[1] * 256);

                uint sourcePointer = 0x4;
                uint destPointer = 0x0;

                var compressedData = data;
                var decompressedData = new byte[decompressedSize];

                // Start Decompression
                while (sourcePointer < compressedSize && destPointer < decompressedSize)
                {
                    var flag = compressedData[sourcePointer]; // Compression Flag
                    sourcePointer++;

                    for (var i = 7; i >= 0; i--)
                    {
                        if ((flag & (1 << i)) == 0) // Data is not compressed
                        {
                            decompressedData[destPointer] = compressedData[sourcePointer];
                            sourcePointer++;
                            destPointer++;
                        }
                        else // Data is compressed
                        {
                            var distance = (((compressedData[sourcePointer] & 0xF) << 8) | compressedData[sourcePointer + 1]) + 1;
                            var amount = (compressedData[sourcePointer] >> 4) + 3;
                            sourcePointer += 2;

                            // Copy the data
                            for (var j = 0; j < amount; j++)
                                decompressedData[destPointer + j] = decompressedData[destPointer - distance + j];
                            destPointer += (uint)amount;
                        }

                        // Check for out of range
                        if (sourcePointer >= compressedSize || destPointer >= decompressedSize)
                            break;
                    }
                }

                return new MemoryStream(decompressedData);
            }
            catch
            {
                return null; // An error occured while decompressing
            }
        }

        /* Compress */
        public static MemoryStream Compress(ref Stream data, string filename)
        {
            try
            {
                var decompressedSize = (uint)data.Length;

                var compressedData = new MemoryStream();
                byte[] decompressedData = data.ToByteArray();

                uint sourcePointer = 0x0;
                uint destPointer = 0x4;

                // Test if the file is too large to be compressed
                if (data.Length > 0xFFFFFF)
                    throw new Exception("Input file is too large to compress.");

                // Set up the Lz Compression Dictionary
                var lzDictionary = new LzWindowDictionary();
                lzDictionary.SetWindowSize(0x1000);
                lzDictionary.SetMaxMatchAmount(0xF + 3);

                // Start compression
                compressedData.Write('\x10' | (decompressedSize << 8));
                while (sourcePointer < decompressedSize)
                {
                    byte flag = 0x0;
                    var flagPosition = destPointer;
                    compressedData.WriteByte(flag); // It will be filled in later
                    destPointer++;

                    for (var i = 7; i >= 0; i--)
                    {
                        var lzSearchMatch = lzDictionary.Search(decompressedData, sourcePointer, decompressedSize);
                        if (lzSearchMatch[1] > 0) // There is a compression match
                        {
                            flag |= (byte)(1 << i);

                            compressedData.WriteByte((byte)((((lzSearchMatch[1] - 3) & 0xF) << 4) | (((lzSearchMatch[0] - 1) & 0xFFF) >> 8)));
                            compressedData.WriteByte((byte)((lzSearchMatch[0] - 1) & 0xFF));

                            lzDictionary.AddEntryRange(decompressedData, (int)sourcePointer, lzSearchMatch[1]);
                            lzDictionary.SlideWindow(lzSearchMatch[1]);

                            sourcePointer += (uint)lzSearchMatch[1];
                            destPointer += 2;
                        }
                        else // There wasn't a match
                        {
                            flag |= (byte)(0 << i);

                            compressedData.WriteByte(decompressedData[sourcePointer]);

                            lzDictionary.AddEntry(decompressedData, (int)sourcePointer);
                            lzDictionary.SlideWindow(1);

                            sourcePointer++;
                            destPointer++;
                        }

                        // Check for out of bounds
                        if (sourcePointer >= decompressedSize)
                            break;
                    }

                    // Write the flag.
                    // The original position gets reset after writing.
                    compressedData.Seek(flagPosition, SeekOrigin.Begin);
                    compressedData.WriteByte(flag);
                    compressedData.Seek(destPointer, SeekOrigin.Begin);
                }

                return compressedData;
            }
            catch
            {
                return null; // An error occured while compressing
            }
        }

        // Check
        public static bool Check(ref Stream data, string filename)
        {
            try
            {
                // Because this can conflict with other compression formats we are going to add a check them too
                return (data.ReadString(0x0, 1) == "\x10"); //&&
                //!Compression.Dictionary[CompressionFormat.PRS].Check(ref data, filename) &&
                //!Images.Dictionary[GraphicFormat.PVR].Check(ref data, filename));
            }
            catch
            {
                return false;
            }
        }
    }

class LzWindowDictionary
{
    int _windowSize = 0x1000;
    int _windowStart;
    int _windowLength;
    int _minMatchAmount = 3;
    int _maxMatchAmount = 18;
    int _blockSize;
    readonly List<int>[] _offsetList;

    public LzWindowDictionary()
    {
        // Build the offset list, so Lz compression will become significantly faster
        _offsetList = new List<int>[0x100];
        for (var i = 0; i < _offsetList.Length; i++)
            _offsetList[i] = new List<int>();
    }

    public int[] Search(byte[] decompressedData, uint offset, uint length)
    {
        RemoveOldEntries(decompressedData[offset]); // Remove old entries for this index

        if (offset < _minMatchAmount || length - offset < _minMatchAmount) // Can't find matches if there isn't enough data
            return new[] { 0, 0 };

        // Start finding matches
        var match = new[] { 0, 0 };

        for (var i = _offsetList[decompressedData[offset]].Count - 1; i >= 0; i--)
        {
            var matchStart = _offsetList[decompressedData[offset]][i];
            var matchSize = 1;

            while (matchSize < _maxMatchAmount && matchSize < _windowLength && matchStart + matchSize < offset && offset + matchSize < length && decompressedData[offset + matchSize] == decompressedData[matchStart + matchSize])
                matchSize++;

            if (matchSize >= _minMatchAmount && matchSize > match[1]) // This is a good match
            {
                match = new[] { (int)(offset - matchStart), matchSize };

                if (matchSize == _maxMatchAmount) // Don't look for more matches
                    break;
            }
        }

        // Return the match.
        // If no match was made, the distance & length pair will be zero
        return match;
    }

    // Slide the window
    public void SlideWindow(int amount)
    {
        if (_windowLength == _windowSize)
            _windowStart += amount;
        else
        {
            if (_windowLength + amount <= _windowSize)
                _windowLength += amount;
            else
            {
                amount -= (_windowSize - _windowLength);
                _windowLength = _windowSize;
                _windowStart += amount;
            }
        }
    }

    // Slide the window to the next block
    public void SlideBlock()
    {
        _windowStart += _blockSize;
    }

    // Remove old entries
    private void RemoveOldEntries(byte index)
    {
        for (var i = 0; i < _offsetList[index].Count; ) // Don't increment i
        {
            if (_offsetList[index][i] >= _windowStart)
                break;
            _offsetList[index].RemoveAt(0);
        }
    }

    // Set variables
    public void SetWindowSize(int size)
    {
        _windowSize = size;
    }
    public void SetMinMatchAmount(int amount)
    {
        _minMatchAmount = amount;
    }
    public void SetMaxMatchAmount(int amount)
    {
        _maxMatchAmount = amount;
    }
    public void SetBlockSize(int size)
    {
        _blockSize = size;
        _windowLength = size; // The window will work in blocks now
    }

    // Add entries
    public void AddEntry(byte[] decompressedData, int offset)
    {
        _offsetList[decompressedData[offset]].Add(offset);
    }
    public void AddEntryRange(byte[] decompressedData, int offset, int length)
    {
        for (int i = 0; i < length; i++)
            AddEntry(decompressedData, offset + i);
    }
}

