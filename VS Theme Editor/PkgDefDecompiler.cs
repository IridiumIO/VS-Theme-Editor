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

    private const string FilePath = "pinkcandy.pkgdef";

    private static Regex sectionHeader = new(@"^\[\$RootKey\$\\Themes\\\{.+?\}\\(?<Category>.+?)\]", RegexOptions.Compiled);
    private static Regex dataLine = new("^\"Data\"=hex:(?<HexData>.+)$", RegexOptions.Compiled);

    public Theme Decompile()
    {
        var fileLines = readFile();
        var themeHeader = getThemeHeader(fileLines);
        
        var pkgDefEntries = getPkgDefEntries(fileLines);

        List<CategoryData> categories = new List<CategoryData>();
        foreach (var entry in pkgDefEntries)
        {
            var categoryData = ParseCategoryData(entry);
            categories.Add(categoryData);
        }

        Theme theme = new Theme
        {
            Categories = categories
        };
        if (themeHeader.Count > 0)
        {
            theme.Guid = new Guid(themeHeader[0].Substring(19,36));
            theme.Name = themeHeader[2].Split('=')[1].Trim('"');
            theme.Slug = themeHeader[1].Split('=')[1].Trim('"');
            theme.Fallback = themeHeader[3].Split('=')[1].Trim('"');
        }

        return theme;

    }


    private string[] readFile()
    {
        return System.IO.File.ReadAllLines(FilePath);
    }


    private List<string> getThemeHeader(string[] lines)
    {
        var header = new List<string>();

        if (lines[1].StartsWith("@"))
        {
            header = lines[0..4].ToList();
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
        return colorType is 1 or 2 or 3 or 4; // CT_RAW, CT_SYSCOLOR, CT_AUTOMATIC, CT_COLORINDEX
    }

    static string FormatArgb(uint abgr)
    {
        return $"#{(abgr >> 24) & 0xFF:X2}{abgr & 0xFF:X2}{(abgr >> 8) & 0xFF:X2}{(abgr >> 16) & 0xFF:X2}";
    }



    private record PkgDefEntry (string SectionHeader, byte[] DataLine);


}
