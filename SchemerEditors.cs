using MDSDK;
using MDSDKBase;
using MDSDKDerived;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
	internal class SchemerAllElementsGeneratedTopic : SchemerEditorBase
    {
        private XmlSchemaElement? _xmlSchemaElementRoot = null;

        public SchemerAllElementsGeneratedTopic(FileInfo fileInfo, XmlSchemaElement xmlSchemaElementRoot) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
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
	internal class SchemerParentElementGeneratedTopic : SchemerEditorBase
    {
        protected Collection<SchemerParentElementGeneratedTopic>? _childElementTopics;
        protected SchemerParentElementGeneratedTopic? _parent = null;

        protected XmlSchemaElement? _xmlSchemaElement = null;
        public string? XmlSchemaElementName { get { return this._xmlSchemaElement!.Name; } }

        public SchemerParentElementGeneratedTopic(FileInfo fileInfo, XmlSchemaElement xmlSchemaElement) : base(fileInfo, EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
        {
            this._xmlSchemaElement = xmlSchemaElement;
        }

        public void Add(SchemerParentElementGeneratedTopic parentTopicThatIsAChildOfThis)
        {
            if (this._childElementTopics == null)
            {
                this._childElementTopics = new Collection<SchemerParentElementGeneratedTopic>();
            }

            _childElementTopics!.Add(parentTopicThatIsAChildOfThis);
            parentTopicThatIsAChildOfThis._parent = this;
        }

        // Methods that don't modify.

        public void PrintElementTree(int indentation = 0)
        {
            for (int i = 0; i < indentation; i++)
            {
                ProgramBase.ConsoleWrite(" ", ConsoleWriteStyle.Default, 0);
            }

            ProgramBase.ConsoleWrite(this.XmlSchemaElementName + " element");

            if (this._childElementTopics != null)
            {
                foreach (var childElementTopic in this._childElementTopics)
                {
                    childElementTopic.PrintElementTree(indentation + 2);
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
            FileInfo fileInfoExisting = SchemerEditorBase.FormatFileInfo(xmlSchemaElementParent, this._xmlSchemaElement!, false);
            SchemerElementExistingTopic schemerElementExistingTopic = new SchemerElementExistingTopic(fileInfoExisting);

            this.BeginYamlFrontmatter();
            this.WriteYamlFrontmatterTitle(this.XmlSchemaElementName + " element");

            this.WriteYamlFrontmatterDescription("TBD");

            this.WriteYamlFrontmatterMsAssetId(schemerElementExistingTopic.GetYamlMsAssetId());

            this.WriteYamlFrontmatterMsTopicReference();
            this.WriteYamlFrontmatterMsDate();
            this.WriteYamlFrontmatterTopicTypeAPIRefKbSyntax();
            this.WriteYamlFrontmatterApiName(this.XmlSchemaElementName!);
            this.WriteYamlFrontmatterApiTypeSchema();

            this.WriteYamlFrontmatterApiLocation(string.Empty);
            this.EndYamlFrontmatter();

            this.WriteSectionHeading(1, this.XmlSchemaElementName!);

            this.WriteSyntax();

            this.WriteSectionHeadingChildElements();

            this.WriteSectionHeadingRemarks();

            this.WriteSectionHeadingRequirements();

            if (this._childElementTopics != null)
            {
                foreach (var childElementTopic in this._childElementTopics)
                {
                    childElementTopic.Generate();
                }
            }
        }

        public void Commit()
        {
            if (this._childElementTopics != null)
            {
                foreach (var childElementTopic in this._childElementTopics)
                {
                    childElementTopic.Commit();
                }
            }
        }
    }
}

//The LANProfile element contains a wired network profile. This element is the unique root element for a wired network profile.

//The target namespace for the LANProfile element is `https://www.microsoft.com/networking/LAN/profile/v1`.

//``` syntax
//<xs:element name="LANProfile">
//    <xs:complexType>
//        <xs:sequence>
//            <xs:element name = "MSM" >
//                < xs:complexType>
//                    <xs:sequence>
//                        <xs:element name = "security" >
//                            < xs:complexType>
//                                <xs:sequence>
//                                    <xs:element name = "OneXEnforced"
//                                        type="boolean"
//                                     />
//                                    <xs:element name = "OneXEnabled"
//                                        type="boolean"
//                                     />
//                                    <xs:any
//                                        processContents = "lax"
//                                        minOccurs="0"
//                                        maxOccurs="unbounded"
//                                        namespace="##other"
//                                     />
//                                </xs:sequence>
//                            </xs:complexType>
//                        </xs:element>
//                        <xs:any
//                            processContents = "lax"
//                            minOccurs="0"
//                            maxOccurs="unbounded"
//                            namespace="##other"
//                         />
//                    </xs:sequence>
//                </xs:complexType>
//            </xs:element>
//            <xs:any
//                processContents = "lax"
//                minOccurs="0"
//                maxOccurs="unbounded"
//                namespace="##other"
//             />
//        </xs:sequence>
//    </xs:complexType>
//</xs:element>
//```

//## Child elements

//| Element | Type | Description |
//|-|-|-|
//| [**MSM**](lan-profileschema-msm-lanprofile-element.md) | | Contains media-specific module(MSM) settings. |
//| [**OneXEnabled**] (lan-profileschema-onexenabled-security-element.md) | boolean | Specifies whether the automatic configuration service for wired networks will attempt port authentication using 802.1X. |
//| [**OneXEnforced**] (lan-profileschema-onexenforced-security-element.md) | boolean | Specifies whether the automatic configuration service for wired networks requires the use of 802.1X for port authentication. |
//| [**security**] (lan-profileschema-security-msm-element.md) | | Contains security settings. |

//## Remarks

//To view the list of child elements in a tree-like structure, see[LAN\_profile Schema Elements](lan-profileschema-elements.md).

//## Requirements

//| Requirement | Value |
//|- | -|
//| Minimum supported client | Windows Vista \[desktop apps only\] |
//| Minimum supported server | Windows Server 2008 \[desktop apps only\] |
