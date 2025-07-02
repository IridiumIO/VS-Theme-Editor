using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VS_Theme_Editor;

internal class PkgDefDecompiler
{

    private static Regex sectionHeader = new(@"^\[\$RootKey\$\\Themes\\\{.+?\}\\(?<Category>.+?)\]", RegexOptions.Compiled);
    private static Regex dataLine = new("^\"Data\"=hex:(?<HexData>.+)$", RegexOptions.Compiled);

    public Theme Decompile(string FilePath)
    {
        var fileLines = readFile(FilePath);
        var themeHeader = getThemeHeader(fileLines);
        
        var pkgDefEntries = getPkgDefEntries(fileLines);

        List<CategoryData> categories = new List<CategoryData>();
        foreach (var entry in pkgDefEntries)
        {
            var categoryData = ParseCategoryData(entry);
            categories.Add(categoryData);
        }

        Theme theme = new Theme();

        foreach (var categoryData in categories) {
            theme.Categories.Add(categoryData);
        }

        if (themeHeader.TryGetValue("@", out var slug))
            theme.Slug = slug;
        if (themeHeader.TryGetValue("Name", out var name))
            theme.Name = name;
        if (themeHeader.TryGetValue("FallbackId", out var fallback))
            theme.Fallback = fallback;
        if (fileLines.Length > 0)
        {
            // Extract Guid from the section header line
            var headerLine = fileLines.FirstOrDefault(l => l.StartsWith("[$RootKey$\\Themes", StringComparison.OrdinalIgnoreCase));
            if (headerLine != null)
            {
                var guidMatch = Regex.Match(headerLine, @"\{([0-9a-fA-F\-]+)\}");
                if (guidMatch.Success)
                    theme.Guid = Guid.Parse(guidMatch.Groups[1].Value);
            }
        }

        return theme;

    }


    private string[] readFile(string filepath)
    {
        return System.IO.File.ReadAllLines(filepath);
    }


    private Dictionary<string, string> getThemeHeader(string[] lines)
    {
        var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Find the section header (first line)
        int i = 0;
        while (i < lines.Length && !lines[i].StartsWith("[$RootKey$\\Themes", StringComparison.OrdinalIgnoreCase))
            i++;

        if (i >= lines.Length)
            return header; // No header found

        // Parse lines after the section header until a blank line or next section
        i++;
        for (; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                break;

            // Match key-value pairs: @="...", "Name"="...", "FallbackId"="..."
            var match = Regex.Match(line, @"^(?<key>@|""[^""]+"")\s*=\s*""(?<value>.*)""$");
            if (match.Success)
            {
                var key = match.Groups["key"].Value.Trim('"');
                var value = match.Groups["value"].Value;
                header[key] = value;
            }
        }

        return header;
    }


    // read the pkgdef file and return a list of PkgDefEntry objects
    private List<PkgDefEntry> getPkgDefEntries(string[] lines)
    {
        var entries = new List<PkgDefEntry>();
        string currentSection = string.Empty;
        foreach (var line in lines)
        {
            if (sectionHeader.IsMatch(line))
            {
                currentSection = sectionHeader.Match(line).Groups["Category"].Value;
            }
            else if (dataLine.IsMatch(line))
            {
                var data = dataLine.Match(line).Groups["HexData"].Value;

                // Convert hex string to byte array
                var hexData = data.Split(',')
                                  .Select(hex => Convert.ToByte(hex.Trim(), 16))
                                  .ToArray();

                entries.Add(new PkgDefEntry(currentSection, hexData));
            }
        }
        return entries;
    }


    private CategoryData ParseCategoryData(PkgDefEntry entry)
    {

        var result = new List<ThemeColorEntry>();

        //start reading bytes
        using MemoryStream ms = new MemoryStream(entry.DataLine);
        using BinaryReader reader = new BinaryReader(ms);

        // Read the first 4 bytes to get the length of the data
        int dataLength = reader.ReadInt32();

        // Next 4 bytes are the header length. Always 11?
        int headerLength = reader.ReadInt32();

        // Next 4 bytes are the number of categories. Always 1?
        int categoryCount = reader.ReadInt32();

        // Next 16 bytes are the Guid
        Guid guid = new Guid(reader.ReadBytes(16));

        // Next 4 bytes are the number of entries
        int entryCount = reader.ReadInt32();


        for (int i = 0; i < entryCount; i++)
        {

            //if (i == 80)
            //{
            //    Debug.WriteLine("Trouble");
            //    // Get the remaining hex data in hex as a string for debugging
            //    var current = ms.Position;
            //    string remainingHex = BitConverter.ToString(reader.ReadBytes((int)(ms.Length - ms.Position))).Replace("-", ", ");
            //    Debug.WriteLine($"Remaining Hex Data: {remainingHex}");

            //    ms.Seek(current, SeekOrigin.Begin);

            //}

            int nameLength = reader.ReadInt32();
            string name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

            byte bgType = reader.ReadByte();
            string bg = null;
            if (IsValidColorType(bgType))
            {
                uint argb = reader.ReadUInt32();
                bg = FormatArgb(argb);
            }

            byte fgType = reader.ReadByte();
            string fg = null;
            if (IsValidColorType(fgType))
            {
                uint argb = reader.ReadUInt32();
                fg = FormatArgb(argb);
            }

            result.Add(new ThemeColorEntry
            {
                Name = name,
                BackgroundType = bgType,
                Background = bg,
                ForegroundType = fgType,
                Foreground = fg
            });
        }
       
        CategoryData categoryData = new CategoryData
        {
            Name = entry.SectionHeader,
            DataLength = dataLength,
            HeaderLength = headerLength,
            CategoryCount = categoryCount,
            Guid = guid,
            EntryCount = entryCount,
            Entries = result
        };
        
        return categoryData;

    }

    static bool IsValidColorType(byte colorType)
    {
        return colorType is 1 or 2 or 3 or 4 or 5 or 6 or 7; // CT_RAW, CT_SYSCOLOR, CT_AUTOMATIC, CT_COLORINDEX, CT_AUTOMATIC, CT_TRACK_FOREGROUND, CT_TRACK_BACKGROUND
    }

    static string FormatArgb(uint abgr)
    {
        var color = $"#{(abgr >> 24) & 0xFF:X2}{abgr & 0xFF:X2}{(abgr >> 8) & 0xFF:X2}{(abgr >> 16) & 0xFF:X2}";

        return color;
    }



    private record PkgDefEntry (string SectionHeader, byte[] DataLine);


}
