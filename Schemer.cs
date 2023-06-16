using MDSDK;
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
        private SchemerAllElementsGeneratedTopic? _schemerElementsGeneratedTopic = null;
        private SchemerParentElementGeneratedTopic? _schemerParentElementGeneratedTopicRoot = null;
        private string? _xsdFileName = null;

        public Schemer(string topicsRootPath, string topicsFolderName, string schemaName, string xsdFileName)
        {
            SchemerEditorBase.DirectoryInfoForExistingTopics = new DirectoryInfo(topicsRootPath + topicsFolderName);
            SchemerEditorBase.DirectoryInfoForGeneratedTopics = new DirectoryInfo(topicsRootPath + topicsFolderName + @"_gen");
            SchemerEditorBase.SchemaName = schemaName;
            this._xsdFileName = xsdFileName;
        }

        /// <summary>
        /// // See what's in the xsd, build tree of Editors, and create files.
        /// </summary>
        /// <exception cref="MDSDKException"></exception>
        public void Survey()
        {
            ProgramBase.ConsoleWrite("*** SURVEY PHASE ***", ConsoleWriteStyle.Default, 2);

            ProgramBase.ConsoleWrite("Reading xsd, and building tree of Editors...");

            if (!SchemerEditorBase.DirectoryInfoForExistingTopics!.Exists)
            {
                ProgramBase.ConsoleWrite(SchemerEditorBase.DirectoryInfoForExistingTopics.FullName + " doesn't exist.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            if (SchemerEditorBase.DirectoryInfoForGeneratedTopics!.Exists)
            {
                ProgramBase.ConsoleWrite(SchemerEditorBase.DirectoryInfoForGeneratedTopics.FullName + " already exists.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }
            else
            {
                SchemerEditorBase.DirectoryInfoForGeneratedTopics.Create();
            }

            try
            {
                var xmlSchemaSet = new XmlSchemaSet();
                xmlSchemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
                xmlSchemaSet.Add(null, this._xsdFileName!);
                xmlSchemaSet.Compile();

                foreach (XmlSchema xmlSchema in xmlSchemaSet.Schemas())
                {
                    if (xmlSchema!.Elements.Count != 1)
                    {
                        ProgramBase.ConsoleWrite("The assumption is that there's only one element at the root. Need to rewrite the tool.", ConsoleWriteStyle.Error);
                        throw new MDSDKException();
                    }
                    foreach (XmlSchemaElement xmlSchemaElement in xmlSchema!.Elements.Values)
                    {
                        this.SurveyElementRecursive(null, null, xmlSchemaElement);
                    }
                }

                ProgramBase.ConsoleWrite("Printing out tree of Editors...");
                this._schemerParentElementGeneratedTopicRoot!.PrintElementTree();
            }
            catch (Exception e)
            {
                ProgramBase.ConsoleWrite(e.Message);
            }
        }

        private void SurveyElementRecursive(SchemerParentElementGeneratedTopic? schemerParentElementGeneratedTopicParent, XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, int indentation = 0)
        {
            if (this._schemerElementsGeneratedTopic == null)
            {
                FileInfo fileInfoGenerated = SchemerAllElementsGeneratedTopic.FormatFileInfo(true);
                this._schemerElementsGeneratedTopic = new SchemerAllElementsGeneratedTopic(fileInfoGenerated, xmlSchemaElement);
            }

            SchemerParentElementGeneratedTopic? schemerParentElementGeneratedTopic = null;

            for (int i = 0; i < indentation; i++)
            {
                ProgramBase.ConsoleWrite(" ", ConsoleWriteStyle.Default, 0);
            }

            ProgramBase.ConsoleWrite(xmlSchemaElement.Name + " element (", ConsoleWriteStyle.Default, 0);

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
                            if (schemerParentElementGeneratedTopic == null)
                            {
                                FileInfo fileInfoGenerated = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, xmlSchemaElement, true);
                                ProgramBase.ConsoleWrite("will create " + fileInfoGenerated.FullName + ")");

                                // Create a new element topic, and add it to its parent topic's child elements collection.
                                schemerParentElementGeneratedTopic = new SchemerParentElementGeneratedTopic(fileInfoGenerated, xmlSchemaElement);
                                if (schemerParentElementGeneratedTopicParent != null)
                                {
                                    schemerParentElementGeneratedTopicParent.Add(schemerParentElementGeneratedTopic);
                                }

                                if (this._schemerParentElementGeneratedTopicRoot == null)
                                {
                                    this._schemerParentElementGeneratedTopicRoot = schemerParentElementGeneratedTopic;
                                }
                            }

                            this.SurveyElementRecursive(schemerParentElementGeneratedTopic, xmlSchemaElement, childXmlSchemaElement, indentation + 2);
                        }
                    }
                }
            }

            if (schemerParentElementGeneratedTopic == null)
            {
                FileInfo fileInfo = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, xmlSchemaElement, false);
                if (fileInfo.Exists)
                {
                    ProgramBase.ConsoleWrite("will delete " + fileInfo.FullName + ")");
                }
                else
                {
                    ProgramBase.ConsoleWrite("nothing to delete)");
                }
            }
        }

        private static void ValidationCallback(object? sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
                ProgramBase.ConsoleWrite("WARNING: ", ConsoleWriteStyle.Warning);
            else if (args.Severity == XmlSeverityType.Error)
                ProgramBase.ConsoleWrite("ERROR: ", ConsoleWriteStyle.Error);

            ProgramBase.ConsoleWrite(args.Message);
        }

        /// <summary>
        /// Generate the content in the files.
        /// </summary>
        public void Generate()
        {
            ProgramBase.ConsoleWrite("*** GENERATE PHASE ***", ConsoleWriteStyle.Default, 2);

            this._schemerParentElementGeneratedTopicRoot!.Generate();
        }

        /// <summary>
        /// Delete existing files, and report any that still exist with the schemaName prefix (for manual deletion).
        /// </summary>
        public void Commit()
        {
            ProgramBase.ConsoleWrite("*** COMMIT PHASE ***", ConsoleWriteStyle.Default, 2);

            this._schemerParentElementGeneratedTopicRoot!.Commit();
        }
    }
}
