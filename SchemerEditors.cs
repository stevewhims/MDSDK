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
        public static string? SchemaName = null;

        private static FileInfo Xxx(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement, bool isTopicNew)
        {
            string fileName = SchemerEditorBase.SchemaName!;
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
            return SchemerEditorBase.Xxx(xmlSchemaElementParent, xmlSchemaElement, true);
        }

        public static FileInfo GetFileInfoForExistingTopic(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement)
        {
            return SchemerEditorBase.Xxx(xmlSchemaElementParent, xmlSchemaElement, false);
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
        private XmlSchemaElement _xmlSchemaElementRoot;

        public SchemerElementsLandingPageEditor(FileInfo fileInfo, XmlSchemaElement xmlSchemaElementRoot) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this._xmlSchemaElementRoot = xmlSchemaElementRoot;
        }

        public static FileInfo FormatFileInfo(bool isTopicNew)
        {
            string fileName = SchemerEditorBase.SchemaName!;
            fileName += "-elements.md";
            return new FileInfo((isTopicNew ? SchemerEditorBase.DirectoryInfoForNewTopics!.FullName : SchemerEditorBase.DirectoryInfoForExistingTopics!.FullName) + @"\" + fileName);
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
        protected XmlSchemaElement XmlSchemaElement;

        protected Collection<ChildElementAdapter>? _childElementAdapters = null;
        protected SchemerComplexTypeElementEditor? _parent = null;

        protected XmlSchemaAny? _childXmlSchemaAny = null;

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

        public void ConsoleWriteElementTree(int indentation = 0)
        {
            ProgramBase.ConsoleWriteIndent(indentation);
            ProgramBase.ConsoleWrite(this.XmlSchemaElement.Name + " element", ConsoleWriteStyle.Highlight);

            if (this._childElementAdapters is not null)
            {
                foreach (var childElementAdapter in this._childElementAdapters!)
                {
                    if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
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
            /// If this topic isn't the topic for the root element (so it has a parent), then grab the XmlSchemaElement that represents the parent's xsd element.
            XmlSchemaElement? xmlSchemaElementParent = null;
            xmlSchemaElementParent = this._parent?.XmlSchemaElement;

            // Predict what the path and filename for this element's topic's would be if it already existed, and get a FileInfo for it.
            FileInfo fileInfoPossiblyExisting = SchemerEditorBase.GetFileInfoForExistingTopic(xmlSchemaElementParent, this.XmlSchemaElement);

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

            this.WriteBeginYamlFrontmatter();
            this.WriteYamlFrontmatterTitle(this.RenderTitlePlusElement());

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
            this.WriteYamlFrontmatterApiName(this.XmlSchemaElement.Name!);
            this.WriteYamlFrontmatterApiTypeSchema();
            this.WriteYamlFrontmatterApiLocation(string.Empty);

            if (schemerElementExistingTopicEditor is not null) this.WriteYamlFrontmatterMsAssetId(schemerElementExistingTopicEditor!.GetYamlMsAssetId());

            this.WriteEndYamlFrontmatter();

            this.WriteSectionHeading(1, this.RenderTitlePlusElement());
            if (schemerElementExistingTopicEditor is not null)
            {
                this.Write(schemerElementExistingTopicEditor!.EditorObjectModel.Description!);
            }

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
                this.WriteBulletPoint(EditorBase.RenderHyperlink(this._parent.RenderTitle(), @"./" + this._parent.FileInfo!.Name));
            }
            else
            {
                this.WriteLine(EditorBase.NoneSentenceString);
            }

            this.WriteLine();
        }

        private string RenderTitle()
        {
            if (this._parent is not null)
            {
                return String.Format("{0} ({1})", this.XmlSchemaElement.Name!, this._parent.XmlSchemaElement.Name!);
            }
            else
            {
                return this.XmlSchemaElement.Name!;
            }
        }

        private string RenderTitlePlusElement()
        {
            return this.RenderTitle() + " element";
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
                if (childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis is not null)
                {
                    elementCell = EditorBase.RenderHyperlink(
                        childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.XmlSchemaElement.Name!,
                        @"./" + childElementAdapter.SchemerComplexTypeElementEditorThatIsAChildOfThis.FileInfo!.Name);
                }
                else
                {
                    elementCell = EditorBase.RenderHyperlink(
                        childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!,
                        @"#" + childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.Name!.ToLower());
                    typeCell = childElementAdapter.XmlSchemaElementThatIsAChildOfThis!.SchemaTypeName.Name;
                }
                var rowCells = new List<string>() { elementCell!, typeCell, EditorBase.TBDSentenceString };
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
