using MDSDK;
using MDSDKDerived;
using Microsoft.VisualBasic;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace MDSDKBase
{
    internal enum EditorBaseFileInfoExistenceRequirements
    {
        FileMustAlreadyExist,
        FileMustNotAlreadyExist
    }

    /// <summary>
    /// Abstract base class for a markdown file editor, whether the document is a topic, toc, or other.
    /// Provides common query and update services, maintains a dirty flag, and saves the document on
    /// request. The SDK skeleton project extends EditorBase with a class named Editor. Editor
    /// is the class you add app-specific services to. Augment EditorBase only with common facilities.
    /// </summary>
    internal abstract class EditorBase
    {
        /// <summary>
        /// The underlying FileInfo object representing the document on disk.
        /// </summary>
        public FileInfo? FileInfo = null;
        /// <summary>
        /// The string object representing the entire file contents.
        /// </summary>
        protected string? fileContents = null;
        /// <summary>
        /// A string collection representing each line of the file.
        /// </summary>
        protected List<string> fileLines = new List<string>();
        /// <summary>
        /// The XDocument object representing the xml document. TODO: delete this when possible; it's a holdover from the WDCML version.
        /// </summary>
        protected XDocument? xDocument = null;
        /// <summary>
        /// Represents whether the document is dirty (has unsaved changes) or not. Calling
        /// <see cref="EditorBase.CheckOutAndSaveChangesIfDirty"/> only has an effect if
        /// <see cref="EditorBase.IsDirty"/> is true. The <see cref="EditorBase"/> should
        /// manage this value itself, but if necessary you can force the desired behavior by setting this field.
        /// </summary>
        public bool IsDirty = false;

        private static Regex DeconstructMarkdownLinkRegex = new Regex(@"\[(?<link_text>.*)\]\((?<link_url>.*)\)", RegexOptions.Compiled);
        private static Regex TwoSpacesRegex = new Regex("  ", RegexOptions.Compiled);
        private static Regex MsAssetIdRegex = new Regex(@"ms.assetid: (?<ms_asset_id>.*)", RegexOptions.Compiled);

        private static string IndexMdCallbackFunctionsH2 = "## Callback functions";
        private static string IndexMdClassesH2 = "## Classes";
        private static string IndexMdEnumerationsH2 = "## Enumerations";
        private static string IndexMdFunctionsH2 = "## Functions";
        private static string IndexMdInterfacesH2 = "## Interfaces";
        private static string IndexMdIoctlsH2 = "## IOCTLs";
        private static string IndexMdStructuresH2 = "## Structures";

        private static string InterfaceTopicMethodsH2 = "## Methods";
        private static string StructureTopicStructFieldsH2 = "## -struct-fields";

        private static string YamlFrontmatterDelimiter = "---";
        private static string SyntaxStartDelimiter = "```syntax";
        private static string CodeBlockEndDelimiter = "```";

        /// <summary>
        /// Constructs a new EditorBase.
        /// </summary>
        /// <param name="fileInfo">The file to edit.</param>
        public EditorBase(FileInfo fileInfo, EditorBaseFileInfoExistenceRequirements fileInfoExistenceRequirements = EditorBaseFileInfoExistenceRequirements.FileMustAlreadyExist)
        {
            this.FileInfo = fileInfo;

            if (fileInfoExistenceRequirements == EditorBaseFileInfoExistenceRequirements.FileMustNotAlreadyExist)
            {
                if (this.FileInfo.Exists)
                {
                    ProgramBase.ConsoleWrite(fileInfo.FullName + " already exists.", ConsoleWriteStyle.Error);
                    throw new MDSDKException();
                }
                else
                {
                    using (StreamWriter sw = this.FileInfo.CreateText()) { }
                }
            }

            try
            {
                using (StreamReader? streamReader = this.FileInfo.OpenText())
                {
                    this.fileContents = streamReader.ReadToEnd();
                }
                using (StreamReader? streamReader = this.FileInfo.OpenText())
                {
                    while (!streamReader.EndOfStream)
                    {
                        this.fileLines.Add(streamReader.ReadLine()!.TrimEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                ProgramBase.ConsoleWrite(fileInfo.FullName + " is invalid.", ConsoleWriteStyle.Error);
                ProgramBase.ConsoleWrite(ex.Message, ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }
        }

        public bool IsValid { get { return this.xDocument != null; } }

        #region Methods that don't modify
        public static (string, string) DeconstructMarkdownLink(string markdownLink)
        {
            var matches = EditorBase.DeconstructMarkdownLinkRegex.Matches(markdownLink);
            if (matches.Count == 1)
            {
                return DeconstructMarkdownLinkRecursive(matches[0].Groups["link_text"].Value, matches[0].Groups["link_url"].Value);
            }
            else
            {
                ProgramBase.ConsoleWrite($"Markdown link {markdownLink} is malformed.");
                throw new MDSDKException();
            }
        }

        private static (string, string) DeconstructMarkdownLinkRecursive(string interface_link_text, string interface_link_url)
        {
            var matches = EditorBase.DeconstructMarkdownLinkRegex.Matches("[" + interface_link_text);
            if (matches.Count == 1)
            {
                return DeconstructMarkdownLinkRecursive(matches[0].Groups["link_text"].Value, matches[0].Groups["link_url"].Value);
            }
            else
            {
                return (interface_link_text, interface_link_url);
            }
        }

        public static MatchCollection RetrieveMatchesForTwoSpaces(string content)
        {
            return EditorBase.TwoSpacesRegex.Matches(content);
        }

        /// <summary>
        /// Gets the single unique descendant with the specified name. Throws if the name is not unique.
        /// </summary>
        /// <param name="name">The element's name (without namespace).</param>
        /// <param name="container">The scope to search in. Uses the entire document by default but you can pass another XElement to limit the seach to that element's descendants.</param>
        /// <returns>The element, or null if the element does not exist.</returns>
        public XElement? GetUniqueDescendant(string? name, XContainer? container = null)
        {
            if (container == null) container = this.xDocument;
            List<XElement> elements = this.GetDescendants(name, container);
            if (elements == null || elements.Count == 0) return null;
            if (elements.Count == 1)
            {
                return elements[0];
            }
            else
            {
                ProgramBase.ConsoleWrite("You called GetUniqueDescendant(\"" + name + "\"), but \"" + name + "\" is not unique.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }
        }

        /// <summary>
        /// Gets the first descendant with the specified name.
        /// </summary>
        /// <param name="name">The element's name (without namespace).</param>
        /// <param name="container">The scope to search in. Uses the entire document by default but you can pass another XElement to limit the seach to that element's descendants.</param>
        /// <returns>The element, or null if the element does not exist.</returns>
        public XElement? GetFirstDescendant(string? name, XContainer? container = null)
        {
            if (container == null) container = this.xDocument;
            List<XElement> elements = this.GetDescendants(name, container);
            if (elements != null && elements.Count > 0)
            {
                return elements[0];
            }
            else
            {
                return null;
            }
        }

        public Table? GetFunctionsInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdFunctionsH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetEnumerationsInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdEnumerationsH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetStructuresInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdStructuresH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetInterfacesInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdInterfacesH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetCallbackFunctionsInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdCallbackFunctionsH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetClassesInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdClassesH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetIoctlsInIndexMd()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.IndexMdIoctlsH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        public Table? GetMethodsInInterfaceTopic()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.InterfaceTopicMethodsH2)
                {
                    return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines, ix + 1);
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the 1-based line number of the beginning of the fields section in a Win32 structure topic.
        /// </summary>
        /// <returns>The 1-based line number, or -1 if not found.</returns>
        public int GetFieldsSection1BasedLineNumber()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();
                if (eachLineTrimmed == EditorBase.StructureTopicStructFieldsH2)
                {
                    return ix + 1;
                }
            }
            return -1;
        }

        public string? GetFirstNoBreakSpaceLine()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();

                if (eachLineTrimmed.Contains("\u00A0"))
                {
                    return eachLineTrimmed;
                }
            }
            return null;
        }

        public string? GetFirstAsteriskInYamlDescriptionLine()
        {
            for (int ix = 0; ix < this.fileLines.Count; ++ix)
            {
                string eachLineTrimmed = this.fileLines[ix].Trim();

                if (eachLineTrimmed.StartsWith("description: *"))
                {
                    return eachLineTrimmed;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all descendants, or all descendants with the specified name if one is specified.
        /// </summary>
        /// <param name="name">The element's name (without namespace) or null to return all descendants.</param>
        /// <param name="container">The scope to search in. Uses the entire document by default but you can pass another XElement to limit the seach to that element's descendants.</param>
        /// <returns>The elements, or null if the element does not exist.</returns>
        public List<XElement> GetDescendants(string? name = null, XContainer? container = null)
        {
            if (container == null) container = this.xDocument;
            if (name == null)
            {
                return container!.Descendants().ToList();
            }
            else
            {
                return container!.Descendants(name).ToList();
            }
        }

        /// <summary>
        /// Gets metadata@id as a string.
        /// </summary>
        /// <returns>The value of the id attribute if found, otherwise null.</returns>
        public string? GetMetadataAtId()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? id = metadata.Attribute("id");
                if (id != null) return id.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets metadata@type as a string.
        /// </summary>
        /// <returns>The value of the type attribute if found, otherwise null.</returns>
        public string? GetMetadataAtTypeAsString()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? type = metadata.Attribute("type");
                if (type != null) return type.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets metadata@type as an enum value.
        /// </summary>
        /// <returns>An enum value representing the value of the type attribute.</returns>
        public TopicType GetMetadataAtTypeAsEnum()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? xAttribute = metadata.Attribute("type");
                if (xAttribute != null)
                {
                    if (xAttribute.Value == "attachedmember_winrt")
                    {
                        return TopicType.AttachedPropertyWinRT;
                    }
                    else if (xAttribute.Value == "attribute")
                    {
                        return TopicType.AttributeWinRT; // change the name a little so we're consistent.
                    }
                    else if (xAttribute.Value == "class_winrt")
                    {
                        return TopicType.ClassWinRT;
                    }
                    else if (xAttribute.Value == "delegate")
                    {
                        return TopicType.DelegateWinRT; // change the name a little so we're consistent.
                    }
                    else if (xAttribute.Value == "enum_winrt")
                    {
                        return TopicType.EnumWinRT;
                    }
                    else if (xAttribute.Value == "event_winrt")
                    {
                        return TopicType.EventWinRT;
                    }
                    else if (xAttribute.Value == "function")
                    {
                        return TopicType.MethodWinRT; // change the name so we're consistent. Some WinRT DX topics are like this.
                    }
                    //else if (xAttribute.Value == "iface")
                    //{
                    //	return TopicType.InterfaceWinRT; // change the name so we're consistent. Some WinRT topics are like this (e.g. see w_net_backgrxfer).
                    //}
                    else if (xAttribute.Value == "interface_winrt")
                    {
                        return TopicType.InterfaceWinRT;
                    }
                    //else if (xAttribute.Value == "method")
                    //{
                    //	return TopicType.MethodWinRT; // change the name so we're consistent. Some WinRT topics are like this (e.g. see w_net_backgrxfer).
                    //}
                    else if (xAttribute.Value == "method_overload")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    else if (xAttribute.Value == "method_overload_winrt")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    else if (xAttribute.Value == "method_winrt")
                    {
                        return TopicType.MethodWinRT;
                    }
                    if (xAttribute.Value == "namespace")
                    {
                        return TopicType.NamespaceWinRT; // change the name a little so we're consistent.
                    }
                    else if (xAttribute.Value == "nodepage")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    else if (xAttribute.Value == "ovw")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    else if (xAttribute.Value == "property_winrt")
                    {
                        return TopicType.PropertyWinRT;
                    }
                    else if (xAttribute.Value == "refpage")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    else if (xAttribute.Value == "startpage")
                    {
                        return TopicType.NotYetKnown; // don't process these.
                    }
                    //else if (xAttribute.Value == "struct")
                    //{
                    //	return TopicType.StructWinRT; // change the name a little so we're consistent. Some WinRT DX topics are like this.
                    //}
                    else if (xAttribute.Value == "struct_winrt")
                    {
                        return TopicType.StructWinRT;
                    }
                    else
                    {
                        return TopicType.NotYetKnown;
                    }
                }
            }

            return TopicType.NotYetKnown;
        }

        /// <summary>
        /// Gets metadata@title as a string.
        /// </summary>
        /// <returns>The value of the title attribute if found, otherwise null.</returns>
        public string? GetMetadataAtTitle()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XElement? title = this.GetUniqueDescendant("title", metadata);
                if (title != null) return title.Value;
            }
            return null;
        }

        public string? GetYamlApiName()
        {
            return "ACTRL_ACCESSA";
            //XElement metadata = this.GetUniqueDescendant("metadata");
            //if (metadata != null)
            //{
            //    XAttribute xAttribute = metadata.Attribute("api_name");
            //    if (xAttribute != null)
            //    {
            //        return xAttribute.Value;
            //    }
            //}
            //return null;
        }

        /// <summary>
        /// Gets metadata@title as nodes.
        /// </summary>
        /// <returns>The nodes of the title attribute if found, otherwise null.</returns>
        public IEnumerable<XNode>? GetMetadataAtTitleAsNodes()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XElement? title = this.GetUniqueDescendant("title", metadata);
                if (title != null) return title.Nodes();
            }
            return null;
        }

        public string? GetSyntaxName()
        {
            XElement? syntax = this.GetUniqueDescendant("syntax");
            if (syntax != null)
            {
                XElement? name = this.GetFirstDescendant("name", syntax);
                if (name != null) return name.Value;
            }
            return null;
        }

        public string? GetMetadataAtIntellisenseIdString()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? intellisenseIdStringAttribute = metadata.Attribute("intellisense_id_string");
                if (intellisenseIdStringAttribute != null)
                {
                    return intellisenseIdStringAttribute.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the type name extracted from metadata@intellisense_id_string as a string.
        /// </summary>
        /// <returns>The type name extracted from the intellisense_id_string attribute if found, otherwise null.</returns>
        public string? GetTypeNameFromMetadataAtIntellisenseIdString()
        {
            string? intellisenseIdString = this.GetMetadataAtIntellisenseIdString();
            if (intellisenseIdString != null)
            {
                string typeName = intellisenseIdString;
                int startIndexOfTypeName = typeName.LastIndexOf('.') + 1;
                int lengthOfTypeName = typeName.Length - startIndexOfTypeName;
                typeName = typeName.Substring(startIndexOfTypeName, lengthOfTypeName);
                return typeName;
            }
            return null;
        }

        public string? GetMethodWinRTParametersFromParams()
        {
            string parmsList = string.Empty;
            XElement? paramsEl = this.GetUniqueDescendant("params");
            if (paramsEl != null)
            {
                foreach (XElement param in this.GetDescendants("param", paramsEl))
                {
                    if (parmsList.Length == 0) parmsList = "(";
                    XElement? datatype = this.GetUniqueDescendant("datatype", param);
                    if (datatype == null) continue;
                    XElement? xref = this.GetUniqueDescendant("xref", datatype);
                    if (xref == null) continue;

                    if (parmsList.Length > 1) parmsList += ", ";
                    parmsList += xref.Value;
                }
                if (parmsList.Length > 0) parmsList += ")";
            }
            return parmsList;
        }

        /// <summary>
        /// Gets applicationplatform@name as a string.
        /// </summary>
        /// <returns>The value of the name attribute if found, otherwise null.</returns>
        public string? GetApplicationPlatformAtName()
        {
            XElement? applicationPlatform = this.GetUniqueDescendant("ApplicationPlatform");
            if (applicationPlatform != null)
            {
                XAttribute? name = applicationPlatform.Attribute("name");
                if (name != null) return name.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets applicationplatform@friendlyname as a string.
        /// </summary>
        /// <returns>The value of the friendlyname attribute if found, otherwise null.</returns>
        public string? GetApplicationPlatformAtFriendlyName()
        {
            XElement? applicationPlatform = this.GetUniqueDescendant("ApplicationPlatform");
            if (applicationPlatform != null)
            {
                XAttribute? friendlyName = applicationPlatform.Attribute("friendlyName");
                if (friendlyName != null) return friendlyName.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets applicationplatform@version as a string.
        /// </summary>
        /// <returns>The value of the friendlyname attribute if found, otherwise null.</returns>
        public string? GetApplicationPlatformAtVersion()
        {
            XElement? applicationPlatform = this.GetUniqueDescendant("ApplicationPlatform");
            if (applicationPlatform != null)
            {
                XAttribute? version = applicationPlatform.Attribute("version");
                if (version != null) return version.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the xref element inside either applies@class or applies@iface as a string.
        /// </summary>
        /// <returns>The xref element if found, otherwise null.</returns>
        public string? GetAppliesClassOrIfaceXrefAtRid()
        {
            XElement? applies = this.GetUniqueDescendant("applies");
            if (applies != null)
            {
                XElement? classEl = this.GetUniqueDescendant("class", applies);
                if (classEl != null)
                {
                    return this.GetAtRidForUniqueXrefDescendant(classEl);
                }
                else
                {
                    XElement? iface = this.GetUniqueDescendant("iface", applies);
                    if (iface != null)
                    {
                        return this.GetAtRidForUniqueXrefDescendant(iface);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets @rid for the unique xref descendant of the specified element.
        /// </summary>
        /// <param name="xElement">The element whose xref descendant to find.</param>
        /// <returns>The value of the rid attribute if found, otherwise null.</returns>
        public string? GetAtRidForUniqueXrefDescendant(XElement? xElement)
        {
            XElement? xref = this.GetUniqueDescendant("xref", xElement);
            if (xref != null)
            {
                XAttribute? rid = xref.Attribute("rid");
                if (rid != null) return rid.Value;
            }

            return null;
        }

        /// <summary>
        /// Gets xref elements whose @hlink contains the specified substring.
        /// </summary>
        /// <param name="substring">The substring to search for</param>
        /// <param name="caseSensitive">True if the comparison should be case-sensitive, otherwise false.</param>
        /// <param name="container">The scope to search in. Uses the entire document by default but you can pass another XElement to limit the seach to that element's descendants.</param>
        /// <returns>A list of matching xref elements.</returns>
        public List<XElement> GetXrefsWhereAtHlinkContains(string substring, bool caseSensitive = false, XContainer? container = null)
        {
            if (container == null) container = this.xDocument;
            if (!caseSensitive) substring = substring.ToLower();
            List<XElement> xrefsWhereAtHlinkContains = new List<XElement>();
            foreach (XElement eachXref in this.GetDescendants("xref", container))
            {
                XAttribute? hlink = eachXref.Attribute("hlink");
                if (hlink != null)
                {
                    string hlinkValue = hlink.Value;
                    if (!caseSensitive) hlinkValue = hlinkValue.ToLower();
                    if (hlinkValue.Contains(substring)) xrefsWhereAtHlinkContains.Add(eachXref);
                }
            }
            return xrefsWhereAtHlinkContains;
        }

        public List<XElement> GetXrefsForRid(string ridString, bool caseSensitive = false, XContainer? container = null)
        {
            if (container == null) container = this.xDocument;
            if (!caseSensitive) ridString = ridString.ToLower();
            List<XElement> xrefsForRid = new List<XElement>();
            foreach (XElement eachXref in this.GetDescendants("xref", container))
            {
                XAttribute? ridAttribute = eachXref.Attribute("rid");
                if (ridAttribute != null)
                {
                    string ridValue = ridAttribute.Value;
                    if (!caseSensitive) ridValue = ridValue.ToLower();
                    if (ridValue == ridString) xrefsForRid.Add(eachXref);
                }
            }
            return xrefsForRid;
        }

        /// <summary>
        /// Factory method that creates an Editor for the named file that is inside
        /// the specified folder. Optionally throws if file not found.
        /// </summary>
        /// <param name="directoryInfo">The folder containing the file.</param>
        /// <param name="fileName">The name of the file for which to retrieve an editor.</param>
        /// <returns>An Editor object for the file in the folder.</returns>
        public static Editor? GetEditorForTopicFileName(DirectoryInfo directoryInfo, string fileName, bool throwIfNotFound = true)
        {
            List<FileInfo> fileInfos = directoryInfo.GetFiles(fileName).ToList();
            if (fileInfos.Count == 1)
            {
                return new Editor(fileInfos[0]);
            }

            ProgramBase.ConsoleWrite($"folder \"{directoryInfo.Name}\" doesn't contain \"{fileName}\" ", ConsoleWriteStyle.Error, 0);
            if (throwIfNotFound) throw new MDSDKException();
            return null;
        }

        /// <summary>
        /// Factory method that creates an Editor for the xtoc file that is inside, and
        /// that has the same name as, the specified project folder. Throws if not exactly
        /// one xtoc file is found whose name matches the folder name.
        /// </summary>
        /// <param name="projectDirectoryInfo">The folder containing the project.</param>
        /// <returns>An Editor object for the xtoc file.</returns>
        public static Editor GetEditorForXtoc(DirectoryInfo projectDirectoryInfo)
        {
            List<FileInfo> xtocFiles = projectDirectoryInfo.GetFiles(projectDirectoryInfo.Name + ".xtoc").ToList();
            if (xtocFiles.Count != 1)
            {
                ProgramBase.ConsoleWrite(string.Format("Project folder {0} does not contain {1}", projectDirectoryInfo.Name, projectDirectoryInfo.Name + ".xtoc"), ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            return new Editor(xtocFiles[0]);
        }

        /// <summary>
        /// Factory method that creates an Editor for each topic file inside the specified folder.
        /// </summary>
        /// <param name="folderDirectoryInfo">A DirectoryInfo representing the folder.</param>
        /// <returns>A list of Editor objects for the topics.</returns>
        public static List<Editor> GetEditorsForTopicsInFolder(DirectoryInfo folderDirectoryInfo)
        {
            List<FileInfo> fileInfos = EditorBase.GetFileInfosForTopicsInFolder(folderDirectoryInfo);

            List<Editor> editors = new List<Editor>();
            foreach (FileInfo eachFileInfo in fileInfos)
            {
                //try
                {
                    editors.Add(new Editor(eachFileInfo));
                }
                //catch (MDSDKException){}
            }
            return editors;
        }

        /// <summary>
        /// Factory method that creates a FileInfo for each topic file inside the specified folder.
        /// </summary>
        /// <param name="folderDirectoryInfo">A DirectoryInfo representing the folder.</param>
        /// <returns>A list of FileInfo objects for the topics.</returns>
        public static List<FileInfo> GetFileInfosForTopicsInFolder(DirectoryInfo folderDirectoryInfo)
        {
            return EditorBase.GetFileInfosForTopicsInXtoc(folderDirectoryInfo, EditorBase.GetEditorForXtoc(folderDirectoryInfo));
        }

        private static List<FileInfo> GetFileInfosForTopicsInXtoc(DirectoryInfo projectDirectoryInfo, Editor xtocEditor)
        {
            List<FileInfo> topicFileInfos = new List<FileInfo>();
            List<XElement> nodes = xtocEditor.GetDescendants("node");
            foreach (XElement eachNode in nodes)
            {
                XAttribute? topicUrlAttribute = eachNode.Attribute("topicURL");
                if (topicUrlAttribute != null && EditorBase.IsTopicPublishedToMsdn(eachNode))
                {
                    //string topicFilename = topicUrlAttribute.Value.Substring(topicUrlAttribute.Value.IndexOf('/') + 1);
                    FileInfo? fileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, topicUrlAttribute.Value));
                    if (fileInfo != null)
                    {
                        topicFileInfos.Add(fileInfo);
                    }
                }
            }

            List<XElement> includes = xtocEditor.GetDescendants("include");
            foreach (XElement eachInclude in includes)
            {
                XAttribute? urlAttribute = eachInclude.Attribute("url");
                if (urlAttribute != null && EditorBase.IsTopicPublishedToMsdn(eachInclude))
                {
                    //string topicFilename = urlAttribute.Value.Substring(urlAttribute.Value.IndexOf('/') + 1);
                    FileInfo? fileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, urlAttribute.Value));
                    if (fileInfo != null)
                    {
                        Editor? includedXtocEditor = new Editor(fileInfo);
                        topicFileInfos.AddRange(EditorBase.GetFileInfosForTopicsInXtoc(projectDirectoryInfo, includedXtocEditor));
                    }
                }
            }

            return topicFileInfos;
        }

        /// <summary>
        /// Determines whether a topic is present in the xtoc and published to msdn. Call this
        /// method on an Editor that represents an xtoc file.
        /// </summary>
        /// <param name="topicUrl">The url (or filename) of the topic in the form of an xtoc node@topicURL value.</param>
        /// <returns>True if the topic is present in the xtoc and published to msdn, otherwise false.</returns>
        public bool IsTopicUrlInXtocAndPublishedToMsdn(string? topicUrl)
        {
            List<XElement> nodes = this.GetDescendants("node");
            foreach (XElement eachNode in nodes)
            {
                XAttribute? topicUrlAttribute = eachNode.Attribute("topicURL");
                if (topicUrlAttribute != null && topicUrlAttribute.Value == topicUrl)
                {
                    return EditorBase.IsTopicPublishedToMsdn(eachNode);
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a section with the specified id exists.
        /// </summary>
        /// <param name="sectionId">The section id to search for.</param>
        /// <returns>True if a section with the specified id exists, otherwise false.</returns>
        public bool DoesSectionWithThisIdExist(string? sectionId)
        {
            foreach (XElement section in this.GetDescendants("section"))
            {
                XAttribute? idAttribute = section.Attribute("id");
                if (idAttribute != null && idAttribute.Value == sectionId) return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a topic is published to msdn based on an xtoc node element.
        /// </summary>
        /// <param name="node">A node element from an xtoc that represents a topic.</param>
        /// <returns>True if the topic is published to msdn (there is no msdn filter), otherwise false.</returns>
        private static bool IsTopicPublishedToMsdn(XElement node)
        {
            return node.Attribute("filter_msdn") == null;
        }

        /// <summary>
        /// Gets a list of interfaces impemented by the type topic represented by the Editor.
        /// </summary>
        /// <returns>A list of interface names as strings.</returns>
        public List<string>? GetInterfacesImplemented()
        {
            List<string>? interfacesImplemented = null;
            XElement? inheritance = this.GetUniqueDescendant("inheritance");
            if (inheritance != null)
            {
                List<XElement>? ancestors = this.GetDescendants("ancestor", inheritance);
                foreach (XElement eachAncestor in ancestors)
                {
                    XAttribute? access_level = eachAncestor.Attribute("access_level");
                    if (access_level != null && access_level.Value == "private")
                    {
                        XElement? xref = this.GetUniqueDescendant("xref", eachAncestor);
                        if (xref != null)
                        {
                            XAttribute? targtype = xref.Attribute("targtype");
                            if (targtype!.Value == "interface_winrt")
                            {
                                if (interfacesImplemented == null) interfacesImplemented = new List<string>();
                                interfacesImplemented.Add(xref.Value);
                            }
                        }
                    }
                }
            }
            return interfacesImplemented;
        }
        #endregion

        #region Methods that modify
        // Methods that modify. Set this.IsDirty to true only you modify the document directly, not
        // if you call a method that already does so.

        /// <summary>
        /// Returns a copy of the first markdown table in the topic, or null if no markdown table is found.
        /// </summary>
        /// <returns>A Table object.</returns>
        public Table? GetFirstTable()
        {
            return Table.GetNextTable(this.FileInfo!.FullName, this.fileLines);
        }

        /// <summary>
        /// Constructs a new element with the specified name, optionally specified content, and optionally specified parent element.
        /// If the parent element exists in the document represented by the Editor then the Editor marks itself dirty.
        /// </summary>
        /// <param name="name">The name to give the new element (without namespace).</param>
        /// <param name="content">Optional content to put inside the new element.</param>
        /// <param name="parentTheNewElementToThisElement">Optional element to parent the new element to (the parent can be, but doesn't have to be, inside the document represented by the Editor).</param>
        /// <returns>The new XElement.</returns>
        public XElement NewXElement(string name, object? content = null, XElement? parentTheNewElementToThisElement = null)
        {
            XElement? xElement = new XElement(name, content);
            if (parentTheNewElementToThisElement != null)
            {
                parentTheNewElementToThisElement.Add(xElement);
            }

            // If the parent element is in the document then dirty the document, otherwise don't.
            if (parentTheNewElementToThisElement != null && this.GetDescendants().Contains(parentTheNewElementToThisElement)) this.IsDirty = true;

            return xElement;
        }

        /// <summary>
        /// Sets the specified attribute on the specified element to the specified value. If the element exists
        /// in the document represented by the Editor then the Editor marks itself dirty.
        /// </summary>
        /// <param name="xElement"></param>
        /// <param name="attributeName"></param>
        /// <param name="value"></param>
        public void SetAttributeValue(XElement? xElement, string attributeName, string? value)
        {
            if (xElement != null)
            {
                xElement.SetAttributeValue(attributeName, value);
                // If the parent element is in the document then dirty the document, otherwise don't.
                if (this.GetDescendants().Contains(xElement)) this.IsDirty = true;
            }
        }
        /// <summary>
        /// Sets metadata@title to a specified string value.
        /// </summary>
        /// <param name="titleAsString">The title to set.</param>
        public void SetMetadataAtTitle(string titleAsString)
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XElement? title = this.GetUniqueDescendant("title", metadata);
                if (title != null)
                {
                    title.Value = titleAsString;
                    this.IsDirty = true;
                }
            }
        }

        public List<string> GetLibraryFilenames()
        {
            var libraryFilenames = new List<string>();

            XElement? content = this.GetUniqueDescendant("content");
            if (content != null)
            {
                XElement? info = this.GetUniqueDescendant("info", content);
                if (info != null)
                {
                    List<XElement> libraries = this.GetDescendants("library", info);
                    if (libraries != null)
                    {
                        foreach (var library in libraries)
                        {
                            XElement? filename = this.GetUniqueDescendant("filename", library);
                            if (filename != null)
                            {
                                libraryFilenames.Add(filename.Value);
                            }
                        }
                    }
                }
            }
            return libraryFilenames;
        }

        public string? GetMetadataAtBeta()
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? beta = metadata.Attribute("beta");
                if (beta != null)
                {
                    return beta.Value;
                }
            }
            return "0";
        }

        public void SetMetadataAtBeta(string betaAttributeValue)
        {
            XElement? metadata = this.GetUniqueDescendant("metadata");
            if (metadata != null)
            {
                XAttribute? beta = metadata.Attribute("beta");
                if (beta != null)
                {
                    beta.Value = betaAttributeValue;
                }
                else
                {
                    metadata.Add(new XAttribute("beta", betaAttributeValue));
                }
            }
            this.IsDirty = true;
        }

        /// <summary>
        /// Sets node@text (the xtoc title) to a specified string value for the specified node@topicURL value.
        /// Call this method on an Editor that represents an xtoc file.
        /// </summary>
        /// <param name="topicUrl">The node@topicURL identifying the topic whose xtoc title you want to set.</param>
        /// <param name="text">The xtoc title to set.</param>
        public void SetXtocNodeAtTextForTopicUrl(string? topicUrl, string? text)
        {
            List<XElement> nodes = this.GetDescendants("node");
            foreach (XElement eachNode in nodes)
            {
                XAttribute? topicUrlAttribute = eachNode.Attribute("topicURL");
                if (topicUrlAttribute != null && topicUrlAttribute.Value == topicUrl)
                {
                    eachNode.SetAttributeValue("text", text);
                    this.IsDirty = true;
                    return;
                }
            }
        }

        /// <summary>
        /// If one or both of the device_families and api_contracts elements is missing, create an empty one.
        /// </summary>
        public void EnsureAtLeastEmptyDeviceFamiliesAndApiContractsElements()
        {
            XElement? addAfterMe = this.GetUniqueDescendant("max_os");
            if (addAfterMe == null)
            {
                addAfterMe = this.GetUniqueDescendant("min_os");
                if (addAfterMe == null)
                {
                    addAfterMe = this.GetUniqueDescendant("info");
                }
            }

            if (addAfterMe != null)
            {
                XElement? device_families = this.GetUniqueDescendant("device_families");
                if (device_families == null)
                {
                    device_families = this.NewXElement("device_families");
                    addAfterMe.AddAfterSelf(device_families);
                    this.IsDirty = true;
                }

                XElement? api_contracts = this.GetUniqueDescendant("api_contracts");
                if (api_contracts == null)
                {
                    device_families.AddAfterSelf(this.NewXElement("api_contracts"));
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Delete every section element from the document.
        /// </summary>
        public void DeleteAllSections()
        {
            List<XElement> sections = this.GetDescendants("section");
            foreach (XElement eachSection in sections)
            {
                eachSection.Remove();
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// If the document is dirty then check it out (using sd edit) and save it to disk.
        /// Log success or failure.
        /// </summary>
        public void CheckOutAndSaveChangesIfDirty()
        {
            if (!this.IsDirty) return;

            string fileName = this.FileInfo!.FullName;
            try
            {
                if (ProgramBase.LiveRun)
                {
                    Interaction.Shell(string.Format("sd edit {0}", fileName), AppWinStyle.Hide, true);
                }
                this.xDocument!.Save(fileName);
                ProgramBase.FilesSavedLog!.Add(fileName);
            }
            catch (System.Exception ex)
            {
                ProgramBase.FileSaveErrorsLog!.Add(string.Format("{0}", ex.Message));
            }

            this.IsDirty = false;
        }
        #endregion

        public string? GetYamlMsAssetId()
        {
            var matches = EditorBase.MsAssetIdRegex.Matches(this.fileContents!);
            if (matches.Count == 1)
            {
                return matches[0].Groups["ms_asset_id"].Value.Trim();
            }
            return null;
        }

        public void Write(string text)
        {
            using (StreamWriter streamWriter = this.FileInfo!.AppendText())
            {
                streamWriter.Write(text);
            }
        }

        public void WriteLine(string? text = null)
        {
            using (StreamWriter streamWriter = this.FileInfo!.AppendText())
            {
                streamWriter.WriteLine(text ?? String.Empty);
            }
        }

        public const int NumberOfCharsToIndentIncrement = 4;
        public static char IndentationChar = ' ';
        public void WriteIndent(int numberOfCharsToIndent)
        {
            string indentation = string.Empty;
            for (int ix = 0; ix < numberOfCharsToIndent; ix++) indentation += EditorBase.IndentationChar;
            this.Write(indentation);
        }

        public static void IncrementIndent(ref int numberOfCharsToIndent)
        {
            numberOfCharsToIndent += EditorBase.NumberOfCharsToIndentIncrement;
        }

        public static void DecrementIndent(ref int numberOfCharsToIndent)
        {
            numberOfCharsToIndent -= EditorBase.NumberOfCharsToIndentIncrement;
        }

        public void WriteBeginYamlFrontmatter()
        {
            this.WriteLine(EditorBase.YamlFrontmatterDelimiter);
        }

        public void WriteEndYamlFrontmatter()
        {
            this.WriteLine(EditorBase.YamlFrontmatterDelimiter);
        }

        private void WriteYamlFrontmatterKeyValuePair(string key, string value)
        {
            this.WriteLine(key + ": " + value);
        }

        private void WriteYamlFrontmatterKeyValues(string key, string[] values)
        {
            this.WriteLine(key + ": ");
            foreach (string value in values)
            {
                this.WriteLine("- " + value);
            }
        }

        public void WriteYamlFrontmatterTitle(string value)
        {
            WriteYamlFrontmatterKeyValuePair("title", value);
        }

        public void WriteYamlFrontmatterDescription(string value)
        {
            WriteYamlFrontmatterKeyValuePair("description", value);
        }

        public void WriteYamlFrontmatterMsAssetId(string? value)
        {
            if (value != null) WriteYamlFrontmatterKeyValuePair("ms.assetid", value);
        }

        public void WriteYamlFrontmatterMsTopicReference()
        {
            WriteYamlFrontmatterKeyValuePair("ms.topic", "reference");
        }

        public void WriteYamlFrontmatterMsDate()
        {
            WriteYamlFrontmatterKeyValuePair("ms.date", DateTime.Now.ToString("MM/dd/yy"));
        }

        public void WriteYamlFrontmatterTopicTypeAPIRefKbSyntax()
        {
            WriteYamlFrontmatterKeyValues("topic_type", new string[] { "APIRef", "kbSyntax" });
        }

        public void WriteYamlFrontmatterApiName(string value)
        {
            WriteYamlFrontmatterKeyValues("api_name", new string[] { value });
        }

        public void WriteYamlFrontmatterApiTypeSchema()
        {
            WriteYamlFrontmatterKeyValues("api_type", new string[] { "Schema" });
        }

        public void WriteYamlFrontmatterApiLocation(string value)
        {
            WriteYamlFrontmatterKeyValuePair("api_location", value);
        }

        public void WriteSectionHeading(int hLevel, string heading)
        {
            this.WriteLine();
            string headingText = string.Empty;
            for (int ix = 0; ix < hLevel; ix++) headingText += '#';
            headingText += " " + heading;
            this.WriteLine(headingText);
            this.WriteLine();
        }

        public void WriteBeginSyntax()
        {
            this.WriteLine(EditorBase.SyntaxStartDelimiter);
        }

        public void WriteBeginComplexTypeElement(XmlSchemaElement xmlSchemaElement, ref int numberOfCharsToIndent)
        {
            this.WriteOpeningElementTag(xmlSchemaElement, ref numberOfCharsToIndent);
            EditorBase.IncrementIndent(ref numberOfCharsToIndent);

            // Opening complexType tag.
            this.WriteIndent(numberOfCharsToIndent);
            this.WriteLine("<xs:complexType>");
            EditorBase.IncrementIndent(ref numberOfCharsToIndent);

            // Opening sequence tag.
            this.WriteIndent(numberOfCharsToIndent);
            this.WriteLine("<xs:sequence>");
            EditorBase.IncrementIndent(ref numberOfCharsToIndent);
        }

        public void WriteOpeningElementTag(XmlSchemaElement xmlSchemaElement, ref int numberOfCharsToIndent)
        {
            // Opening element tag.
            this.WriteIndent(numberOfCharsToIndent);
            this.Write("<xs:element name=\"" + xmlSchemaElement.Name + "\"");
            if (xmlSchemaElement.MinOccurs == 0)
            {
                this.WriteLine("");
                this.WriteIndent(numberOfCharsToIndent + EditorBase.NumberOfCharsToIndentIncrement);
                this.WriteLine("minOccurs=\"0\"");
                this.WriteIndent(numberOfCharsToIndent + 1);
                this.WriteLine(">");
            }
            else
            {
                this.WriteLine(">");
            }
        }

        public void WriteEndComplexTypeElement(ref int numberOfCharsToIndent)
        {
            // Closing sequence tag.
            EditorBase.DecrementIndent(ref numberOfCharsToIndent);
            this.WriteIndent(numberOfCharsToIndent);
            this.WriteLine("</xs:sequence>");

            // Closing complexType tag.
            EditorBase.DecrementIndent(ref numberOfCharsToIndent);
            this.WriteIndent(numberOfCharsToIndent);
            this.WriteLine("</xs:complexType>");

            EditorBase.DecrementIndent(ref numberOfCharsToIndent);
            this.WriteIndent(numberOfCharsToIndent);
            this.WriteLine("</xs:element>");
        }

        public void WriteEndSyntax()
        {
            this.WriteLine(EditorBase.CodeBlockEndDelimiter);
        }

        public void WriteSectionHeadingChildElements()
        {
            this.WriteSectionHeading(2, "Child elements");
        }

        public void WriteSectionHeadingRemarks()
        {
            this.WriteSectionHeading(2, "Remarks");
        }

        public void WriteSectionHeadingRequirements()
        {
            this.WriteSectionHeading(2, "Requirements");
        }
    }
}
