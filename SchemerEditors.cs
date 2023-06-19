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
        public static DirectoryInfo? DirectoryInfoForGeneratedTopics = null;
        public static string? SchemaName = null;

        public static FileInfo FormatFileInfo(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, bool generated)
        {
            string fileName = SchemerEditorBase.SchemaName!;
            fileName += "-" + xmlSchemaElement.Name!.ToLower();
            if (xmlSchemaElementParent != null)
            {
                fileName += "-" + xmlSchemaElementParent.Name!.ToLower();
            }
            fileName += "-element.md";
            return new FileInfo((generated ? SchemerEditorBase.DirectoryInfoForGeneratedTopics!.FullName : SchemerEditorBase.DirectoryInfoForExistingTopics!.FullName) + @"\" + fileName);
        }
    }

    /// <summary>
    /// Topic for an existing schema element.
    /// </summary>
    internal class SchemerElementExistingTopic : SchemerEditorBase
    {
        public SchemerElementExistingTopic(FileInfo fileInfo) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustAlreadyExist) { }
    }

    /// <summary>
    /// Topic for an all-elements landing page, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-elements.
    /// </summary>
	internal class SchemerElementsLandingPageEditor : SchemerEditorBase
    {
        private XmlSchemaElement _xmlSchemaElementRoot;

        public SchemerElementsLandingPageEditor(FileInfo fileInfo, XmlSchemaElement xmlSchemaElementRoot) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this._xmlSchemaElementRoot = xmlSchemaElementRoot;
        }

        public static FileInfo FormatFileInfo(bool generated)
        {
            string fileName = SchemerEditorBase.SchemaName!;
            fileName += "-elements.md";
            return new FileInfo((generated ? SchemerEditorBase.DirectoryInfoForGeneratedTopics!.FullName : SchemerEditorBase.DirectoryInfoForExistingTopics!.FullName) + @"\" + fileName);
        }

        // Methods that don't modify.

        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.
    }

    /// <summary>
    /// Topic for a parent element topic, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-macrandomization-wlanprofile-element.
    /// </summary>
	internal class SchemerComplexTypeElementEditor : SchemerEditorBase
    {
        protected XmlSchemaElement _xmlSchemaElement;
        public string? XmlSchemaElementName { get { return this._xmlSchemaElement.Name; } }

        protected Collection<ChildElementAdapter>? _childElementAdapters = null;
        protected SchemerComplexTypeElementEditor? _parent = null;

        protected XmlSchemaAny? _childXmlSchemaAny = null;

        public SchemerComplexTypeElementEditor(FileInfo fileInfo, XmlSchemaElement xmlSchemaElement) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this._xmlSchemaElement = xmlSchemaElement;
        }

        public void AddChildElementAdapter(SchemerComplexTypeElementEditor schemerComplexTypeElementEditorThatIsAChildOfThis)
        {
            if (this._childElementAdapters == null)
            {
                this._childElementAdapters = new Collection<ChildElementAdapter>();
            }
            this._childElementAdapters!.Add(new ChildElementAdapter(schemerComplexTypeElementEditorThatIsAChildOfThis));
            schemerComplexTypeElementEditorThatIsAChildOfThis._parent = this;
        }

        public void AddChildElementAdapter(XmlSchemaElement xmlSchemaElementThatIsAChildOfThis)
        {
            if (this._childElementAdapters == null)
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

        public void ConsoleWriteElementTree(int indentation = 0)
        {
            ProgramBase.ConsoleWriteIndent(indentation);
            ProgramBase.ConsoleWrite(this.XmlSchemaElementName + " element", ConsoleWriteStyle.Highlight);

            if (this._childElementAdapters != null)
            {
                foreach (var childElementAdapter in this._childElementAdapters!)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis != null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.ConsoleWriteElementTree(indentation + ProgramBase.NumberOfCharsToIndentIncrement);
                    }
                    else
                    {
                        ProgramBase.ConsoleWriteIndent(indentation + ProgramBase.NumberOfCharsToIndentIncrement);
                        ProgramBase.ConsoleWrite(childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name + " element", ConsoleWriteStyle.Highlight);
                    }
                }
            }
        }

        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.

        public void Generate()
        {
            XmlSchemaElement? xmlSchemaElementParent = null;
            if (this._parent != null)
            {
                xmlSchemaElementParent = this._parent._xmlSchemaElement;
            }

            FileInfo fileInfoPossiblyExisting = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, this._xmlSchemaElement, false);
            SchemerElementExistingTopic? schemerElementExistingTopic = null;
            if (fileInfoPossiblyExisting.Exists)
            {
                schemerElementExistingTopic = new SchemerElementExistingTopic(fileInfoPossiblyExisting);
            }
            else
            {
                ProgramBase.ConsoleWrite(fileInfoPossiblyExisting.FullName + " doesn't exist; nothing to mine.", ConsoleWriteStyle.Highlight);
            }

            this.WriteBeginYamlFrontmatter();
            this.WriteYamlFrontmatterTitle(this.RenderTitlePlusElement());

            this.WriteYamlFrontmatterDescription("TBD");

            if (schemerElementExistingTopic != null) this.WriteYamlFrontmatterMsAssetId(schemerElementExistingTopic!.GetYamlMsAssetId());

            this.WriteYamlFrontmatterMsTopicReference();
            this.WriteYamlFrontmatterMsDate();
            this.WriteYamlFrontmatterTopicTypeAPIRefKbSyntax();
            this.WriteYamlFrontmatterApiName(this.XmlSchemaElementName!);
            this.WriteYamlFrontmatterApiTypeSchema();

            this.WriteYamlFrontmatterApiLocation(string.Empty);
            this.WriteEndYamlFrontmatter();

            this.WriteSectionHeading(1, this.RenderTitlePlusElement());
            if (schemerElementExistingTopic != null)
            {
                this.WriteLine(schemerElementExistingTopic!.Description!);
                this.WriteLine();
            }

            this.WriteBeginSyntax();
            this.GenerateSyntax();
            this.WriteEndSyntax();

            this.GenerateParentElementsSection();

            this.GenerateChildElementsSection();

            this.WriteSectionHeadingRemarks();

            this.WriteSectionHeadingRequirements();

            if (this._childElementAdapters != null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis != null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.Generate();
                    }
                }
            }
        }

        private void GenerateSyntax()
        {
            int numberOfCharsToIndent = 0;

            this.WriteBeginComplexTypeElement(this._xmlSchemaElement, ref numberOfCharsToIndent);

            this.GenerateSyntaxForImmediateChildren(ref numberOfCharsToIndent);

            WriteEndComplexTypeElement(ref numberOfCharsToIndent);
        }

        private void GenerateSyntaxForImmediateChildren(ref int numberOfCharsToIndent)
        {
            if (this._childElementAdapters != null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis != null)
                    {
                        this.WriteOpeningElementTag(childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis._xmlSchemaElement, ref numberOfCharsToIndent, false, true);
                    }
                    else
                    {
                        this.WriteOpeningElementTag(childElementAdapter.XmlSchemaElementThatIsAChildOfThis!, ref numberOfCharsToIndent);
                    }
                }
            }

            if (this._childXmlSchemaAny != null)
            {
                WriteAny(this._childXmlSchemaAny, ref numberOfCharsToIndent);
            }
        }

        private void GenerateParentElementsSection()
        {
            if (this._parent != null)
            {
                this.WriteSectionHeadingParentElements();

                this.WriteBulletPoint(EditorBase.RenderMarkdownLink(this._parent.RenderTitle(), @"./" + this._parent.FileInfo!.Name));
            }
        }

        private string RenderTitle()
        {
            if (this._parent != null)
            {
                return String.Format("{0} ({1})", this.XmlSchemaElementName!, this._parent.XmlSchemaElementName!);
            }
            else
            {
                return this.XmlSchemaElementName!;
            }
        }

        private string RenderTitlePlusElement()
        {
            return this.RenderTitle() + " element";
        }

        private void GenerateChildElementsSection()
        {
            if (this._childElementAdapters != null)
            {
                this.WriteSectionHeadingChildElements();

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
                if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis != null)
                {
                    elementCell = EditorBase.RenderMarkdownLink(
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.XmlSchemaElementName!,
                        @"./" + childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.FileInfo!.Name);
                }
                else
                {
                    elementCell = EditorBase.RenderMarkdownLink(
                        childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!,
                        @"#" + childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!.ToLower());
                    typeCell = childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.SchemaTypeName.Name;
                }
                var rowCells = new List<string>() { elementCell!, typeCell, "TBD" };
                var row = new TableRow(rowCells);
                rows.Add(row);
            }

            var table = new Table(columnHeadings, rows);
            this.WriteLine(table.RenderAsMarkdown());
        }

        private void GenerateSectionsForImmediateChildrenOfSimpleType()
        {
            foreach (var childElementAdapter in this._childElementAdapters!)
            {
                if (childElementAdapter.XmlSchemaElementThatIsAChildOfThis != null)
                {
                    this.WriteSectionHeading(3, childElementAdapter.XmlSchemaElementThatIsAChildOfThis.Name!);
                }
            }
        }

        public void Commit()
        {
            if (this._childElementAdapters != null)
            {
                foreach (var childElementAdapter in this._childElementAdapters)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis != null)
                    {
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.Commit();
                    }
                }
            }
        }
    }
}
