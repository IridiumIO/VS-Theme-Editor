using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace VS_Theme_Editor;

internal class PkgDefCompiler
{
    public void Compile(Theme theme, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // Write theme header
        if (theme.Guid == null || theme.Name == null || theme.Slug == null )
            throw new InvalidOperationException("Theme header properties must not be null.");

        writer.WriteLine($"[$RootKey$\\Themes\\{{{theme.Guid}}}]");
        writer.WriteLine($"@=\"{theme.Slug}\"");
        writer.WriteLine($"\"Name\"=\"{theme.Name}\"");
        writer.WriteLine(theme.Fallback is not null ? $"\"FallbackId\"=\"{theme.Fallback}\"" : "");
        writer.WriteLine();

        // Write each category
        foreach (var category in theme.Categories)
        {
            writer.WriteLine($"[$RootKey$\\Themes\\{{{theme.Guid}}}\\{category.Name}]");
            writer.WriteLine($"\"Data\"=hex:{SerializeCategoryData(category)}");
            writer.WriteLine();
        }
    }

    private string SerializeCategoryData(CategoryData category)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Calculate entry count
        int entryCount = category.Entries.Count;

        // Calculate header length (fixed: 4+4+4+16+4 = 32 bytes)
        int headerLength = 32;

        // Calculate total data length
        int dataLength = headerLength;
        foreach (var entry in category.Entries)
        {
            int nameBytesLength = Encoding.UTF8.GetByteCount(entry.Name ?? "");
            // Each entry: name length (4) + name bytes + bgType (1) + [bg (4)?] + fgType (1) + [fg (4)?]
            dataLength += 4 + nameBytesLength + 1;
            if (TryParseArgb(entry.Background, out _))
                dataLength += 4;
            dataLength += 1;
            if (TryParseArgb(entry.Foreground, out _))
                dataLength += 4;
        }

        // Write header with recalculated values
        writer.Write(dataLength);
        writer.Write(11);
        writer.Write(category.CategoryCount); // You may want to recalculate this as well if needed
        writer.Write(category.Guid.ToByteArray());
        writer.Write(entryCount);

        // Write entries
        foreach (var entry in category.Entries)
        {
            var nameBytes = Encoding.UTF8.GetBytes(entry.Name ?? "");
            writer.Write(nameBytes.Length);
            writer.Write(nameBytes);

            if (TryParseArgb(entry.Background, out uint bgArgb))
            { 
                if (entry.BackgroundType == 0)
                {
                    writer.Write((byte)1);

                }
                else
                {
                    writer.Write((byte)entry.BackgroundType);
                }
                    writer.Write(bgArgb); 
            }else
            {
                writer.Write((byte)0); // No background color
            }

            if (TryParseArgb(entry.Foreground, out uint fgArgb))
            {
                if (entry.ForegroundType == 0)
                {
                    writer.Write((byte)1);

                }
                else
                {
                    writer.Write((byte)entry.ForegroundType);
                }
                writer.Write(fgArgb);
            }else
            {
                writer.Write((byte)0); // No foreground color
            }
        }

        // Convert to hex string
        return BitConverter.ToString(ms.ToArray()).Replace("-", ",").ToLower();
    }

    private static bool IsValidColorType(byte colorType)
    {
        return colorType is 1 or 2 or 3 or 4;
    }

    private static bool TryParseArgb(string color, out uint abgr)
    {
        // Accepts "#AARRGGBB" or "AARRGGBB" and converts to ABGR uint
        abgr = 0;
        if (string.IsNullOrWhiteSpace(color))
            return false;
        var hex = color.TrimStart('#');
        if (hex.Length != 8)
        {
            Color test = (Color)ColorConverter.ConvertFromString(color); // Try to parse as ARGB hex string
            if (test.ToString().Length != 8) return false;
            hex = test.ToString();
        }

        // Parse as ARGB
        if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
            return false;

        // ARGB: AA RR GG BB
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);

        // ABGR: AA BB GG RR
        abgr = ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
        return true;
    }
}