using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace SchemaFromXsd
{
    internal class SchemaBuilder
    {
        private Settings Settings { get; }

        private XDocument Doc { get; }

        private XElement? Schema { get; set; }

        private XNamespace Namespace { get; }

        private List<Field> Fields { get; }

        private Stack<string> Parents { get; }

        public SchemaBuilder(Settings settings)
        {
            this.Settings = settings;
            this.Fields = new();
            this.Parents = new();

            this.Doc = XDocument.Load(this.Settings.SchemaFile);
            this.Namespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
        }

        public void Run()
        {
            this.Schema = this.Doc.Element(this.Namespace + "schema")
                ?? throw new InvalidOperationException("Failed to find base schema element.");
            XElement rootType = this.Schema
                .Elements()
                .SingleOrDefault(n => n.Attribute("name")?.Value == "clinical_study")
                ?? throw new InvalidOperationException("Failed to find base 'clinical_study' element");

            this.Visit(rootType.Elements().First());

            this.WriteOutput();
        }

        private void WriteOutput()
        {
            using StreamWriter file = new(this.Settings.OutputFile);
            char delimiter = '\t';

            if (this.Settings.OutputFormat.ToUpperInvariant() == Settings.CsvFormat)
            {
                delimiter = ',';
                file.WriteLine("FieldName,Type,Path");
            }

            foreach (Field field in this.Fields)
            {
                file.WriteLine($"{field.Name}{delimiter}{field.Type}{delimiter}{field.Path}");
            }
        }

        private void Visit(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "element" :
                    this.VisitElement(element);
                    break;
                case "choice":
                    this.VisitChoice(element);
                    break;
                case "complexType" :
                case "sequence" :
                    this.IterateChildren(element);
                    break;
                case "attribute":
                    this.VisitAttribute(element);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected node type encountered: {element.Name.LocalName}");
            }
        }

        private void VisitAttribute(XElement element)
        {
            string name = SchemaBuilder.PascalCasify(element.Attribute("name")?.Value);
            (string typeName, _, _) = this.GetElementType(element);

            if (!typeName.StartsWith("xs:"))
            {
                throw new InvalidOperationException(
                    $"Found attribute with type {typeName}.  Attributes can only have primitive types");
            }

            this.AppendPrimitiveField(name, typeName, false);
        }

        private void VisitChoice(XElement element)
        {
            bool unbounded = element.Attribute("maxOccurs")?.Value == "unbounded";

            if (unbounded)
            {
                IEnumerable<XElement> childElements = element
                    .Elements()
                    .Where(n => n.Name.LocalName == "element");
                foreach (XElement childElement in childElements)
                {
                    this.VisitElement(childElement, overrideUnbounded: true);
                }
            }

            this.IterateChildren(element);
        }

        private void IterateChildren(XElement element)
        {
            foreach (XElement childElement in element.Elements())
            {
                this.Visit(childElement);
            }
        }

        private void VisitElement(XElement element, bool overrideUnbounded = false)
        {
            string name = SchemaBuilder.PascalCasify(element.Attribute("name")?.Value);
            (string typeName, XElement? typeElement, XElement? extensionElement) = this.GetElementType(element);
            bool unbounded = element.Attribute("maxOccurs")?.Value == "unbounded" || overrideUnbounded;

            if (typeName.StartsWith("xs:"))
            {
                this.AppendPrimitiveField(name, typeName, unbounded);

                if (extensionElement != null)
                {
                    this.Parents.Push(name);
                    if (unbounded)
                    {
                        this.Parents.Push("Value");
                        this.IterateChildren(extensionElement);
                        this.Parents.Pop();
                    }
                    else
                    {
                        this.IterateChildren(extensionElement);
                    }
                    this.Parents.Pop();
                }

                return;
            }

            if (typeElement == null)
            {
                throw new InvalidOperationException($"Failed to find type for element with name {name}");
            }

            if (unbounded)
            {
                this.Fields.Add(new Field(
                    $"{this.GetFieldName(name)}Id", 
                    "integer",
                    $"{this.GetFieldPath(name)}>Id"));
            }

            this.Parents.Push(name);
            this.Visit(typeElement);
            this.Parents.Pop();
        }

        private void AppendPrimitiveField(string name, string typeName, bool unbounded)
        {
            string fieldName = this.GetFieldName(name);
            string fieldPath = this.GetFieldPath(name);

            if (unbounded)
            {
                this.Fields.Add(new Field($"{fieldName}Id", "integer", $"{fieldPath}>Id"));
                this.Fields.Add(new Field($"{fieldName}Value", typeName[3..], $"{fieldPath}>Value"));
                return;
            }

            this.Fields.Add(new Field(fieldName, typeName[3..], fieldPath));
        }

        private (string typeName, XElement? typeElement, XElement? extensionElement) GetElementType(XElement element)
        {
            string? type = element.Attribute("type")?.Value;
            if (type == null)
            {
                return ("custom_struct", element.Element(this.Namespace + "complexType"), null);
            }

            XElement? definedType = this.Schema
                !.Elements()
                .SingleOrDefault(n => n.Attribute("name")?.Value == type);

            XElement? extensionElement = null;

            if (definedType != null)
            {
                (type, extensionElement) = this.GetPrimitiveTypeFromDefinedType(definedType, type);
            }

            return (type, definedType, extensionElement);
        }

        private (string typeName, XElement? extensionElement) GetPrimitiveTypeFromDefinedType(
            XElement definedType, 
            string originalType)
        {
            if (definedType.Name.LocalName == "simpleType")
            {
                return (this.GetPrimitiveTypeFromSimpleType(definedType), null);
            }

            if (definedType.Name.LocalName == "complexType")
            {
                return this.GetPrimitiveTypeFromComplexType(definedType, originalType);
            }

            throw new InvalidOperationException($"Unexpected type element type: {definedType.Name.LocalName}");
        }

        private (string typeName, XElement? extensionElement) GetPrimitiveTypeFromComplexType(
            XElement definedType, 
            string originalType)
        {
            XElement? simpleContentElement = definedType.Element(this.Namespace + "simpleContent");
            XElement? extensionElement = null;
            if (simpleContentElement != null)
            {
                extensionElement = simpleContentElement.Element(this.Namespace + "extension")
                    ?? throw new InvalidOperationException(
                        "Encountered simpleContent element with no extension element.");

                string baseTypeName = extensionElement.Attribute("base")?.Value
                    ?? throw new InvalidOperationException(
                        "Failed to find base attribute of extension element.");

                if (baseTypeName.StartsWith("xs:"))
                {
                    return (baseTypeName, extensionElement);
                }

                XElement baseTypeElement = this.Schema
                    !.Elements()
                    .SingleOrDefault(n => n.Attribute("name")?.Value == baseTypeName)
                    ?? throw new InvalidOperationException($"Failed to find base type named {baseTypeName}");

                return (this.GetPrimitiveTypeFromDefinedType(baseTypeElement, originalType).typeName, extensionElement);
            }

            return (originalType, extensionElement);
        }

        private string GetPrimitiveTypeFromSimpleType(XElement simpleType)
        {
            XElement? restrictionElement = simpleType.Element(this.Namespace + "restriction");
            if (restrictionElement != null)
            {
                return restrictionElement.Attribute("base")?.Value
                    ?? throw new InvalidOperationException("Failed to find base attribute for simple type restriction");
            }

            XElement? unionElement = simpleType.Element(this.Namespace + "union");
            if (unionElement != null)
            {
                string memberTypes = unionElement.Attribute("memberTypes")?.Value
                    ?? throw new InvalidOperationException("Failed to find memberTypes attribute for simple type union");
                XElement memberType = this.Schema
                    !.Elements()
                    .SingleOrDefault(n => n.Attribute("name")?.Value == memberTypes.Split(' ').First())
                    ?? throw new InvalidOperationException("Failed to find first memberType element for union element");

                return this.GetPrimitiveTypeFromSimpleType(memberType);
            }

            throw new InvalidOperationException("Failed to get primitve type from simple type element");
        }

        private string GetFieldName(string name)
        {
            return String.Join(String.Empty, Parents.Reverse().Append(name));
        }

        private string GetFieldPath(string name)
        {
            return String.Join('>', Parents.Reverse().Append(name));
        }

        private static string PascalCasify(string? underscoreString)
        {
            if (underscoreString == null)
            {
                throw new ArgumentNullException(nameof(underscoreString), "Encountered element without a name.");
            }

            StringBuilder sb = new();
            foreach(string word in underscoreString.ToLower().Split('_'))
            {
                sb.Append(word[0].ToUpper()).Append(word.Skip(1).ToArray());
            }

            return sb.ToString();
        }
    }
}
