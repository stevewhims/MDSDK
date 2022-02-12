using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MDSDKBase;
using MDSDKDerived;
using System.Text.RegularExpressions;

namespace MDSDK
{
    // Also see Win32HeaderLexer.

    // Then, lex all the SDK header files (for functions and structs... anything else?). For each parm/member, look up the type and insert "Type:" plus a link embedded
    // into the parm declaration.

    // sdk-api stubs/vb_release
    // Get the value of the UID: field, trim it, if it starts with NA then ignore the file because it's a meaningless "namespace" file.
    // If it starts with NC then it's a callback function.
    // If it starts with NE then it's an enumeration.

    // The stub filename determines the topic url. Note that the author could have renamed the file in the content branch (as long as the UID is the same), but that's not a great idea.

    /// <summary>
    /// A class that performs lexical analysis on the Win32 and COM API reference build repo.
    /// </summary>
    internal class Win32APIReferenceBuiltTopicsLexer
    {
        /// <summary>
        /// Retrieves an ApiRefModelWin32 object, representing the contents of the Win32 and COM API reference build repo.
        /// </summary>
        public static ApiRefModelWin32 GetApiRefModelWin32()
        {
            var apiRefModelWin32 = new ApiRefModelWin32();

            using (new ChangeAndRestoreCurrentDirectory($"{ProgramBase.MyContentReposFolderDirectoryInfo.FullName}\\{ProgramBase.Win32ApiReferenceBuildRepoName}\\sdk-api-src\\content"))
            {
                string platformDesc = "Win32 and COM";

                List<DirectoryInfo> headerDirectoryInfos = new List<DirectoryInfo>();
                var win32ApiReferenceBuildFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
                foreach (var headerDirectoryInfo in win32ApiReferenceBuildFolder.GetDirectories())
                {
                    string headerDirectoryName = headerDirectoryInfo.Name;
                    if (!headerDirectoryName.StartsWith("_"))
                    {
                        headerDirectoryInfos.Add(headerDirectoryInfo);
                    }
                }

                ProgramBase.ConsoleWrite($"{Environment.NewLine}Lexing {platformDesc} built topics.", ConsoleWriteStyle.Highlight);

                string projectListIntro = $"These are the shipping headers that document {platformDesc} functions.";
                ProgramBase.ConsoleWrite(projectListIntro, ConsoleWriteStyle.Highlight);

                foreach (DirectoryInfo eachHeaderDirectoryInfo in headerDirectoryInfos)
                {
                    ProgramBase.ConsoleWrite($"{eachHeaderDirectoryInfo.Name} ", ConsoleWriteStyle.Default, 0);

                    Win32APIReferenceBuiltTopicsLexer.ProcessHeader(apiRefModelWin32, eachHeaderDirectoryInfo);
                }
                ProgramBase.ConsoleWrite(string.Empty, ConsoleWriteStyle.Default, 2);
            }

            return apiRefModelWin32;
        }

        private static void ProcessHeader(ApiRefModelWin32 apiRefModelWin32, DirectoryInfo eachHeaderDirectoryInfo)
        {
            Editor indexMdEditor = EditorBase.GetEditorForTopicFileName(eachHeaderDirectoryInfo, "index.md", false);
            if (indexMdEditor != null)
            {
                // Free functions.
                Table freeFunctionWin32sTable = indexMdEditor.GetFunctionsInIndexMd();
                if (freeFunctionWin32sTable != null)
                {
                    foreach (var row in freeFunctionWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddFreeFunctionWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }

                // Enumerations.
                Table enumerationWin32sTable = indexMdEditor.GetEnumerationsInIndexMd();
                if (enumerationWin32sTable != null)
                {
                    foreach (var row in enumerationWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddEnumerationWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }

                // Structures.
                Table structureWin32sTable = indexMdEditor.GetStructuresInIndexMd();
                if (structureWin32sTable != null)
                {
                    foreach (var row in structureWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddStructureWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }

                // Interfaces.
                Table interfaceCOMsTable = indexMdEditor.GetInterfacesInIndexMd();
                if (interfaceCOMsTable != null)
                {
                    foreach (var row in interfaceCOMsTable.Rows)
                    {
                        string interface_link_text = null;
                        string interface_link_url = null;
                        (interface_link_text, interface_link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        InterfaceCOM interfaceCOM = apiRefModelWin32.AddInterfaceCOM(interface_link_text, eachHeaderDirectoryInfo.Name + ".h", interface_link_url);

                        // Methods.
                        string header_file_name_normalized = eachHeaderDirectoryInfo.Name.ToLower().Replace(".", "-");
                        string interface_link_text_normalized = interface_link_text.ToLower();
                        int indexOfLastNamespaceSeparator = interface_link_text_normalized.LastIndexOf("::");
                        if (indexOfLastNamespaceSeparator != -1)
                        {
                            interface_link_text_normalized = interface_link_text_normalized.Substring(indexOfLastNamespaceSeparator + 2);
                        }
                        Editor interfaceTopicEditor = EditorBase.GetEditorForTopicFileName(eachHeaderDirectoryInfo, $"nn-{header_file_name_normalized}-{interface_link_text_normalized}.md", false);
                        if (interfaceTopicEditor != null)
                        {
                            Table methodCOMsTable = interfaceTopicEditor.GetMethodsInInterfaceTopic();
                            if (methodCOMsTable != null)
                            {
                                foreach (var methodRow in methodCOMsTable.Rows)
                                {
                                    string method_link_text = null;
                                    string method_link_url = null;
                                    (method_link_text, method_link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                                    apiRefModelWin32.AddMethodCOM(method_link_text, eachHeaderDirectoryInfo.Name + ".h", method_link_url, interfaceCOM);
                                }
                            }
                        }
                    }
                }

                // Callback functions.
                Table callbackFunctionWin32sTable = indexMdEditor.GetCallbackFunctionsInIndexMd();
                if (callbackFunctionWin32sTable != null)
                {
                    foreach (var row in callbackFunctionWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddCallbackFunctionWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }

                // Classes.
                Table classWin32sTable = indexMdEditor.GetClassesInIndexMd();
                if (classWin32sTable != null)
                {
                    foreach (var row in classWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddClassWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }

                // IOCTLs.
                Table ioctlWin32sTable = indexMdEditor.GetIoctlsInIndexMd();
                if (ioctlWin32sTable != null)
                {
                    foreach (var row in ioctlWin32sTable.Rows)
                    {
                        string link_text = null;
                        string link_url = null;
                        (link_text, link_url) = EditorBase.DeconstructMarkdownLink(row.RowCells[0]);
                        apiRefModelWin32.AddIoctlWin32(link_text, eachHeaderDirectoryInfo.Name + ".h", link_url);
                    }
                }
            }
        }
    }
}