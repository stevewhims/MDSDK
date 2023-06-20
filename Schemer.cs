using MDSDK;
using MDSDKBase;
using MDSDKDerived;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MDSDK
{
    internal class ChildElementAdapter
    {
        /// <summary>
        /// A class that adapts to either a non-topic or a topic child element.
        /// </summary>
        /// 
        public SchemerComplexTypeElementEditor? SchemerComplexTypeElementEditorThatIsAChildOfThis = null;
        public XmlSchemaElement? XmlSchemaElementThatIsAChildOfThis = null;

        public ChildElementAdapter(SchemerComplexTypeElementEditor schemerComplexTypeElementEditorThatIsAChildOfThis)
        {
            this.SchemerComplexTypeElementEditorThatIsAChildOfThis = schemerComplexTypeElementEditorThatIsAChildOfThis;
        }

        public ChildElementAdapter(XmlSchemaElement xmlSchemaElementThatIsAChildOfThis)
        {
            this.XmlSchemaElementThatIsAChildOfThis = xmlSchemaElementThatIsAChildOfThis;
        }
    }

    /// <summary>
    /// A class that generates and maintains XML schema content.
    /// See https://learn.microsoft.com/dotnet/standard/data/xml/reading-and-writing-xml-schemas.
    /// </summary>
    internal class Schemer
    {
        private SchemerElementsLandingPageEditor? _schemerElementsLandingPageEditor = null;
        private SchemerComplexTypeElementEditor? _schemerComplexTypeElementEditorRoot = null;
        private string? _xsdFileName = null;

        public Schemer(string topicsRootPath, string topicsFolderName, string schemaName, string xsdFileName)
        {
            SchemerEditorBase.DirectoryInfoForExistingTopics = new DirectoryInfo(topicsRootPath + topicsFolderName);
            SchemerEditorBase.DirectoryInfoForNewTopics = new DirectoryInfo(topicsRootPath + topicsFolderName + @"_gen");
            SchemerEditorBase.SchemaName = schemaName;
            this._xsdFileName = xsdFileName;
        }

        public void DebugInit()
        {
            SchemerEditorBase.DirectoryInfoForNewTopics!.Delete(true);
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

            if (SchemerEditorBase.DirectoryInfoForNewTopics!.Exists)
            {
                ProgramBase.ConsoleWrite(SchemerEditorBase.DirectoryInfoForNewTopics.FullName + " already exists.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }
            else
            {
                SchemerEditorBase.DirectoryInfoForNewTopics.Create();
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

                ProgramBase.ConsoleWrite(string.Empty);
                ProgramBase.ConsoleWrite("Printing out tree of Editors...");
                this._schemerComplexTypeElementEditorRoot!.ConsoleWriteElementTree();
            }
            catch (Exception e)
            {
                ProgramBase.ConsoleWrite(e.Message);
            }
        }

        private void SurveyElementRecursive(SchemerComplexTypeElementEditor? schemerComplexTypeElementEditorParent, XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, int indentation = 0)
        {
            if (this._schemerElementsLandingPageEditor is null)
            {
                FileInfo fileInfoForNewTopic = SchemerElementsLandingPageEditor.FormatFileInfo(true);
                this._schemerElementsLandingPageEditor = new SchemerElementsLandingPageEditor(fileInfoForNewTopic, xmlSchemaElement);
            }

            SchemerComplexTypeElementEditor? schemerComplexTypeElementEditor = null;

            ProgramBase.ConsoleWriteIndent(indentation);

            ProgramBase.ConsoleWrite(xmlSchemaElement.Name + " element", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite(" (", ConsoleWriteStyle.Default, 0);

            XmlSchemaComplexType? xmlSchemaComplexType = xmlSchemaElement.ElementSchemaType as XmlSchemaComplexType;
            if (xmlSchemaComplexType is not null)
            {
                if (xmlSchemaComplexType.ContentType != XmlSchemaContentType.ElementOnly)
                {
                    ProgramBase.ConsoleWrite("The assumption is that complex types contain only elements (not attributes). Need to rewrite the tool.", ConsoleWriteStyle.Error);
                    throw new MDSDKException();
                }

                XmlSchemaSequence? xmlSchemaSequence = xmlSchemaComplexType.ContentTypeParticle as XmlSchemaSequence;
                if (xmlSchemaSequence is not null)
                {
                    foreach (var item in xmlSchemaSequence.Items)
                    {
                        XmlSchemaElement? childXmlSchemaElement = item as XmlSchemaElement;
                        if (childXmlSchemaElement is not null)
                        {
                            if (schemerComplexTypeElementEditor is null)
                            {
                                FileInfo fileInfoForNewTopic = SchemerEditorBase.GetFileInfoForNewTopic(xmlSchemaElementParent, xmlSchemaElement);
                                ProgramBase.ConsoleWrite("will create " + fileInfoForNewTopic.FullName + ")");

                                // Create a new element topic, and add it to its parent topic's child element adapters collection.
                                schemerComplexTypeElementEditor = new SchemerComplexTypeElementEditor(fileInfoForNewTopic, xmlSchemaElement);
                                if (schemerComplexTypeElementEditorParent is not null)
                                {
                                    schemerComplexTypeElementEditorParent.AddChildElementAdapter(schemerComplexTypeElementEditor);
                                }

                                if (this._schemerComplexTypeElementEditorRoot is null)
                                {
                                    this._schemerComplexTypeElementEditorRoot = schemerComplexTypeElementEditor;
                                }
                            }

                            this.SurveyElementRecursive(schemerComplexTypeElementEditor, xmlSchemaElement, childXmlSchemaElement, indentation + ProgramBase.NumberOfCharsToIndentIncrement);
                        }

                        XmlSchemaAny? childXmlSchemaAny = item as XmlSchemaAny;
                        if (childXmlSchemaAny is not null)
                        {
                            if (schemerComplexTypeElementEditorParent is not null)
                            {
                                schemerComplexTypeElementEditorParent.AddChildAny(childXmlSchemaAny);
                            }
                        }
                    }
                }
            }

            if (schemerComplexTypeElementEditor is null)
            {
                if (schemerComplexTypeElementEditorParent is not null)
                {
                    schemerComplexTypeElementEditorParent.AddChildElementAdapter(xmlSchemaElement);
                }

                FileInfo fileInfo = SchemerEditorBase.GetFileInfoForExistingTopic(xmlSchemaElementParent, xmlSchemaElement);
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

            this._schemerComplexTypeElementEditorRoot!.Generate();
        }

        /// <summary>
        /// Delete existing files, and report any that still exist with the schemaName prefix (for manual deletion).
        /// </summary>
        public void Commit()
        {
            ProgramBase.ConsoleWrite("*** COMMIT PHASE ***", ConsoleWriteStyle.Default, 2);

            this._schemerComplexTypeElementEditorRoot!.Commit();
        }
    }
}
