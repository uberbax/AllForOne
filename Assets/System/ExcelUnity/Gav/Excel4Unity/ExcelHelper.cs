using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;
using System.IO;
using System.IO.Compression;
using System.Xml;

public class ExcelHelper
{

    public static Excel LoadExcel(string path)
    {
#if UNITY_EDITOR
        FileInfo file = new FileInfo(path);
        ExcelPackage ep = new ExcelPackage(file);

        Excel xls = new Excel(ep.Workbook);
        return xls;
#else
        using (FileStream stream = File.OpenRead(path))
        {
            return LoadOpenXml(stream);
        }
#endif
    }
    
    public static Excel LoadExcel(Stream s)
    {
#if UNITY_EDITOR
        ExcelPackage ep = new ExcelPackage(s);

        Excel xls = new Excel(ep.Workbook);
        return xls;
#else
        return LoadOpenXml(s);
#endif
    }

    public static Excel LoadOpenXml(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException("stream");

        if (stream.CanSeek)
            stream.Position = 0;

        Excel excel = new Excel();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            List<string> sharedStrings = ReadSharedStrings(archive);
            XmlDocument workbook = LoadXml(archive, "xl/workbook.xml");
            XmlDocument relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            Dictionary<string, string> sheetPaths = ReadRelationships(relationships);

            XmlNodeList sheets = workbook.SelectNodes("//*[local-name()='sheets']/*[local-name()='sheet']");
            foreach (XmlNode sheet in sheets)
            {
                string relationshipId = GetAttributeByLocalName(sheet, "id");
                string sheetPath;
                if (string.IsNullOrEmpty(relationshipId) || !sheetPaths.TryGetValue(relationshipId, out sheetPath))
                    throw new InvalidDataException("Worksheet relationship was not found for '" + GetAttributeByLocalName(sheet, "name") + "'.");

                XmlDocument worksheet = LoadXml(archive, sheetPath);
                ExcelTable table = ReadWorksheet(worksheet, sharedStrings);
                table.TableName = GetAttributeByLocalName(sheet, "name");
                excel.Tables.Add(table);
            }
        }

        return excel;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        List<string> values = new List<string>();
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return values;

        XmlDocument document = LoadXml(entry);
        XmlNodeList items = document.SelectNodes("//*[local-name()='sst']/*[local-name()='si']");
        foreach (XmlNode item in items)
            values.Add(ReadTextNodes(item));

        return values;
    }

    private static Dictionary<string, string> ReadRelationships(XmlDocument relationships)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        XmlNodeList nodes = relationships.SelectNodes("//*[local-name()='Relationship']");
        foreach (XmlNode node in nodes)
        {
            string id = GetAttributeByLocalName(node, "Id");
            string target = GetAttributeByLocalName(node, "Target");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                continue;

            target = target.Replace('\\', '/');
            if (target.StartsWith("/"))
                target = target.Substring(1);
            else if (!target.StartsWith("xl/"))
                target = "xl/" + target;

            result[id] = target;
        }

        return result;
    }

    private static ExcelTable ReadWorksheet(XmlDocument worksheet, List<string> sharedStrings)
    {
        ExcelTable table = new ExcelTable();
        XmlNode dimension = worksheet.SelectSingleNode("//*[local-name()='dimension']");
        if (dimension != null)
            ApplyDimension(table, GetAttributeByLocalName(dimension, "ref"));
        int dimensionRows = table.NumberOfRows;
        int dimensionColumns = table.NumberOfColumns;
        bool hasDimension = dimensionRows > 0 && dimensionColumns > 0;

        XmlNodeList rows = worksheet.SelectNodes("//*[local-name()='sheetData']/*[local-name()='row']");
        int fallbackRow = 0;
        foreach (XmlNode row in rows)
        {
            fallbackRow++;
            int rowIndex = ParsePositiveInt(GetAttributeByLocalName(row, "r"), fallbackRow);
            int fallbackColumn = 0;

            foreach (XmlNode cell in row.SelectNodes("./*[local-name()='c']"))
            {
                fallbackColumn++;
                string reference = GetAttributeByLocalName(cell, "r");
                int columnIndex = ParseColumnIndex(reference, fallbackColumn);
                fallbackColumn = columnIndex;
                if (hasDimension && (rowIndex > dimensionRows || columnIndex > dimensionColumns))
                    continue;
                table.SetValue(rowIndex, columnIndex, ReadCellValue(cell, sharedStrings));
            }
        }

        return table;
    }

    private static string ReadCellValue(XmlNode cell, List<string> sharedStrings)
    {
        string type = GetAttributeByLocalName(cell, "t");
        if (type == "inlineStr")
        {
            XmlNode inline = cell.SelectSingleNode("./*[local-name()='is']");
            return inline == null ? "" : ReadTextNodes(inline);
        }

        XmlNode valueNode = cell.SelectSingleNode("./*[local-name()='v']");
        string value = valueNode == null ? "" : valueNode.InnerText;

        if (type == "s")
        {
            int index;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) && index >= 0 && index < sharedStrings.Count
                ? sharedStrings[index]
                : "";
        }

        if (type == "b")
            return value == "1" ? bool.TrueString : bool.FalseString;

        if (string.IsNullOrEmpty(type) || type == "n")
        {
            double number;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                return number.ToString(CultureInfo.CurrentCulture);
        }

        return value;
    }

    private static string ReadTextNodes(XmlNode parent)
    {
        XmlNodeList textNodes = parent.SelectNodes(".//*[local-name()='t']");
        if (textNodes.Count == 0)
            return "";

        System.Text.StringBuilder value = new System.Text.StringBuilder();
        foreach (XmlNode textNode in textNodes)
            value.Append(textNode.InnerText);
        return value.ToString();
    }

    private static void ApplyDimension(ExcelTable table, string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return;

        int separator = reference.LastIndexOf(':');
        string start = separator >= 0 ? reference.Substring(0, separator) : reference;
        string end = separator >= 0 ? reference.Substring(separator + 1) : reference;
        int startRow = ParseRowIndex(start);
        int endRow = ParseRowIndex(end);
        int startColumn = ParseColumnIndex(start, 0);
        int endColumn = ParseColumnIndex(end, 0);
        table.NumberOfRows = Math.Max(table.NumberOfRows, Math.Max(0, endRow - startRow + 1));
        table.NumberOfColumns = Math.Max(table.NumberOfColumns, Math.Max(0, endColumn - startColumn + 1));
    }

    private static int ParseColumnIndex(string reference, int fallback)
    {
        if (string.IsNullOrEmpty(reference))
            return fallback;

        int column = 0;
        for (int i = 0; i < reference.Length; i++)
        {
            char character = reference[i];
            if (character >= 'a' && character <= 'z')
                character = (char)(character - 'a' + 'A');
            if (character < 'A' || character > 'Z')
                break;
            column = column * 26 + character - 'A' + 1;
        }
        return column > 0 ? column : fallback;
    }

    private static int ParseRowIndex(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return 0;

        int index = 0;
        while (index < reference.Length && !char.IsDigit(reference[index]))
            index++;
        return ParsePositiveInt(reference.Substring(index), 0);
    }

    private static int ParsePositiveInt(string value, int fallback)
    {
        int parsed;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed > 0 ? parsed : fallback;
    }

    private static string GetAttributeByLocalName(XmlNode node, string localName)
    {
        if (node == null || node.Attributes == null)
            return "";

        foreach (XmlAttribute attribute in node.Attributes)
        {
            if (attribute.LocalName == localName)
                return attribute.Value;
        }
        return "";
    }

    private static XmlDocument LoadXml(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path);
        if (entry == null)
            throw new InvalidDataException("Required workbook part was not found: " + path);
        return LoadXml(entry);
    }

    private static XmlDocument LoadXml(ZipArchiveEntry entry)
    {
        XmlDocument document = new XmlDocument();
        using (Stream entryStream = entry.Open())
            document.Load(entryStream);
        return document;
    }

	public static Excel CreateExcel(string path) {
		ExcelPackage ep = new ExcelPackage ();
		ep.Workbook.Worksheets.Add ("sheet");
		Excel xls = new Excel(ep.Workbook);
		SaveExcel (xls, path);
		return xls;
	}

    public static void SaveExcel(Excel xls, string path)
    {
        FileInfo output = new FileInfo(path);
        ExcelPackage ep = new ExcelPackage();
        for (int i = 0; i < xls.Tables.Count; i++)
        {
            ExcelTable table = xls.Tables[i];
            ExcelWorksheet sheet = ep.Workbook.Worksheets.Add(table.TableName);
            for (int row = 1; row <= table.NumberOfRows; row++) {
                for (int column = 1; column <= table.NumberOfColumns; column++) {
                    sheet.Cells[row, column].Value = table.GetValue(row, column);
                }
            }
        }
        ep.SaveAs(output);
    }
}
