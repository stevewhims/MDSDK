using MDSDK;
using MDSDKBase;
using MDSDKDerived;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Permissions;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MDSDK
{
    /// <summary>
    /// See the xml docs for EditorBase.
    /// </summary>
	internal class SchemerEditorBase : EditorBase
    {
        public SchemerEditorBase(FileInfo fileInfo, EditorBaseFileInfoExistenceRequirements fileInfoExistenceRequirements) : base(fileInfo, fileInfoExistenceRequirements) { }

        public static DirectoryInfo? DirectoryInfoForExistingTopics = null;
        public static DirectoryInfo? DirectoryInfoForNewTopics = null;
        public static string? SchemaDisplayName = null;
        public static string? SchemaNameForFilenames = null;

        protected virtual string RenderTitle() { return string.Empty; }
        protected virtual string? RenderYamlFrontmatterApiNameValue() { return null; }

        protected void GenerateCommon(SchemerElementExistingTopicEditor? schemerElementExistingTopicEditor)
        {
            this.WriteBeginYamlFrontmatter();
            this.WriteYamlFrontmatterTitle(this.RenderTitle());

            if (schemerElementExistingTopicEditor is not null)
            {
                this.WriteYamlFrontmatterDescription(schemerElementExistingTopicEditor!.GetYamlDescription());
            }
            else
            {
                this.WriteYamlFrontmatterDescription(EditorBase.TBDSentenceString);
            }

            this.WriteYamlFrontmatterMsTopicReference();
            this.WriteYamlFrontmatterMsDate();
            this.WriteYamlFrontmatterTopicTypeAPIRefKbSyntax();
            this.WriteYamlFrontmatterApiName(this.RenderYamlFrontmatterApiNameValue());
            this.WriteYamlFrontmatterApiTypeSchema();
            this.WriteYamlFrontmatterApiLocation(string.Empty);

            if (schemerElementExistingTopicEditor is not null) this.WriteYamlFrontmatterMsAssetId(schemerElementExistingTopicEditor!.GetYamlMsAssetId());

            this.WriteEndYamlFrontmatter();

            this.WriteSectionHeading(1, this.RenderTitle());
            if (schemerElementExistingTopicEditor is not null)
            {
                this.Write(schemerElementExistingTopicEditor!.EditorObjectModel.Description!);
            }
        }
    }

    /// <summary>
    /// Topic for an existing schema element.
    /// </summary>
    internal class SchemerElementExistingTopicEditor : SchemerEditorBase
    {
        public SchemerElementExistingTopicEditor(FileInfo fileInfo) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustAlreadyExist) { }
    }

    /// <summary>
    /// Topic for an all-elements landing page, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-elements.
    /// </summary>
    internal class SchemerElementsLandingPageEditor : SchemerEditorBase
    {
        private SchemerComplexTypeElementEditor _schemerComplexTypeElementEditor;

        public SchemerElementsLandingPageEditor(FileInfo fileInfo, SchemerComplexTypeElementEditor schemerComplexTypeElementEditor) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this._schemerComplexTypeElementEditor = schemerComplexTypeElementEditor;
        }

        private static FileInfo GetFileInfoForNewOrExistingTopic(bool isTopicNew)
        {
            string fileName = SchemerEditorBase.SchemaNameForFilenames!;
            fileName += "-elements.md";
            return new FileInfo((isTopicNew ? SchemerEditorBase.DirectoryInfoForNewTopics!.FullName : SchemerEditorBase.DirectoryInfoForExistingTopics!.FullName) + @"\" + fileName);
        }

        public static FileInfo GetFileInfoForNewTopic()
        {
            return SchemerElementsLandingPageEditor.GetFileInfoForNewOrExistingTopic(true);
        }

        public static FileInfo GetFileInfoForExistingTopic()
        {
            return SchemerElementsLandingPageEditor.GetFileInfoForNewOrExistingTopic(false);
        }

        protected override string RenderTitle()
        {
            return SchemerEditorBase.SchemaDisplayName + " schema elements";
        }

        public void Generate()
        {
            // Predict what the path and filename for this element's topic's would be if it already existed, and get a FileInfo for it.
            FileInfo fileInfoPossiblyExisting = SchemerElementsLandingPageEditor.GetFileInfoForExistingTopic();

            // If there *is* an existing topic for this element, then create an Editor for it.
            SchemerElementExistingTopicEditor? schemerElementExistingTopicEditor = null;
            if (fileInfoPossiblyExisting.Exists)
            {
                schemerElementExistingTopicEditor = new SchemerElementExistingTopicEditor(fileInfoPossiblyExisting);
            }
            else
            {
                ProgramBase.ConsoleWrite(fileInfoPossiblyExisting.FullName + " doesn't exist; nothing to mine.", ConsoleWriteStyle.Warning);
            }

            this.GenerateCommon(schemerElementExistingTopicEditor);

            string tree = string.Empty;
            this._schemerComplexTypeElementEditor.RenderElementTreeForElementsLandingPage(ref tree);
            this.Write(tree);
        }
    }

    /// <summary>
    /// Topic for a parent element topic, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-macrandomization-wlanprofile-element.
    /// </summary>
    internal class SchemerComplexTypeElementEditor : SchemerEditorBase
    {
        public XmlSchemaElement XmlSchemaElement { get; protected set; }

        protected Collection<ChildElementAdapter>? _childElementAdapters = null;
        protected SchemerComplexTypeElementEditor? _parent = null;

        protected XmlSchemaAny? _childXmlSchemaAny = null;

        protected override string? RenderYamlFrontmatterApiNameValue() { return this.XmlSchemaElement.Name!; }

        private static FileInfo GetFileInfoForNewOrExistingTopic(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, bool isTopicNew)
        {
            string fileName = SchemerEditorBase.SchemaNameForFilenames!;
            fileName += "-" + xmlSchemaElement.Name!.ToLower();
            if (xmlSchemaElementParent is not null)
            {
                fileName += "-" + xmlSchemaElementParent.Name!.ToLower();
            }
            fileName += "-element.md";
            return new FileInfo((isTopicNew ? SchemerEditorBase.DirectoryInfoForNewTopics!.FullName : SchemerEditorBase.DirectoryInfoForExistingTopics!.FullName) + @"\" + fileName);
        }

        public static FileInfo GetFileInfoForNewTopic(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement)
        {
            return SchemerComplexTypeElementEditor.GetFileInfoForNewOrExistingTopic(xmlSchemaElementParent, xmlSchemaElement, true);
        }

        public static FileInfo GetFileInfoForExistingTopic(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement)
        {
            return SchemerComplexTypeElementEditor.GetFileInfoForNewOrExistingTopic(xmlSchemaElementParent, xmlSchemaElement, false);
        }

        public SchemerComplexTypeElementEditor(FileInfo fileInfo, XmlSchemaElement xmlSchemaElement) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this.XmlSchemaElement = xmlSchemaElement;
        }

        public void AddChildElementAdapter(SchemerComplexTypeElementEditor schemerComplexTypeElementEditorThatIsAChildOfThis)
        {
            if (this._childElementAdapters is null)
            {
                this._childElementAdapters = new Collection<ChildElementAdapter>();
            }
            this._childElementAdapters!.Add(new ChildElementAdapter(schemerComplexTypeElementEditorThatIsAChildOfThis));
            schemerComplexTypeElementEditorThatIsAChildOfThis._parent = this;
        }

        public void AddChildElementAdapter(XmlSchemaElement xmlSchemaElementThatIsAChildOfThis)
        {
            if (this._childElementAdapters is null)
            {
                this._childElementAdapters = new Collection<ChildElementAdapter>();
            }
            this._childElementAdapters!.Add(new ChildElementAdapter(xmlSchemaElementThatIsAChildOfThis));
        }

        public void AddChildAny(XmlSchemaAny childXmlSchemaAny)
        {
            this._childXmlSchemaAny = childXmlSchemaAny;
        }

        // Methods that don't modify.

        public void ConsoleWriteElementTree(int numberOfCharsToIndent = 0)
        {
            ProgramBase.ConsoleWriteIndent(numberOfCharsToIndent);
            ProgramBase.ConsoleWrite(this.XmlSchemaElement.Name + " element", ConsoleWriteStyle.Highlight);

            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters!)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.ConsoleWriteElementTree(numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                    }
                    else
                    {
                        ProgramBase.ConsoleWriteIndent(numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                        ProgramBase.ConsoleWrite(childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name + " element", ConsoleWriteStyle.Highlight);
                    }
                }
            }
        }

        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.

        public void Generate()
        {
            /// If this topic isn't the topic for the root element (so it has a parent), then grab the XmlSchemaElement that represents the parent's xsd element.
            XmlSchemaElement? xmlSchemaElementParent = null;
            xmlSchemaElementParent = this._parent?.XmlSchemaElement;

            // Predict what the path and filename for this element's topic's would be if it already existed, and get a FileInfo for it.
            FileInfo fileInfoPossiblyExisting = SchemerComplexTypeElementEditor.GetFileInfoForExistingTopic(xmlSchemaElementParent, this.XmlSchemaElement);

            // If there *is* an existing topic for this element, then create an Editor for it.
            SchemerElementExistingTopicEditor? schemerElementExistingTopicEditor = null;
            if (fileInfoPossiblyExisting.Exists)
            {
                schemerElementExistingTopicEditor = new SchemerElementExistingTopicEditor(fileInfoPossiblyExisting);
            }
            else
            {
                ProgramBase.ConsoleWrite(fileInfoPossiblyExisting.FullName + " doesn't exist; nothing to mine.", ConsoleWriteStyle.Warning);
            }

            this.GenerateCommon(schemerElementExistingTopicEditor);

            this.WriteBeginSyntax();
            this.GenerateSyntax();
            this.WriteEndSyntax();

            this.GenerateParentElementsSection();

            this.GenerateChildElementsSection();

            if (schemerElementExistingTopicEditor is not null)
            {
                this.WriteRemarks(schemerElementExistingTopicEditor!.EditorObjectModel.Remarks!);
            }

            this.WriteSectionHeadingRequirements();
            if (schemerElementExistingTopicEditor is not null)
            {
                this.WriteRequirements(schemerElementExistingTopicEditor!.EditorObjectModel.RequirementsTable!);
            }

            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.Generate();
                    }
                }
            }
        }

        private void GenerateSyntax()
        {
            int numberOfCharsToIndent = 0;

            this.WriteBeginComplexTypeElement(this.XmlSchemaElement, ref numberOfCharsToIndent);

            this.GenerateSyntaxForImmediateChildren(ref numberOfCharsToIndent);

            WriteEndComplexTypeElement(ref numberOfCharsToIndent);
        }

        private void GenerateSyntaxForImmediateChildren(ref int numberOfCharsToIndent)
        {
            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                    {
                        this.WriteOpeningElementTag(childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.XmlSchemaElement, ref numberOfCharsToIndent, false, true);
                    }
                    else
                    {
                        this.WriteOpeningElementTag(childElementAdapter.XmlSchemaElementThatIsAChildOfThis!, ref numberOfCharsToIndent);
                    }
                }
            }

            if (this._childXmlSchemaAny is not null)
            {
                WriteAny(this._childXmlSchemaAny, ref numberOfCharsToIndent);
            }
        }

        private void GenerateParentElementsSection()
        {
            this.WriteSectionHeadingParentElements();
            this.WriteLine();

            if (this._parent is not null)
            {
                this.WriteBulletPoint(EditorBase.RenderHyperlink(this._parent.RenderElementName(), @"./" + this._parent.FileInfo!.Name));
            }
            else
            {
                this.WriteLine(EditorBase.NoneSentenceString);
            }

            this.WriteLine();
        }

        private string RenderElementName()
        {
            if (this._parent is not null)
            {
                return String.Format($"{this.XmlSchemaElement.Name!} ({this._parent.XmlSchemaElement.Name!})");
            }
            else
            {
                return this.XmlSchemaElement.Name!;
            }
        }

        private static string RenderElementNameForXmlSchemaElement(string? parentElementName, XmlSchemaElement xmlSchemaElement)
        {
            if (parentElementName is not null)
            {
                return String.Format($"{xmlSchemaElement.Name!} ({parentElementName!})");
            }
            else
            {
                return xmlSchemaElement.Name!;
            }
        }

        protected override string RenderTitle()
        {
            return this.RenderElementName() + " element";
        }

        private void GenerateChildElementsSection()
        {
            if (this._childElementAdapters is not null)
            {
                this.WriteSectionHeadingChildElements();
                this.WriteLine();

                this.GenerateTableForImmediateChildren();

                this.GenerateSectionsForImmediateChildrenOfSimpleType();
            }
        }

        private void GenerateTableForImmediateChildren()
        {
            var columnHeadings = new List<string>() { "Element", "Type", "Description" };
            var rows = new List<TableRow>();

            foreach (var childElementAdapter in this._childElementAdapters!)
            {
                string? elementCell = string.Empty;
                string? typeCell = string.Empty;
                string descriptionCell = EditorBase.TBDSentenceString;

                if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                {
                    elementCell = EditorBase.RenderHyperlink(
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.XmlSchemaElement.Name!,
                        @"./" + childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.FileInfo!.Name,
                        true);
                    descriptionCell = "DUNNO YET";
                }
                else
                {
                    elementCell = EditorBase.RenderHyperlink(
                        childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!,
                        @"#" + childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!.ToLower(),
                        true);
                    typeCell = childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.SchemaTypeName.Name;

                    // Predict what the path and filename for the child element's topic's would be if it already existed, and get a FileInfo for it.
                    FileInfo fileInfoPossiblyExistingChildTopic = SchemerComplexTypeElementEditor.GetFileInfoForExistingTopic(this.XmlSchemaElement, childElementAdapter.XmlSchemaElementThatIsAChildOfThis);

                    // If there *is* an existing topic for this element, then create an Editor for it, and get the YAML description from it.
                    if (fileInfoPossiblyExistingChildTopic.Exists)
                    {
                        var schemerElementExistingChildTopicEditor = new SchemerElementExistingTopicEditor(fileInfoPossiblyExistingChildTopic);
                        string? yamlDescription = schemerElementExistingChildTopicEditor.GetYamlDescription();
                        if (yamlDescription is not null) descriptionCell = yamlDescription;
                    }
                }

                var rowCells = new List<string>() { elementCell!, typeCell, descriptionCell };
                var row = new TableRow(rowCells);
                rows.Add(row);
            }

            var table = new Table(columnHeadings, rows);
            this.WriteLine(table.Render());
            this.WriteLine();
        }

        private void GenerateSectionsForImmediateChildrenOfSimpleType()
        {
            foreach (var childElementAdapter in this._childElementAdapters!)
            {
                if (childElementAdapter.XmlSchemaElementThatIsAChildOfThis is not null)
                {
                    this.WriteSectionHeading(3, childElementAdapter.XmlSchemaElementThatIsAChildOfThis.Name!);
                    this.WriteLine();
                }
            }
        }

        public void RenderElementTreeForElementsLandingPage(ref string tree, int numberOfCharsToIndent = 0)
        {
            tree += EditorBase.RenderIndent(numberOfCharsToIndent);
            tree += EditorBase.RenderBulletPoint(EditorBase.RenderHyperlink(this.RenderElementName(), @"./" + this.FileInfo!.Name, true));
            tree += Environment.NewLine;

            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters!)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.RenderElementTreeForElementsLandingPage(ref tree, numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                    }
                    else
                    {
                        string qualifiedName = SchemerComplexTypeElementEditor.RenderElementNameForXmlSchemaElement(this.XmlSchemaElement.Name, childElementAdapter.XmlSchemaElementThatIsAChildOfThis!);

                        tree += EditorBase.RenderIndent(numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                        tree += EditorBase.RenderBulletPoint(EditorBase.RenderHyperlink(
                            childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!,
                            $"{this.FileInfo.Name}#{childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!.ToLower()}",
                            true));
                        tree += Environment.NewLine;

                        // See whether we need to add an appendix element after the element we just wrote.
                        SchemerCustomConfigurationAppendixElement? appendixElement = SchemerCustomConfiguration.FindSchemerCustomConfigurationAppendixElementForName(qualifiedName);
                        if (appendixElement != null)
                        {
                            tree += EditorBase.RenderIndent(numberOfCharsToIndent + ProgramBase.NumberOfCharsToIndentIncrement);
                            tree += EditorBase.RenderBulletPoint(EditorBase.RenderHyperlink(
                                appendixElement.Name!,
                                appendixElement.Url!,
                                true));
                            tree += Environment.NewLine;
                        }
                    }
                }
            }
        }

        public void Commit()
        {
            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.Commit();
                    }
                }
            }
        }
    }
}
