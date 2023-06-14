using MDSDKBase;
using MDSDKDerived;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MDSDK
{
    /// <summary>
    /// A class that generates and maintains XML schema content.
    /// https://learn.microsoft.com/dotnet/standard/data/xml/reading-and-writing-xml-schemas
    /// </summary>
    internal class Schemer
    {
        // If this class doesn't get too complicated, it might be good just to move this code into SchemerEditorBase.
        public static void Process(string topicsRootPath, string topicsFolderName, string schemaName, string xsdFileName)
        {
            SchemerEditorBase.DirectoryInfoForExistingTopics = new DirectoryInfo(topicsRootPath + topicsFolderName);
            SchemerEditorBase.DirectoryInfoForGeneratedTopics = new DirectoryInfo(topicsRootPath + topicsFolderName);
            SchemerEditorBase.SchemaName = schemaName;

            if (!SchemerEditorBase.DirectoryInfoForExistingTopics.Exists)
            {
                ProgramBase.ConsoleWrite(SchemerEditorBase.DirectoryInfoForExistingTopics.FullName + " doesn't exist.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
    }

            if (SchemerEditorBase.DirectoryInfoForGeneratedTopics.Exists)
            {
                ProgramBase.ConsoleWrite(SchemerEditorBase.DirectoryInfoForGeneratedTopics.FullName + " already exists.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            try
            {
                var xmlSchemaSet = new XmlSchemaSet();
                xmlSchemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
                xmlSchemaSet.Add(null, xsdFileName);
                xmlSchemaSet.Compile();

                foreach (XmlSchema xmlSchema in xmlSchemaSet.Schemas())
                {
                    foreach (XmlSchemaElement xmlSchemaElement in xmlSchema!.Elements.Values)
                    {
                        Schemer.ProcessElement(null, xmlSchemaElement);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ProcessElement(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, int indentation = 0)
        {
            SchemerParentElementTopic? schemerParentElementTopic = null;

            for (int i = 0; i < indentation; i++)
            {
                Console.Write(" ");
            }

            Console.WriteLine("Element: {0}", xmlSchemaElement.Name);

            XmlSchemaComplexType? xmlSchemaComplexType = xmlSchemaElement.ElementSchemaType as XmlSchemaComplexType;
            if (xmlSchemaComplexType != null)
            {
                XmlSchemaSequence? xmlSchemaSequence = xmlSchemaComplexType.ContentTypeParticle as XmlSchemaSequence;
                if (xmlSchemaSequence != null)
                {
                    foreach (var item in xmlSchemaSequence.Items)
                    {
                        XmlSchemaElement? childXmlSchemaElement = item as XmlSchemaElement;
                        if (childXmlSchemaElement != null)
                        {
                            if (schemerParentElementTopic == null)
                            {
                                FileInfo fileInfo = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, xmlSchemaElement);
                                schemerParentElementTopic = new SchemerParentElementTopic(fileInfo, xmlSchemaElement);
                            }

                            Schemer.ProcessElement(xmlSchemaElement, childXmlSchemaElement, indentation + 2);
                        }
                    }
                }
            }

            if (schemerParentElementTopic == null)
            {
                FileInfo fileInfo = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, xmlSchemaElement);
                Console.WriteLine("Need to delete topic: {0}", fileInfo.Name);
            }
        }

        private static void ValidationCallback(object? sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                Console.Write("WARNING: ");
            else if (args.Severity == XmlSeverityType.Error)
                Console.Write("ERROR: ");

            Console.WriteLine(args.Message);
        }
    }
}
