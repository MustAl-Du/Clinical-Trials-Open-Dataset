using Parquet;
using Parquet.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Thrift.Protocol.Entities;

namespace XmlToParquet
{
    internal class Converter
    {
        private static readonly Dictionary<string, Func<string, ColumnInfo?>> ColumnInfoConstructors = new()
        {
            ["string"] = fieldName => ColumnInfo.Create<string?>(fieldName),
            ["integer"] = fieldName => ColumnInfo.Create<int?>(fieldName),
            ["positiveInteger"] = fieldName => ColumnInfo.Create<uint?>(fieldName),
            ["float"] = fieldName => ColumnInfo.Create<float?>(fieldName),
            ["Type"] = fieldName => null
        };
        
        private Settings Settings { get; }

        private List<ColumnInfo> ColumnInfos { get; }

        private FieldTreeNode FieldTree { get; }

        private Stack<ParentData> ParentIndexStack { get; }

        private Stack<string> ParentStack { get; }

        private Dictionary<string, int> LastParentIds { get; }

        public Converter(Settings settings)
        {
            this.Settings = settings;
            this.ColumnInfos = new();
            this.FieldTree = new("ClinicalStudy");
            this.ParentIndexStack = new();
            this.ParentStack = new();
            this.LastParentIds = new();
        }

        public async Task ConvertAsync()
        {
            Console.WriteLine("Start Building Schema");
            Schema schema = this.BuildSchema();
            Console.WriteLine("Finished Building Schema");

            using Stream outputFile = File.OpenWrite(this.Settings.OutputFile);
            using ParquetWriter parquetWriter = await ParquetWriter.CreateAsync(schema, outputFile);

            if (File.Exists(this.Settings.Source))
            {
                await ConvertFile(this.Settings.Source, outputFile, parquetWriter);
            }

            if (Directory.Exists(this.Settings.Source))
            {
                foreach(string filePath in Directory.EnumerateFiles(
                    this.Settings.Source, 
                    "*.xml", 
                    SearchOption.AllDirectories))
                {
                    await ConvertFile(filePath, outputFile, parquetWriter);
                }
            }
        }

        private async Task ConvertFile(string filePath, Stream outputFile, ParquetWriter parquetWriter)
        {
            Console.WriteLine($"Start converting xml for file {filePath}");
            XDocument doc = XDocument.Load(filePath);
            XElement root = doc.Element("clinical_study")
                ?? throw new InvalidOperationException("can't find clinical_study element");
            foreach (XElement e in root.Elements())
            {
                this.Visit(e);
            }
            Console.WriteLine("Finished converting xml");

            Console.WriteLine("Start final padding");
            this.PadAll();
            Console.WriteLine("Finished final padding");

            Console.WriteLine("Start writing parquet file");
            await this.WriteParquetAsync(parquetWriter);
            await outputFile.FlushAsync();
            Console.WriteLine("Finished writing parquet file");

            this.ClearColumnData();
        }

        private void ClearColumnData()
        {
            foreach (ColumnInfo columnInfo in this.ColumnInfos)
            {
                columnInfo.Data.Clear();
            }
        }

        private async Task WriteParquetAsync(ParquetWriter parquetWriter)
        {
            using ParquetRowGroupWriter groupWriter = parquetWriter.CreateRowGroup();

            foreach (ColumnInfo columnInfo in this.ColumnInfos)
            {
                switch (columnInfo.Data)
                {
                    case IList<string?> stringData:
                        await groupWriter.WriteColumnAsync(
                            new DataColumn(columnInfo.Field, stringData.ToArray()));
                        break;
                    case IList<int?> intData:
                        await groupWriter.WriteColumnAsync(
                            new DataColumn(columnInfo.Field, intData.ToArray()));
                        break;
                    case IList<uint?> uintData:
                        await groupWriter.WriteColumnAsync(
                            new DataColumn(columnInfo.Field, uintData.ToArray()));
                        break;
                    case IList<float?> floatData:
                        await groupWriter.WriteColumnAsync(
                            new DataColumn(columnInfo.Field, floatData.ToArray()));
                        break;
                }
            }
        }

        private void PadAll()
        {
            int finalRowCount = this.ColumnInfos.Max(ci => ci.Data.Count);
            foreach (ColumnInfo columnInfo in this.ColumnInfos)
            {
                while (columnInfo.Data.Count < finalRowCount)
                {
                    if (columnInfo.Field.Name == this.Settings.RowIdField)
                    {
                        IList<string> idData = (IList<string>)columnInfo.Data;
                        idData.Add(idData[0]);
                    }
                    else
                    {
                        columnInfo.Data.Add(null);
                    }
                }
            }
        }

        private void Visit(XElement e)
        {
            string name = Converter.PascalCasify(e.Name.LocalName);

            // If the schema has an Id field defined for the node, generate the ParentData object and push it
            // onto the stack.
            FieldTreeValueNode? idField = this.FieldTree.GetValueNode(
                string.Join(">", this.ParentStack.Reverse().Append(name).Append("Id")));
            if(idField != null)
            {
                ColumnInfo cinfo = this.ColumnInfos[idField.Index];
                int idValue = this.LastParentIds.ContainsKey(cinfo.Field.Name)
                    ? this.LastParentIds[cinfo.Field.Name] + 1
                    : 1;
                this.LastParentIds[cinfo.Field.Name] = idValue;

                int startIndex = Math.Max(cinfo.Data.Count, this.ParentIndexStack.TryPeek()?.StartIndex ?? 0);

                this.ParentIndexStack.Push(new ParentData(idValue, startIndex, cinfo));
            }

            // If this is an inernal node, iterate over its children and return (after popping the ParentData
            // for the idField if one was found.
            if(e.HasElements)
            {
                this.ParentStack.Push(name);
                foreach(XElement child in e.Elements())
                {
                    this.Visit(child);
                }
                this.ParentStack.Pop();

                if (idField != null)
                {
                    this.ParentIndexStack.Pop();
                }

                // If the Element has attributes, add their data (with padding if necessary).
                this.AddAttributeData(e, false, name);

                return;
            }

            // Otherwise add the data for the current element (with padding if necessary).
            string fieldName = idField != null
                ? string.Join(">", this.ParentStack.Reverse().Append(name).Append("Value"))
                : string.Join(">", this.ParentStack.Reverse().Append(name));
            FieldTreeValueNode valueNode = this.FieldTree[fieldName];
            ColumnInfo columnInfo = this.ColumnInfos[valueNode.Index];

            this.PadRows(columnInfo);

            Converter.AddFieldData(columnInfo, e.Value);

            // If the Element has attributes, add their data (with padding if necessary).
            this.AddAttributeData(e, idField != null, name);

            // Add any required Id field values.
            if (this.ParentIndexStack.Any())
            {
                foreach (ParentData parentData in this.ParentIndexStack)
                {
                    this.PadRows(parentData.ColumnInfo);
                    if (parentData.ColumnInfo.Data.Count < columnInfo.Data.Count)
                    {
                        Converter.AddFieldData(parentData.ColumnInfo, parentData.ParentId);
                    }
                }
            }

            if (idField != null)
            {
                this.ParentIndexStack.Pop();
            }
        }

        private void AddAttributeData(XElement e, bool IsChildOfValueField, string elementName)
        {
            if (e.HasAttributes)
            {
                foreach (XAttribute attribute in e.Attributes())
                {
                    string name = Converter.PascalCasify(attribute.Name.LocalName);
                    string fieldName = IsChildOfValueField
                        ? string.Join(
                            ">",
                            this.ParentStack.Reverse().Append(elementName).Append("Value").Append(name))
                        : string.Join(
                            ">",
                            this.ParentStack.Reverse().Append(elementName).Append(name));
                    FieldTreeValueNode valueNode = this.FieldTree[fieldName];
                    ColumnInfo columnInfo = this.ColumnInfos[valueNode.Index];

                    this.PadRows(columnInfo);

                    Converter.AddFieldData(columnInfo, attribute.Value);
                }
            }
        }

        private void PadRows(ColumnInfo cinfo)
        {
            if(!this.ParentIndexStack.Any())
            {
                return;
            }

            int startIndex = this.ParentIndexStack.Peek().StartIndex;
            
            while(cinfo.Data.Count < startIndex)
            {
                cinfo.Data.Add(null);
            }
        }

        private static void AddFieldData(ColumnInfo cinfo, string value)
        {
            IList data = cinfo.Data;
            switch (data)
            {
                case IList<string?> stringData:
                    stringData.Add(value);
                    break;
                case IList<int?> intData:
                    intData.Add(Int32.Parse(value));
                    break;
                case IList<uint?> uintData:
                    uintData.Add(UInt32.Parse(value));
                    break;
                case IList<float?> floatData:
                    floatData.Add(Single.Parse(value));
                    break;
            }

            Console.WriteLine($"Stored field {cinfo.Field.Name} with value '{value}'");
        }

        private static void AddFieldData<T>(ColumnInfo cinfo, T value)
            where T : struct
        {
            IList<T?> data = (IList<T?>)cinfo.Data;
            data.Add(new T?(value));

            Console.WriteLine($"Stored field {cinfo.Field.Name} with value {value}");
        }

        private Schema BuildSchema()
        {
            using StreamReader file = new(this.Settings.SchemaFile);

            string? line = file.ReadLine();
            while (line != null)
            {
                this.ColumnInfos.AddIfNotNull(this.BuildColumnInfo(line));
                line = file.ReadLine();
            }

            return new Schema(this.ColumnInfos.Select(ci => ci.Field).ToArray());
        }

        private ColumnInfo? BuildColumnInfo(string line)
        {
            string[] parts = line.Split(',');

            if (parts.Length != 3)
            {
                throw new FormatException($"Schema file line incorrectly formatted: '{line}'");
            }

            if (!Converter.ColumnInfoConstructors.ContainsKey(parts[1]))
            {
                throw new ArgumentException(
                    $"Schema file line contains unexpected type: '{parts[1]}'",
                    nameof(line));
            }

            ColumnInfo? field = Converter.ColumnInfoConstructors[parts[1]](parts[0]);

            this.AddToFieldTree(field?.Field, parts[2]);

            Console.WriteLine($"Added ColumnInfo for line '{line}'");

            return field;
        }

        private void AddToFieldTree(Field? field, string path)
        {
            if (field == null) { return; }

            string[] pathParts = path.Split('>');
            FieldTreeNodeBase node = this.FieldTree;
            int i = 0;
            while (node is FieldTreeNode parentNode 
                && i < pathParts.Length - 1
                && parentNode.Children.ContainsKey(pathParts[i]))
            {
                node = parentNode.Children[pathParts[i]];
                i++;
            }

            while (node is FieldTreeNode parentNode && i < pathParts.Length - 1)
            {
                node = new FieldTreeNode(pathParts[i]);
                parentNode.Children[pathParts[i]] = node;
                i++;
            }

            if (node is FieldTreeNode finalParentNode && i == pathParts.Length - 1)
            {
                finalParentNode.Children[pathParts[i]] = new FieldTreeValueNode(pathParts[i], this.ColumnInfos.Count);
                return;
            }

            throw new InvalidOperationException($"Failed to insert field with path {path} into tree.");
        }

        private static string PascalCasify(string? underscoreString)
        {
            if (underscoreString == null)
            {
                throw new ArgumentNullException(nameof(underscoreString), "Encountered element without a name.");
            }

            StringBuilder sb = new();
            foreach (string word in underscoreString.ToLower().Split('_'))
            {
                sb.Append(word[0].ToUpper()).Append(word.Skip(1).ToArray());
            }

            return sb.ToString();
        }
    }
}
