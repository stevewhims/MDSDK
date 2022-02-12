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
    /// <summary>
    /// A class that performs lexical analysis on the Win32 and COM API reference content repo.
    /// </summary>
    internal class Win32APIReferenceContentTopicsLexer
    {
        public static void DocumentTypesForParameters(ApiRefModelWin32 apiRefModelWin32)
        {
            using (new ChangeAndRestoreCurrentDirectory($"{ProgramBase.MyContentReposFolderDirectoryInfo.FullName}\\{ProgramBase.Win32ApiReferenceContentRepoName}\\sdk-api-src\\content"))
            {
                string platformDesc = "Win32 and COM";

                List<DirectoryInfo> headerDirectoryInfos = Win32APIReferenceContentTopicsLexer.GetHeaderDirectoryInfos();

                ProgramBase.ConsoleWrite($"{Environment.NewLine}Lexing {platformDesc} content topics.", ConsoleWriteStyle.Highlight);

                string projectListIntro = $"These are the shipping headers that document {platformDesc} functions.";
                ProgramBase.ConsoleWrite(projectListIntro, ConsoleWriteStyle.Highlight);

                foreach (DirectoryInfo eachHeaderDirectoryInfo in headerDirectoryInfos)
                {
                    ProgramBase.ConsoleWrite($"{eachHeaderDirectoryInfo.Name} ", ConsoleWriteStyle.Default, 0);
                    Win32APIReferenceContentTopicsLexer.GetApiRefModelWin32ProcessHeader(apiRefModelWin32, eachHeaderDirectoryInfo);
                }
                ProgramBase.ConsoleWrite(string.Empty, ConsoleWriteStyle.Default, 2);
            }
        }

        public static void ReportAnyFirstAsteriskInYamlDescription()
        {
            using (new ChangeAndRestoreCurrentDirectory($"{ProgramBase.MyContentReposFolderDirectoryInfo.FullName}\\{ProgramBase.Win32ApiReferenceContentRepoName}\\sdk-api-src\\content"))
            {
                string platformDesc = "Win32 and COM";

                List<DirectoryInfo> headerDirectoryInfos = Win32APIReferenceContentTopicsLexer.GetHeaderDirectoryInfos();

                ProgramBase.ConsoleWrite($"{Environment.NewLine}Lexing {platformDesc} content topics.", ConsoleWriteStyle.Highlight);

                string projectListIntro = $"These are the shipping headers that document {platformDesc} functions.";
                ProgramBase.ConsoleWrite(projectListIntro, ConsoleWriteStyle.Highlight);

                foreach (DirectoryInfo eachHeaderDirectoryInfo in headerDirectoryInfos)
                {
                    ProgramBase.ConsoleWrite($"{eachHeaderDirectoryInfo.Name} ", ConsoleWriteStyle.Default, 0);
                    foreach (FileInfo fileInfo in eachHeaderDirectoryInfo.GetFiles("*.md").ToList())
                    {
                        Editor topicEditor = new Editor(fileInfo);
                        string firstAsteriskInYamlDescriptionLine = topicEditor.GetFirstAsteriskInYamlDescriptionLine();
                        if (firstAsteriskInYamlDescriptionLine != null)
                        {
                            ProgramBase.ConsoleWrite($"{Environment.NewLine}{fileInfo.Name} {firstAsteriskInYamlDescriptionLine}", ConsoleWriteStyle.Error, 2);
                        }
                    }
                }
                ProgramBase.ConsoleWrite(string.Empty, ConsoleWriteStyle.Default, 2);
            }
        }

        private static List<DirectoryInfo> GetHeaderDirectoryInfos()
        {
            List<DirectoryInfo> headerDirectoryInfos = new List<DirectoryInfo>();
            var win32ApiReferenceContentFolder = new DirectoryInfo(Directory.GetCurrentDirectory());
            foreach (var headerDirectoryInfo in win32ApiReferenceContentFolder.GetDirectories())
            {
                string headerDirectoryName = headerDirectoryInfo.Name;
                if (!headerDirectoryName.StartsWith("_"))
                {
                    headerDirectoryInfos.Add(headerDirectoryInfo);
                }
            }
            return headerDirectoryInfos;
        }

        private static void GetApiRefModelWin32ProcessHeader(ApiRefModelWin32 apiRefModelWin32, DirectoryInfo eachHeaderDirectoryInfo)
        {
            // Construct a WindowsSDKWin32HeaderFilesLexer to use for the duration of this method.
            var windowsSDKWin32HeaderFileLexer = WindowsSDKWin32HeaderFileLexer.MakeWindowsSDKWin32HeaderFileLexer(eachHeaderDirectoryInfo);

            // Free functions.

            // Structures.

            // TODO: in the built topic object model, for each header, have a dictionary that maps UID to correct name (the built topic's H1 should
            // be the real name.
            // Then for each content file (which shouldn't even have an H1), map its UID to that name. Then use that in the regex to find the right struct.
            // In the Type field, put the SAL and also any "[size_is(cEntries)]" type stuff.

            List<FileInfo> fileInfos = eachHeaderDirectoryInfo.GetFiles("ns-*.md").ToList();
            foreach (FileInfo fileInfo in fileInfos)
            {
                Editor structureTopicEditor = new Editor(fileInfo);
                int lineNumberOneBased = structureTopicEditor.GetFieldsSection1BasedLineNumber();

                (string structure_members, string structure_names) = windowsSDKWin32HeaderFileLexer.FindStructureDefinition(structureTopicEditor.GetYamlApiName());

                string[] structure_members_array = structure_members.Split(';');
                for (int ix = 0; ix < structure_members_array.Length; ++ix)
                {
                    structure_members_array[ix] = structure_members_array[ix].Trim();

                    string[] member_elements_array = structure_members_array[ix].Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
                    for (int ix2 = 0; ix2 < member_elements_array.Length; ++ix2)
                    {
                        member_elements_array[ix2] = member_elements_array[ix2].Trim();

                        string[] member_element_elements_array = member_elements_array[ix2].Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                        if (member_element_elements_array.Length > 0)
                        {
                            if (member_element_elements_array[0] != member_elements_array[ix2])
                            {

                            }
                        }
                    }
                }
                string[] structure_names_array = structure_names.Split(',');
                for (int ix = 0; ix < structure_names_array.Length; ++ix)
                {
                    structure_names_array[ix] = structure_names_array[ix].Trim();
                }
            }
        }

        public static void ReportAnyNoBreakSpaceInWin32ConceptualTopics()
        {
            using (new ChangeAndRestoreCurrentDirectory($"{ProgramBase.MyContentReposFolderDirectoryInfo.FullName}\\{ProgramBase.Win32ConceptualContentRepoName}\\desktop-src"))
            {
                var win32ConceptualContentFolder = new DirectoryInfo(Directory.GetCurrentDirectory());

                foreach (var eachDirectoryInfo in win32ConceptualContentFolder.GetDirectories())
                {
                    Win32APIReferenceContentTopicsLexer.ReportAnyNoBreakSpaceInFolderRecursive(string.Empty, eachDirectoryInfo);
                }

                ProgramBase.ConsoleWrite(string.Empty, ConsoleWriteStyle.Default, 2);
            }
        }

        private static void ReportAnyNoBreakSpaceInFolderRecursive(string parentFolder, DirectoryInfo eachDirectoryInfo)
        {
            if (eachDirectoryInfo.Name.ToLower() == "images") return;

            ProgramBase.ConsoleWrite($"{parentFolder}{eachDirectoryInfo.Name} ", ConsoleWriteStyle.Default, 0);

            foreach (FileInfo fileInfo in eachDirectoryInfo.GetFiles("*.md").ToList())
            {
                Editor topicEditor = new Editor(fileInfo);
                string firstNoBreakSpaceLine = topicEditor.GetFirstNoBreakSpaceLine();
                if (firstNoBreakSpaceLine != null)
                {
                    ProgramBase.ConsoleWrite($"{Environment.NewLine}{fileInfo.Name} {firstNoBreakSpaceLine}", ConsoleWriteStyle.Error, 2);
                }
            }

            foreach (var eachSubdirectoryInfo in eachDirectoryInfo.GetDirectories())
            {
                Win32APIReferenceContentTopicsLexer.ReportAnyNoBreakSpaceInFolderRecursive($"{parentFolder}{eachDirectoryInfo.Name}\\", eachSubdirectoryInfo);
            }
        }
    }
}