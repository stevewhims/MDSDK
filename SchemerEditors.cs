using MDSDKBase;
using MDSDKDerived;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        public SchemerEditorBase(FileInfo fileInfo) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist) { }

        public static DirectoryInfo? DirectoryInfoForExistingTopics = null;
        public static DirectoryInfo? DirectoryInfoForGeneratedTopics = null;
        public static string? SchemaName = null;

        public static FileInfo FormatFileInfo(XmlSchemaElement? xmlSchemaElementParent, XmlSchemaElement xmlSchemaElement)
        {
            string fileName = SchemerEditorBase.SchemaName!;
            fileName += "-" + xmlSchemaElement.Name!.ToLower();
            if (xmlSchemaElementParent != null)
            {
                fileName += "-" + xmlSchemaElementParent.Name!.ToLower();
            }
            fileName += "-element.md";
            return new FileInfo(SchemerEditorBase.DirectoryInfoForGeneratedTopics!.FullName + fileName);
        }
    }

    /// <summary>
    /// Topic for an elements landing page, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-elements.
    /// </summary>
	internal class SchemerElementsTopic : SchemerEditorBase
    {
        public SchemerElementsTopic(FileInfo fileInfo) : base(fileInfo) { }

        // Methods that don't modify.

        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.
    }

    /// <summary>
    /// Topic for a parent element topic, such as https://learn.microsoft.com/windows/win32/nativewifi/wlan-profileschema-macrandomization-wlanprofile-element.
    /// </summary>
	internal class SchemerParentElementTopic : SchemerEditorBase
    {
        public SchemerParentElementTopic(FileInfo fileInfo, XmlSchemaElement xmlSchemaElement) : base(fileInfo)
        {
            Console.WriteLine("Creating topic: {0}", fileInfo.Name);

            this._childElementTopics = new Collection<SchemerParentElementTopic>();
        }

        protected Collection<SchemerParentElementTopic>? _childElementTopics;
        ReadOnlyCollection<SchemerParentElementTopic>? _roCache;

        public void Add(SchemerParentElementTopic parentElementTopic)
        {
            _childElementTopics!.Add(parentElementTopic);
        }

        public ReadOnlyCollection<SchemerParentElementTopic> ChildElementTopics
        {
            get
            {
                _roCache ??= new ReadOnlyCollection<SchemerParentElementTopic>(_childElementTopics!);

                return _roCache;

            }
        }

        // Methods that don't modify.

        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.
    }
}
