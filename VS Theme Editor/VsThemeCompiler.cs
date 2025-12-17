using System.Globalization;
using System.Text;
using System.Windows.Media;
using System.Xml;

namespace VS_Theme_Editor;

public class VsThemeCompiler
{
    public void Compile(Theme theme, string filePath)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true
        };

        using var writer = XmlWriter.Create(filePath, settings);

        writer.WriteStartElement("Themes");

        {
            writer.WriteStartElement("Theme");
            writer.WriteAttributeString("Name", theme.Name);
            writer.WriteAttributeString("GUID", theme.Guid?.ToString("B", CultureInfo.InvariantCulture));
            // writer.WriteAttributeString("BaseGUID", theme.BaseGuid?.ToString("B", CultureInfo.InvariantCulture));

            // sorting for easier comparison
            foreach (var category in theme.Categories.OrderBy(x => x.Name))
            {
                writer.WriteStartElement("Category");
                writer.WriteAttributeString("Name", category.Name);
                writer.WriteAttributeString("GUID", category.Guid.ToString("B", CultureInfo.InvariantCulture));

                // sorting for easier comparison
                foreach (var color in category.Entries.OrderBy(x => x.Name))
                {
                    writer.WriteStartElement("Color");
                    writer.WriteAttributeString("Name", color.Name);

                    {
                        var (bgSave, bgType) = GetColorTypeSaveTuple(color.BackgroundType);
                        var (bgValid, bgHex) = GetHexColor(color.Background);
                        if (bgSave && bgValid)
                        {
                            writer.WriteStartElement("Background");
                            writer.WriteAttributeString("Type", bgType);
                            writer.WriteAttributeString("Source", bgHex);
                            writer.WriteEndElement(); // end Background
                        }
                    }

                    {
                        var (fgSave, fgType) = GetColorTypeSaveTuple(color.ForegroundType);
                        var (fgValid, fgHex) = GetHexColor(color.Foreground);
                        if (fgSave && fgValid)
                        {
                            writer.WriteStartElement("Foreground");
                            writer.WriteAttributeString("Type", fgType);
                            writer.WriteAttributeString("Source", fgHex);
                            writer.WriteEndElement(); // end Foreground
                        }
                    }

                    writer.WriteEndElement(); // end Color
                }

                writer.WriteEndElement(); // end Category
            }

            writer.WriteEndElement(); // end Theme
        }

        writer.WriteEndElement(); // end Themes

        writer.Flush(); // Ensure all data is written to the underlying stream
    }

    // I thought to create enum __VSCOLORTYPE : byte and use it, but saw in other places that we use just magic numbers, so decided just use this code, so you'll refactor it later
    // create enum - to avoid referencing Microsoft.VisualStudio.Interop
    // https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.shell.interop.__vscolortype?view=visualstudiosdk-2022
    public static (bool save, string type) GetColorTypeSaveTuple(byte type)
    {
        return type switch
        {
            0 => (false, "CT_INVALID"),
            1 => (true, "CT_RAW"),
            2 => (true, "CT_COLORINDEX"),
            3 => (true, "CT_SYSCOLOR"),
            4 => (true, "CT_VSCOLOR"),
            5 => (true, "CT_AUTOMATIC"),
            6 => (true, "CT_TRACK_FOREGROUND"),
            7 => (true, "CT_TRACK_BACKGROUND"),
            _ => (false, "CT_INVALID"),
        };
    }

    // again, I don't want to refactor/touch anything else right now
    private static (bool valid, string hex) GetHexColor(string color)
    {
        // Accepts "#AARRGGBB" or "AARRGGBB" and returns in last format
        if (string.IsNullOrWhiteSpace(color))
            return (false, "");
        var hex = color.TrimStart('#');
        if (hex.Length != 8)
        {
            var test = (Color)ColorConverter.ConvertFromString(color); // Try to parse as ARGB hex string
            if (test.ToString().Length != 8) return (false, "");
            hex = test.ToString();
        }

        // Parse as ARGB
        if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint argb))
            return (false, "");

        return (true, hex);
    }
}