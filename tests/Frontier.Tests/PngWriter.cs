using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using IdleBar.Scene;

namespace IdleBar.Tests;

public static class PngWriter
{
    private static readonly uint[] CrcTable = BuildTable();

    public static void Write(ArtRaster raster, string path, int scale)
    {
        int width = raster.Width * scale;
        int height = raster.Height * scale;
        using MemoryStream pixels = new();
        using (ZLibStream zlib = new(pixels, CompressionLevel.Optimal, true))
        {
            byte[] row = new byte[width * 4 + 1];
            for (int y = 0; y < height; y++)
            {
                row[0] = 0;
                for (int x = 0; x < width; x++)
                {
                    int source = ((y / scale) * raster.Width + x / scale) * 4;
                    bool empty = raster.Data[source + 3] == 0;
                    row[1 + x * 4] = empty ? (byte)17 : raster.Data[source];
                    row[2 + x * 4] = empty ? (byte)20 : raster.Data[source + 1];
                    row[3 + x * 4] = empty ? (byte)28 : raster.Data[source + 2];
                    row[4 + x * 4] = 255;
                }

                zlib.Write(row);
            }
        }

        using FileStream file = File.Create(path);
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] header = new byte[13];
        WriteBig(header, 0, (uint)width);
        WriteBig(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 6;
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", pixels.ToArray());
        Chunk(file, "IEND", []);
    }

    private static void Chunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4];
        WriteBig(length, 0, (uint)data.Length);
        stream.Write(length);
        byte[] typed = [.. Encoding.ASCII.GetBytes(type), .. data];
        stream.Write(typed);
        byte[] crc = new byte[4];
        WriteBig(crc, 0, Crc(typed));
        stream.Write(crc);
    }

    private static void WriteBig(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Crc(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] BuildTable()
    {
        uint[] table = new uint[256];
        for (uint index = 0; index < 256; index++)
        {
            uint value = index;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }
}
