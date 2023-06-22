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
    /// <summary>
    /// A class that (via its derived types) adapts to either a topic or a non-topic child element.
    /// </summary>
    /// 
    internal abstract class ChildElementAdapter
    {
    }

    internal class ChildElementAdapterTopic : ChildElementAdapter
    {
        public SchemerComplexTypeElementEditor SchemerComplexTypeElementEditorThatIsAChildOfThis { get; private set; }

        public ChildElementAdapterTopic(SchemerComplexTypeElementEditor schemerComplexTypeElementEditorThatIsAChildOfThis)
        {
            this.SchemerComplexTypeElementEditorThatIsAChildOfThis = schemerComplexTypeElementEditorThatIsAChildOfThis;
        }
    }

    internal class ChildElementAdapterNonTopic : ChildElementAdapter
    {
        public XmlSchemaElement XmlSchemaElementThatIsAChildOfThis { get; private set; }
        public SchemerElementExistingTopicEditor? SchemerElementExistingTopicEditor { get; private set; }

        public ChildElementAdapterNonTopic(XmlSchemaElement xmlSchemaElementThatIsAChildOfThis, SchemerElementExistingTopicEditor? schemerElementExistingChildTopicEditor)
        {
            this.XmlSchemaElementThatIsAChildOfThis = xmlSchemaElementThatIsAChildOfThis;
            this.SchemerElementExistingTopicEditor = schemerElementExistingChildTopicEditor;
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

        public Schemer(string topicsRootPath, string topicsFolderName, string schemaDisplayName, string schemaNameForFilenames, string xsdFileName)
        {
            SchemerEditorBase.DirectoryInfoForExistingTopics = new DirectoryInfo(topicsRootPath + topicsFolderName);
            SchemerEditorBase.DirectoryInfoForNewTopics = new DirectoryInfo(topicsRootPath + topicsFolderName + @"_gen");
            SchemerEditorBase.SchemaDisplayName = schemaDisplayName;
            SchemerEditorBase.SchemaNameForFilenames = schemaNameForFilenames;
            this._xsdFileName = xsdFileName;

            SchemerCustomConfiguration.ReadConfigurationFile(this);
        }

        public void DebugInit()
        {
            if (SchemerEditorBase.DirectoryInfoForNewTopics!.Exists)
            {
                SchemerEditorBase.DirectoryInfoForNewTopics!.Delete(true);
            }
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

        private void SurveyElementRecursive(SchemerComplexTypeElementEditor? schemerComplexTypeElementEditorParent, XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, int numberOfCharsToIndent = 0)
        {
            SchemerComplexTypeElementEditor? schemerComplexTypeElementEditor = null;

            ProgramBase.ConsoleWriteIndent(numberOfCharsToIndent);

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
                                FileInfo fileInfoForNewTopic = SchemerComplexTypeElementEditor.GetFileInfoForNewTopic(xmlSchemaElementParent, xmlSchemaElement);
                                ProgramBase.ConsoleWrite("will create " + fileInfoForNewTopic.FullName + ")");

                                // Create a new element topic, and add it to its parent topic's child element adapters collection.
                                schemerComplexTypeElementEditor = new SchemerComplexTypeElementEditor(fileInfoForNewTopic, xmlSchemaElement);
                                if (schemerComplexTypeElementEditorParent is not null)
                                {
                                    schemerComplexTypeElementEditorParent.AddChildElementAdapter(schemerComplexTypeElementEditor);
                                }

                                // Predict what the path and filename for this element's topic's would be if it already existed, and get a FileInfo for it.
                                FileInfo fileInfoPossiblyExisting = SchemerComplexTypeElementEditor.GetFileInfoForExistingTopic(xmlSchemaElementParent, xmlSchemaElement);

                                // If there *is* an existing topic for this element, then create an Editor for it.
                                if (fileInfoPossiblyExisting.Exists)
                                {
                                    schemerComplexTypeElementEditor.SetSchemerElementExistingTopicEditor(fileInfoPossiblyExisting);
                                }

                                if (this._schemerComplexTypeElementEditorRoot is null)
                                {
                                    this._schemerComplexTypeElementEditorRoot = schemerComplexTypeElementEditor;
                                }
                            }

                            if (this._schemerElementsLandingPageEditor is null)
                            {
                                FileInfo fileInfoForNewTopic = SchemerElementsLandingPageEditor.GetFileInfoForNewTopic();
                                this._schemerElementsLandingPageEditor = new SchemerElementsLandingPageEditor(fileInfoForNewTopic, schemerComplexTypeElementEditor!);
                            }

                            this.SurveyElementRecursive(schemerComplexTypeElementEditor, xmlSchemaElement, childXmlSchemaElement, numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                        }

                        XmlSchemaAny? childXmlSchemaAny = item as XmlSchemaAny;
                        if (childXmlSchemaAny is not null)
                        {
                            if (schemerComplexTypeElementEditor is not null)
                            {
                                schemerComplexTypeElementEditor.AddChildAny(childXmlSchemaAny);
                            }
                        }
                    }
                }
            }

            if (schemerComplexTypeElementEditor is null)
            {
                // Predict what the path and filename for the child element's topic's would be if it already existed, and get a FileInfo for it.
                FileInfo fileInfoPossiblyExistingChildTopic = SchemerComplexTypeElementEditor.GetFileInfoForExistingTopic(xmlSchemaElementParent, xmlSchemaElement);

                // If there *is* an existing topic for the child element, then create an Editor for it.
                SchemerElementExistingTopicEditor? schemerElementExistingChildTopicEditor = null;
                if (fileInfoPossiblyExistingChildTopic.Exists)
                {
                    schemerElementExistingChildTopicEditor = new SchemerElementExistingTopicEditor(fileInfoPossiblyExistingChildTopic);
                    ProgramBase.ConsoleWrite("will delete " + fileInfoPossiblyExistingChildTopic.FullName + ")");
                }
                else
                {
                    ProgramBase.ConsoleWrite("nothing to delete)");
                }

                if (schemerComplexTypeElementEditorParent is not null)
                {
                    schemerComplexTypeElementEditorParent.AddChildElementAdapter(xmlSchemaElement, schemerElementExistingChildTopicEditor);
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

            this._schemerElementsLandingPageEditor!.Generate();

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
