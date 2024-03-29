﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Authentication;
using MDSDK;
using MDSDKDerived;

namespace MDSDKBase
{
    /// <summary>
    /// Abstract base class for the program. Provides services such as reading config,
    /// outputting messages, managing log files, returning docsets from main,
    /// and returning stubs as a list of either FileInfo or Editor, and reading
    /// and validating mapping files (1:1, 1:N, or N:1). The SDK skeleton project extends ProgramBase
    /// with a class called Program. Program is the class you add app-specific facilities to. Augment
    /// ProgramBase only with common facilities.
    /// </summary>
    internal abstract class ProgramBase
    {
        /// <summary>
        /// XMetaL can only render a topic in Page Preview if it's below a certain size. This
        /// value represents the upper limit.
        /// </summary>
        public const Int32 MaximumNumberOfTableRowsXMetaLCanHandleInATopic = 1800;
        /// <summary>
        /// The folder on your local machine containing your cloned content repos. This is read from configuration.txt.
        /// </summary>
        public static DirectoryInfo? MyContentReposFolderDirectoryInfo;
        /// <summary>
        /// The folder where the api ref topic stubs live on the network. This is read from configuration.txt.
        /// </summary>
        public static DirectoryInfo? ApiRefStubDirectoryInfo = null;
        /// <summary>
        /// A string containing the path of the folder from where this exe was loaded.
        /// </summary>
        public static string? ExeFolderPath;
        /// <summary>
        /// A string containing the name of the content repo to target. This is read from configuration.txt.
        /// </summary>
        public static string? ContentRepoName;
        /// <summary>
        /// A string containing the name of the Win32 and COM conceptual repo. This is read from configuration.txt.
        /// </summary>
        public static string? Win32ConceptualContentRepoName;
        /// <summary>
        /// A string containing the name of the Win32 and COM API ref repo. This is read from configuration.txt.
        /// </summary>
        public static string? Win32ApiReferenceContentRepoName;
        /// <summary>
        /// A string containing the name of the WinRT conceptual repo. This is read from configuration.txt.
        /// </summary>
        public static string? WinRTConceptualContentRepoName;
        /// <summary>
        /// A string containing the name of the WinRT API ref repo. This is read from configuration.txt.
        /// </summary>
        public static string? WinRTApiReferenceContentRepoName;
        /// <summary>
        /// A string containing the name of the WinRT-related repo. This is read from configuration.txt.
        /// </summary>
        public static string? WinRTRelatedContentRepoName;
        /// <summary>
        /// A string containing the name of the Win32 and COM API ref build repo. This is read from configuration.txt.
        /// </summary>
        public static string? Win32ApiReferenceBuildRepoName;
        /// <summary>
        /// A string containing the name of the folder containing the Windows SDK Win32 header files. This is read from configuration.txt.
        /// </summary>
        public static string? WindowsSDKWin32HeaderFilesFolderName;
        /// <summary>
        /// A string containing the name of the branch to base the personal branch on. This is read from configuration.txt.
        /// </summary>
        public static string? BaseBranchName;
        /// <summary>
        /// A string containing your alias. This is read from configuration.txt.
        /// </summary>
        public static string? MyAlias;
        /// <summary>
        /// A string containing the name of the personal branch to create. This is read from configuration.txt.
        /// </summary>
        public static string? PersonalBranchName;
        /// <summary>
        /// A string containing the Git commit message for this run. This is read from configuration.txt.
        /// </summary>
        public static string? CommitMessage;
        /// <summary>
        /// True if this is a live run, otherwise false. A live run creates a branch, and edits/saves files; a dry run does none of those things. This is read from configuration.txt.
        /// </summary>
        public static bool LiveRun = false;
        /// <summary>
        /// True if an xtoc's topicURL points to a missing file, otherwise false. This is read from configuration.txt.
        /// </summary>
        //public static bool ThrowExceptionOnBadXTocTopicURL = true;

        // The trailing spaces are important so that they don't get included in the value that follows.
        // Although, I really ought to change that to ignore whitespace between key and value, because
        // some folks are whitespace-blind, and will put any number of spaces in any number of places.
        private const string MY_CONTENT_REPOS_FOLDER_CONFIG_KEY = "my_content_repos_folder ";
        private const string WIN32_CONCEPTUAL_CONTENT_REPO_NAME_CONFIG_KEY = "win32_conceptual_content_repo_name ";
        private const string WIN32_API_REFERENCE_CONTENT_REPO_NAME_CONFIG_KEY = "win32_api_reference_content_repo_name ";
        private const string WINRT_CONCEPTUAL_CONTENT_REPO_NAME_CONFIG_KEY = "winrt_conceptual_content_repo_name ";
        private const string WINRT_API_REFERENCE_CONTENT_REPO_NAME_CONFIG_KEY = "winrt_api_reference_content_repo_name ";
        private const string WINRT_RELATED_CONTENT_REPO_NAME_CONFIG_KEY = "winrt_related_content_repo_name ";
        private const string WIN32_API_REFERENCE_BUILD_REPO_NAME_CONFIG_KEY = "win32_api_reference_build_repo_name ";
        private const string WINDOWS_SDK_WIN32_HEADER_FILES_FOLDER_CONFIG_KEY = "windows_sdk_win32_header_files_folder ";
        private const string LIVE_RUN_CONFIG_KEY = "live_run ";
        private const string CONTENT_REPO_NAME_CONFIG_KEY = "content_repo_name ";
        private const string BASE_BRANCH_NAME_CONFIG_KEY = "base_branch_name ";
        private const string MY_ALIAS_CONFIG_KEY = "my_alias ";
        private const string PERSONAL_BRANCH_NAME_CONFIG_KEY = "personal_branch_name ";
        private const string COMMIT_MESSAGE_CONFIG_KEY = "commit_message ";
        //private const string THROWEXCEPTIONONBADXTOCTOPICURL_CONFIG_KEY = "throwexceptiononbadxtoctopicurl ";
        private const string UWP_PROJ_CONFIG_KEY = "uwp_proj ";
        private const string UWP_EXCLUDE_TYPE_CONFIG_KEY = "uwp_exclude_type ";
        private const string WINRT_PROJ_CONFIG_KEY = "winrt_proj ";
        private const string REF_PROJ_PREFIX_CONFIG_KEY = "ref_proj_prefix ";

        /// <summary>
        /// The list of projects, or search patterns, that document UWP (Windows 10) features and namespaces. This is read from configuration.txt.
        /// </summary>
        public static List<string> UWPProjects = new List<string>();
        /// <summary>
        /// A list of types that are present in UWP projects, but which are not themselves UWP types. This is read from configuration.txt.
        /// </summary>
        public static List<string> UWPExcludedTypes = new List<string>();
        /// <summary>
        /// A list of projects, or search patterns, that document WinRT (Windows 8.x, Windows Phone 8.x, and Windows 10) features and namespaces. This is read from configuration.txt.
        /// </summary>
        public static List<string> WinRTProjects = new List<string>();
        /// <summary>
        /// A list of project prefixes that contain reference. These are prefixes, not search patterns.
        /// </summary>
        public static List<string> ReferenceProjectPrefixes = new List<string>();
        /// <summary>
        /// A list of keys found in unique key maps that are not unique. This list is recorded
        /// while loading maps, and is reported to a log at the end of the run.
        /// </summary>
        private static List<string> DuplicateMapKeys = new List<string>();

        // Logs
        private bool didWeWriteAnyOtherLogs = false;
        private List<Log> Logs = new List<Log>();
        /// <summary>
        /// Use this method to register your logs. ProgramBase will output and announce any
        /// non-empty logs at the end of the run.
        /// </summary>
        /// <param name="log">The log to register.</param>
        public void RegisterLog(Log log) { this.Logs.Add(log); }
        public static Log? FilesSavedLog;
        public static Log? FileSaveErrorsLog;
        private static Log? NonexistentRidsInMappingsLog;
        private static Log? DuplicatedMappingsLog;
        private static Log? MalformedMappingsLog;
        public static Log? DupedWin32ApiNamesLog;

        /// <summary>
        /// Construct a new <see cref="Program"/> and call <see cref="ProgramBase.Run"/> on it from your Main method.
        /// </summary>
        /// <returns>0 to indicate success; 1 to indicate failure.</returns>
        protected int Run()
        {
            try
            {
                ProgramBase.ReadConfigurationFile();

                ProgramBase.FilesSavedLog = new Log() { Label = "Files saved.", Filename = "FilesSaved_Log.txt" };
                ProgramBase.FileSaveErrorsLog = new Log() { Label = "File save errors.", Filename = "FileSaveErrors_Log.txt", AnnouncementStyle = ConsoleWriteStyle.Error };
                ProgramBase.NonexistentRidsInMappingsLog = new Log() { Label = "Non-existent topic rids found in mapping file(s).", Filename = "NonexistentRidInMappingFile_Log.txt", AnnouncementStyle = ConsoleWriteStyle.Error };
                ProgramBase.MalformedMappingsLog = new Log() { Label = "Malformed mappings (should be two comma-separated values).", Filename = "MalformedMappings_Log.txt", AnnouncementStyle = ConsoleWriteStyle.Error };
                ProgramBase.DuplicatedMappingsLog = new Log() { Label = "Duplicated mappings.", Filename = "DuplicatedMappings_Log.txt", AnnouncementStyle = ConsoleWriteStyle.Error };
                ProgramBase.DupedWin32ApiNamesLog = new Log() { Label = "Duped Win32 API names.", Filename = "DupedWin32ApiNames_Log.txt", AnnouncementStyle = ConsoleWriteStyle.Error };
                this.RegisterLog(ProgramBase.NonexistentRidsInMappingsLog);
                this.RegisterLog(ProgramBase.MalformedMappingsLog);
                this.RegisterLog(ProgramBase.DuplicatedMappingsLog);
                this.RegisterLog(ProgramBase.DupedWin32ApiNamesLog);

                this.OnRun();

                ProgramBase.SetCurrentDirectory(ProgramBase.ExeFolderPath);

                this.OutputFilesSavedLog();
                this.OutputOtherLogs();
            }
            catch (MDSDKException)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// When you call <see cref="ProgramBase.Run"/>, <see cref="ProgramBase"/> will call your override
        /// of <see cref="ProgramBase.OnRun"/>. That's where you'll do your work.
        /// </summary>
        protected abstract void OnRun();

        public static void SetCurrentDirectory(DirectoryInfo directoryInfo)
        {
            Directory.SetCurrentDirectory(directoryInfo.FullName!);
            ProgramBase.ConsoleWrite($"Current directory set to {directoryInfo.FullName}", ConsoleWriteStyle.Success);
        }

        public static void SetCurrentDirectory(string? directoryFullName)
        {
            Directory.SetCurrentDirectory(directoryFullName!);
            ProgramBase.ConsoleWrite($"Current directory set to {directoryFullName}", ConsoleWriteStyle.Success);
        }

        private static void ReadConfigurationFile()
        {
            ProgramBase.ExeFolderPath = Directory.GetCurrentDirectory();

            FileInfo fileInfo = new FileInfo("configuration.txt");
            if (!fileInfo.Exists)
            {
                ProgramBase.ConsoleWrite("MISSING CONFIGURATION FILE. You need a file named configuration.txt in the Output Directory.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                string? currentLine;

                while (null != (currentLine = streamReader.ReadLine()))
                {
                    currentLine = currentLine.Trim();
                    if (!currentLine.StartsWith("//") && currentLine.Length > 0)
                    {
                        string? value = null;

                        if (ProgramBase.GetConfigValue(currentLine, ProgramBase.MY_CONTENT_REPOS_FOLDER_CONFIG_KEY, ref value))
                        {
                            string? expandedMyContentReposFolderPath = Environment.ExpandEnvironmentVariables(value!);
                            ProgramBase.MyContentReposFolderDirectoryInfo = new DirectoryInfo(expandedMyContentReposFolderPath);
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WIN32_CONCEPTUAL_CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.Win32ConceptualContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WIN32_API_REFERENCE_CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.Win32ApiReferenceContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WINRT_CONCEPTUAL_CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.WinRTConceptualContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WINRT_API_REFERENCE_CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.WinRTApiReferenceContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WINRT_RELATED_CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.WinRTRelatedContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WIN32_API_REFERENCE_BUILD_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.Win32ApiReferenceBuildRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WINDOWS_SDK_WIN32_HEADER_FILES_FOLDER_CONFIG_KEY, ref value))
                        {
                            ProgramBase.WindowsSDKWin32HeaderFilesFolderName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.LIVE_RUN_CONFIG_KEY, ref value))
                        {
                            ProgramBase.LiveRun = (value == "1");
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.CONTENT_REPO_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.ContentRepoName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.BASE_BRANCH_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.BaseBranchName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.MY_ALIAS_CONFIG_KEY, ref value))
                        {
                            ProgramBase.MyAlias = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.PERSONAL_BRANCH_NAME_CONFIG_KEY, ref value))
                        {
                            ProgramBase.PersonalBranchName = value;
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.COMMIT_MESSAGE_CONFIG_KEY, ref value))
                        {
                            ProgramBase.CommitMessage = value;
                        }
                        //else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.THROWEXCEPTIONONBADXTOCTOPICURL_CONFIG_KEY, ref value))
                        //{
                        //	ProgramBase.ThrowExceptionOnBadXTocTopicURL = (value == "1");
                        //}
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.UWP_PROJ_CONFIG_KEY, ref value))
                        {
                            ProgramBase.UWPProjects.Add(value!);
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.UWP_EXCLUDE_TYPE_CONFIG_KEY, ref value))
                        {
                            if (!ProgramBase.UWPExcludedTypes.Contains(value!))
                            {
                                ProgramBase.UWPExcludedTypes.Add(value!);
                            }
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.WINRT_PROJ_CONFIG_KEY, ref value))
                        {
                            if (!ProgramBase.WinRTProjects.Contains(value!))
                            {
                                ProgramBase.WinRTProjects.Add(value!);
                            }
                        }
                        else if (ProgramBase.GetConfigValue(currentLine, ProgramBase.REF_PROJ_PREFIX_CONFIG_KEY, ref value))
                        {
                            if (!ProgramBase.ReferenceProjectPrefixes.Contains(value!))
                            {
                                ProgramBase.ReferenceProjectPrefixes.Add(value!);
                            }
                        }
                    }
                }
            }

            if (ProgramBase.MyContentReposFolderDirectoryInfo is null)
            {
                ProgramBase.ConsoleWrite("MISSING CONTENT REPOS FOLDER CONFIG INFO. Your configuration.txt needs to contain something like: my_content_repos_folder %USERPROFILE%\\source\\repos. This is the folder on your local machine containing your cloned content repos.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            ProgramBase.SetCurrentDirectory(ProgramBase.MyContentReposFolderDirectoryInfo);
        }

        private static bool GetConfigValue(string currentLine, string key, ref string? value)
        {
            if (currentLine.StartsWith(key))
            {
                value = currentLine.Substring(key.Length).Trim();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loads each line of a text file into a list of strings.
        /// </summary>
        /// <param name="fileName">The name of the file to load from your Output Folder. Add the file to your project as Content/Copy if newer.</param>
        /// <param name="listToAddTo">Optional list of strings to add to.</param>
        /// <param name="notFoundMessage">Optional message to override the default file-not-found message.</param>
        public static void LoadTextFileIntoStringList(string fileName, ref List<string> listToAddTo, string? notFoundMessage = null)
        {
            if (listToAddTo is null) listToAddTo = new List<string>();

            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                ProgramBase.ConsoleWrite(notFoundMessage ?? "Could not find " + fileName, ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                while (!streamReader.EndOfStream)
                {
                    listToAddTo.Add(streamReader.ReadLine()!);
                }
            }
        }

        /// <summary>
        /// Load a named file that contains associations from a key to a mapped value, where keys
        /// are unique (that is, a key is valid only if it appears exactly once in the file). This method
        /// reports duplicate keys to a log. Mapped values need not be unique.
        /// </summary>
        /// <param name="fileName">The name of the file to load from your Output Folder. Add the file to your project as Content/Copy if newer.</param>
        /// <param name="containsTopicRids">Key and values are rids, so validate them.</param>
        /// <param name="docSetsToValidateAgainst">Docset against which to valid keys and mapped values, respectively.</param>
        /// <returns>A dictionary representing the map.</returns>
        protected Dictionary<string, string> LoadUniqueKeyMap(string fileName, bool containsTopicRids = false, params DocSet[] docSetsToValidateAgainst)
        {
            if (containsTopicRids && docSetsToValidateAgainst.Length != 2)
            {
                ProgramBase.ConsoleWrite("Rids in " + fileName + " could not be validated: you need to pass two docsets to validate keys and mapped values against, respectively.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            Dictionary<string, string> mappingsDict = new Dictionary<string, string>();

            ProgramBase.SetCurrentDirectory(ProgramBase.ExeFolderPath);

            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                ProgramBase.ConsoleWrite("Could not find " + fileName, ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                string? currentLine = null;

                while (null != (currentLine = streamReader.ReadLine()))
                {
                    if (!currentLine.StartsWith("//") && currentLine.Length > 0)
                    {
                        string[] values = currentLine.Split(',');

                        for (int ix = 0; ix < values.Length; ++ix)
                        {
                            values[ix] = values[ix].Trim();
                        }

                        if (values.Length == 2 && values[0].Length > 0 && values[1].Length > 0)
                        {
                            // If this is the third+ time we've seen this key...
                            if (Program.DuplicateMapKeys.Contains(values[0]))
                            {
                                Program.DuplicatedMappingsLog!.Add(fileName + " has duplicate key: " + currentLine);
                                continue;
                            }
                            // If this is the second time we've seen this key...
                            if (mappingsDict.ContainsKey(values[0]))
                            {
                                Program.DuplicatedMappingsLog!.Add(fileName + " has duplicate key: " + values[0] + "," + mappingsDict[values[0]]);
                                Program.DuplicatedMappingsLog!.Add(fileName + " has duplicate key: " + currentLine);
                                mappingsDict.Remove(values[0]);
                                Program.DuplicateMapKeys.Add(values[0]);
                                continue;
                            }
                            if (containsTopicRids)
                            {
                                if (docSetsToValidateAgainst[0].GetFileInfoForTopic(values[0]) is null)
                                {
                                    ProgramBase.NonexistentRidsInMappingsLog!.Add(docSetsToValidateAgainst[0].Description + " don't contain " + values[0]);
                                }
                                if (docSetsToValidateAgainst[1].GetFileInfoForTopic(values[1]) is null)
                                {
                                    ProgramBase.NonexistentRidsInMappingsLog!.Add(docSetsToValidateAgainst[1].Description + " don't contain " + values[1]);
                                }
                            }
                            mappingsDict[values[0]] = values[1];
                        }
                        else
                        {
                            ProgramBase.MalformedMappingsLog!.Add(fileName + " has malformed mapping: " + currentLine);
                        }
                    }
                }
            }

            ProgramBase.SetCurrentDirectory(ProgramBase.MyContentReposFolderDirectoryInfo!);

            return mappingsDict;
        }

        /// <summary>
        /// Load a named file that contains associations from a key to a mapped value, where keys
        /// are not unique (that is, a key may appear any number of times in the file). Mapped values
        /// need not be unique.
        /// </summary>
        /// <param name="fileName">The name of the file to load from your Output Folder. Add the file to your project as Content/Copy if newer.</param>
        /// <returns>A dictionary representing the map.</returns>
        protected Dictionary<string, List<string>> LoadNonUniqueKeyMap(string fileName, char delimiter = ',')
        {
            Dictionary<string, List<string>> mappingsDict = new Dictionary<string, List<string>>();

            ProgramBase.SetCurrentDirectory(ProgramBase.ExeFolderPath);

            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                ProgramBase.ConsoleWrite("Could not find " + fileName, ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                string? currentLine = null;

                while (null != (currentLine = streamReader.ReadLine()))
                {
                    if (!currentLine.StartsWith("//") && currentLine.Length > 0)
                    {
                        string[] values = currentLine.Split(delimiter);

                        for (int ix = 0; ix < values.Length; ++ix)
                        {
                            values[ix] = values[ix].Trim();
                        }

                        if (values.Length == 2 && values[0].Length > 0 && values[1].Length > 0)
                        {
                            if (!mappingsDict.ContainsKey(values[0]))
                            {
                                mappingsDict[values[0]] = new List<string>();
                            }
                            if (!mappingsDict[values[0]].Contains(values[1].ToLower()))
                            {
                                mappingsDict[values[0]].Add(values[1].ToLower());
                            }
                        }
                        else
                        {
                            ProgramBase.MalformedMappingsLog!.Add(fileName + " has malformed mapping: " + currentLine);
                        }
                    }
                }
            }

            ProgramBase.SetCurrentDirectory(ProgramBase.MyContentReposFolderDirectoryInfo!);

            return mappingsDict;
        }

        /// <summary>
        /// Gets a FileInfo for each API ref topic stub found in project folders that match the search pattern.
        /// </summary>
        /// <param name="searchPattern">A project folder search pattern, for example *, w_*, or w_appmod*.</param>
        /// <returns>A list of FileInfo.</returns>
        public List<FileInfo> GetFileInfosForTopicStubsForFolderSearchPattern(string searchPattern = "*")
        {
            List<FileInfo> fileInfos = new List<FileInfo>();

            foreach (DirectoryInfo eachDirectoryInfo in ProgramBase.ApiRefStubDirectoryInfo!.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly).ToList())
            {
                if (Directory.Exists(Path.Combine(eachDirectoryInfo.FullName, eachDirectoryInfo.Name)))
                {
                    DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(eachDirectoryInfo.FullName, eachDirectoryInfo.Name));
                    fileInfos.AddRange(targetDir.GetFiles("*.xml").ToList());
                }
                else
                {
                    ProgramBase.ConsoleWrite(eachDirectoryInfo.Name + " doesn't have a same-named subfolder.", ConsoleWriteStyle.Error);
                }
            }
            return fileInfos;
        }

        /// <summary>
        /// Gets an Editor for each API ref topic stub found in project folders that match the search pattern.
        /// </summary>
        /// <param name="searchPattern">A project folder search pattern, for example *, w_*, or w_appmod*.</param>
        /// <returns>A list of Editor.</returns>
        public List<Editor> GetEditorsForTopicStubsForFolderSearchPattern(string searchPattern = "*")
        {
            List<FileInfo> fileInfos = this.GetFileInfosForTopicStubsForFolderSearchPattern(searchPattern);

            List<Editor> editors = new List<Editor>();
            foreach (FileInfo eachFileInfo in fileInfos)
            {
                editors.Add(new Editor(eachFileInfo));
            }
            return editors;
        }

        /// <summary>
        /// Gets a FileInfo for the API ref topic stub that matches the id.
        /// </summary>
        /// <param name="id">The topic id to search for.</param>
        /// <returns>A FileInfo.</returns>
        public FileInfo? GetFileInfoForTopicStub(string id)
        {
            string[] segments = id.Split('.');
            if (segments is null || segments.Length != 2) return null;

            string project = segments[0];
            List<DirectoryInfo> directoryInfos = ProgramBase.ApiRefStubDirectoryInfo!.GetDirectories(project, SearchOption.TopDirectoryOnly).ToList();
            if (directoryInfos.Count != 1) return null;
            DirectoryInfo directoryInfo = directoryInfos[0];

            if (Directory.Exists(Path.Combine(directoryInfo.FullName, directoryInfo.Name)))
            {
                DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(directoryInfo.FullName, directoryInfo.Name));
                List<FileInfo> fileInfo = targetDir.GetFiles(segments[1].Replace('.', '_') + "*.xml").ToList();
                if (fileInfo.Count != 1) return null;

                return fileInfo[0];
            }
            else
            {
                ProgramBase.ConsoleWrite(directoryInfo.Name + " doesn't have a same-named subfolder.", ConsoleWriteStyle.Error);
            }
            return null;
        }

        /// <summary>
        /// Gets a Editor for the API ref topic stub that matches the id.
        /// </summary>
        /// <param name="id">The topic id to search for.</param>
        /// <returns>An Editor.</returns>
        public Editor? GetEditorForTopicStub(string id)
        {
            FileInfo? fileInfo = this.GetFileInfoForTopicStub(id);
            if (fileInfo is not null)
            {
                return new Editor(fileInfo);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Writes a color-coded message to the console.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="style">An optional color style. The default is light gray.</param>
        /// <param name="newLinesAfter">Optional, defaults to 1. The number of newlines to print after the message.</param>
        public static void ConsoleWrite(string message, ConsoleWriteStyle style = ConsoleWriteStyle.Default, int newLinesAfter = 1)
        {
            ConsoleColor previousColor = Console.ForegroundColor;
            if (style == ConsoleWriteStyle.Highlight) Console.ForegroundColor = ConsoleColor.White;
            else if (style == ConsoleWriteStyle.Success) Console.ForegroundColor = ConsoleColor.Green;
            else if (style == ConsoleWriteStyle.Warning) Console.ForegroundColor = ConsoleColor.Yellow;
            else if (style == ConsoleWriteStyle.Error) Console.ForegroundColor = ConsoleColor.Red;
            else if (style == ConsoleWriteStyle.OutputFromAnotherProcess) Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(message);
            while (newLinesAfter-- > 0) Console.Write(Environment.NewLine);
            Console.ForegroundColor = previousColor;
        }

        public const int NumberOfCharsToIndentIncrement = 2;
        public static char IndentationCharForConsole = ' ';
        public static string RenderIndent(int numberOfCharsToIndent)
        {
            string indentationString = string.Empty;
            for (int ix = 0; ix < numberOfCharsToIndent; ix++) indentationString += ProgramBase.IndentationCharForConsole;
            return indentationString;
        }

        public static void ConsoleWriteIndent(int numberOfCharsToIndent)
        {
            ProgramBase.ConsoleWrite(ProgramBase.RenderIndent(numberOfCharsToIndent), ConsoleWriteStyle.Default, 0);
        }

        private void OutputFilesSavedLog()
        {
            ProgramBase.DeleteFileIfExists(ProgramBase.FilesSavedLog!.Filename);

            if (ProgramBase.LiveRun)
            {
                ProgramBase.ConsoleWrite("========FILES SAVED (LIVE RUN)========", ConsoleWriteStyle.Highlight);
            }
            else
            {
                ProgramBase.ConsoleWrite("=======FILES NOT SAVED (DRY RUN)======", ConsoleWriteStyle.Highlight);
            }

            if (ProgramBase.FilesSavedLog.Count > 0)
            {
                ProgramBase.ConsoleWrite(ProgramBase.FilesSavedLog[0]);
            }
            else
            {
                if (ProgramBase.LiveRun)
                {
                    ProgramBase.ConsoleWrite("!!!No files saved. This was a live run, and it was a no-op!!!", ConsoleWriteStyle.Warning);
                }
                else
                {
                    ProgramBase.ConsoleWrite("***None***", ConsoleWriteStyle.Warning);
                }
            }

            if (ProgramBase.FilesSavedLog.Count > 1)
            {
                ProgramBase.ConsoleWrite("For the rest, see " + ProgramBase.FilesSavedLog.Filename);

                using (StreamWriter streamWriter = File.CreateText(ProgramBase.FilesSavedLog.Filename!))
                {
                    foreach (string eachLine in ProgramBase.FilesSavedLog)
                    {
                        streamWriter.WriteLine(eachLine);
                    }
                }
            }

            if (this.OutputOtherLog(ProgramBase.FileSaveErrorsLog!))
            {
                if (ProgramBase.LiveRun)
                {
                    ProgramBase.ConsoleWrite("!!!There are file save errors!!!", ConsoleWriteStyle.Warning);
                }
                else
                {
                    ProgramBase.ConsoleWrite("!!!Make files writable if you want to save them!!!", ConsoleWriteStyle.Warning);
                }
            }
        }

        private void OutputOtherLogs()
        {
            ProgramBase.ConsoleWrite($"{Environment.NewLine}===========LOGS===========", ConsoleWriteStyle.Highlight);

            this.didWeWriteAnyOtherLogs = false;

            foreach (Log eachLog in this.Logs)
            {
                this.OutputOtherLog(eachLog);
            }

            if (!this.didWeWriteAnyOtherLogs)
            {
                ProgramBase.ConsoleWrite("***No logs***", ConsoleWriteStyle.Warning);
            }
        }

        private bool OutputOtherLog(Log log)
        {
            bool didWeWriteAnyOtherLogsThisCall = false;

            ProgramBase.DeleteFileIfExists(log.Filename);

            if (log.Count > 0)
            {
                didWeWriteAnyOtherLogsThisCall = this.didWeWriteAnyOtherLogs = true;
                ProgramBase.ConsoleWrite("See " + log.Filename, log.AnnouncementStyle);

                using (StreamWriter streamWriter = File.CreateText(log.Filename!))
                {
                    if (log.Headers is not null)
                    {
                        streamWriter.WriteLine(string.Format(log.FormatString!, log.Headers));
                    }
                    else
                    {
                        streamWriter.WriteLine("// " + log.Label);
                    }
                    foreach (string eachLine in log)
                    {
                        streamWriter.WriteLine(eachLine);
                    }
                }
            }

            return didWeWriteAnyOtherLogsThisCall;
        }

        private static void DeleteFileIfExists(string? path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException ex)
            {
                ProgramBase.ConsoleWrite("I can't refresh the file " + path, ConsoleWriteStyle.Error);
                ProgramBase.ConsoleWrite(ex.Message, ConsoleWriteStyle.Error);
                ProgramBase.ConsoleWrite("Please close it if you have it open.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }
        }
    }

    /// <summary>
    /// Represents a 1-element stack used to push the current directory before
    /// changing it, and then pop it back afterward. The class implements
    /// IDisposable for syntax convenience and safety.
    /// </summary>
    internal class ChangeAndRestoreCurrentDirectory : IDisposable
    {
        private string? pushedCurrentDirectory = null;

        public ChangeAndRestoreCurrentDirectory(DirectoryInfo directoryInfo) : this(directoryInfo.FullName) { }

        public ChangeAndRestoreCurrentDirectory(string directoryFullName)
        {
            this.pushedCurrentDirectory = Directory.GetCurrentDirectory();
            ProgramBase.SetCurrentDirectory(directoryFullName);
        }

        public void Dispose()
        {
            ProgramBase.SetCurrentDirectory(this.pushedCurrentDirectory);
        }
    }

    /// <summary>
    /// Represents a docset, defined by a platform specification (WinRT vs UWP)
    /// and a content type specification (conceptual vs ref) axes. You can query
    /// for a flat list of FileInfo, a flat list of Editor, or a hierarchical
    /// object model of API reference.
    /// </summary>
    internal class DocSet
    {
        /// <summary>
        /// A list of DirectoryInfo objects each representing a project in the docset.
        /// You can access this directly if you need to.
        /// </summary>
        public List<DirectoryInfo> ProjectDirectoryInfos = new List<DirectoryInfo>();
        public DocSetType DocSetType;
        public Platform Platform;
        public string Description;
        private ApiRefModel? apiRefModel;

        /// <summary>
        /// Constructs and returns a DocSet to the given specification.
        /// </summary>
        /// <param name="docSetType">The content type (conceptual, ref, or both).</param>
        /// <param name="platform">The platform (UWP or WinRT).</param>
        /// <param name="description">A description to assign to the DocSet. This is used whenever the SDK needs to identify the DocSet in an error message.</param>
        /// <returns>A new DocSet object.</returns>
        public static DocSet CreateDocSet(DocSetType docSetType, Platform platform, string description)
        {
            ProgramBase.ConsoleWrite("Creating docset: \"" + description + "\"", ConsoleWriteStyle.Success);
            DocSet docSet = new DocSet(docSetType, platform, description);

            List<string>? metroAndWindevDotTxt = null;

            //ProgramBase.LoadTextFileIntoStringList("metro.txt", ref metroAndWindevDotTxt, "MISSING metro.txt. This file could not be found in your enlistment folder path. Your configuration.txt contains something like: my_content_repos_folder D:\\Source_Depot\\devdocmain. This should be the folder that contains the dev_*, m_*, w_* folders, BuildX, metro.txt, etc.");
            //ProgramBase.LoadTextFileIntoStringList("windev.txt", ref metroAndWindevDotTxt, "MISSING windev.txt. This file could not be found in your enlistment folder path. Your configuration.txt contains something like: my_content_repos_folder D:\\Source_Depot\\devdocmain. This should be the folder that contains the dev_*, m_*, w_* folders, BuildX, metro.txt, etc.");

            string docSetTypeDesc = "features and namespaces";
            if (docSet.DocSetType == DocSetType.ConceptualOnly) docSetTypeDesc = "features";
            if (docSet.DocSetType == DocSetType.ReferenceOnly) docSetTypeDesc = "namespaces";

            string projectListIntro = "These are the shipping projects that document UWP (Windows 10 only) " + docSetTypeDesc + " (they're in metro.txt or windev.txt). The app only processes topics in these projects that are represented by an unfiltered TOC entry (that is, no MSDN build condition).";
            if (docSet.Platform == Platform.WinRTWindows8xAnd10) projectListIntro = "These are the shipping projects that document WinRT (Windows 8.x, Windows Phone 8.x, and Windows 10) " + docSetTypeDesc + " (they're in metro.txt and windev.txt). The app only processes topics in these projects that are represented by an unfiltered TOC entry (that is, no MSDN build condition).";

            if (docSet.Platform == Platform.UWPWindows10)
            {
                foreach (string eachProjectName in ProgramBase.UWPProjects)
                {
                    docSet.ProjectDirectoryInfos.AddRange(ProgramBase.MyContentReposFolderDirectoryInfo!.GetDirectories(eachProjectName, SearchOption.TopDirectoryOnly).ToList());
                }
            }
            else
            {
                foreach (string eachProjectName in ProgramBase.WinRTProjects)
                {
                    docSet.ProjectDirectoryInfos.AddRange(ProgramBase.MyContentReposFolderDirectoryInfo!.GetDirectories(eachProjectName, SearchOption.TopDirectoryOnly).ToList());
                }
            }

            List<DirectoryInfo> directoryInfosToRemove = new List<DirectoryInfo>();
            foreach (DirectoryInfo eachDirectoryInfo in docSet.ProjectDirectoryInfos)
            {
                string prefix = eachDirectoryInfo.Name.Substring(0, eachDirectoryInfo.Name.IndexOf('_') + 1);
                if ((ProgramBase.ReferenceProjectPrefixes.Contains(prefix) && docSet.DocSetType == DocSetType.ConceptualOnly) || (!ProgramBase.ReferenceProjectPrefixes.Contains(prefix) && docSet.DocSetType == DocSetType.ReferenceOnly))
                {
                    directoryInfosToRemove.Add(eachDirectoryInfo);
                }
            }
            foreach (DirectoryInfo eachDirectoryInfoToRemove in directoryInfosToRemove)
            {
                docSet.ProjectDirectoryInfos.Remove(eachDirectoryInfoToRemove);
            }

            ProgramBase.ConsoleWrite(projectListIntro, ConsoleWriteStyle.Highlight);

            // Remove any project not listed in metro.txt nor windev.txt.
            List<DirectoryInfo> projectsToRemove = new List<DirectoryInfo>();
            foreach (DirectoryInfo eachDirectoryInfo in docSet.ProjectDirectoryInfos)
            {
                if (!metroAndWindevDotTxt!.Contains(eachDirectoryInfo.Name))
                {
                    projectsToRemove.Add(eachDirectoryInfo);
                }
                else
                {
                    ProgramBase.ConsoleWrite(eachDirectoryInfo.Name, ConsoleWriteStyle.Default, 0);
                    if (eachDirectoryInfo != docSet.ProjectDirectoryInfos[docSet.ProjectDirectoryInfos.Count - 1])
                    {
                        ProgramBase.ConsoleWrite(", ", ConsoleWriteStyle.Default, 0);
                    }
                    else
                    {
                        ProgramBase.ConsoleWrite(".", ConsoleWriteStyle.Default, 2);
                    }
                }
            }
            foreach (DirectoryInfo eachProjectToRemove in projectsToRemove)
            {
                docSet.ProjectDirectoryInfos.Remove(eachProjectToRemove);
            }

            return docSet;
        }

        private DocSet(DocSetType docSetType, Platform platform, string description)
        {
            this.DocSetType = docSetType;
            this.Platform = platform;
            this.Description = description;
        }

        /// <summary>
        /// Gets a DirectoryInfo representing the project in the docset whose name exactly matches the specified name.
        /// </summary>
        /// <param name="projectName">The name of the project to find an exact match for.</param>
        /// <returns>A DirectoryInfo.</returns>
        public DirectoryInfo? FindForProjectName(string projectName)
        {
            return this.ProjectDirectoryInfos.Find(directoryInfo => directoryInfo.Name == projectName);
        }

        /// <summary>
        /// Gets a DirectoryInfo representing each project in the docset whose name starts with the specified
        /// prefix. Note, this is a prefix, not a search pattern.
        /// </summary>
        /// <param name="projectNamePrefix">A project name prefix to find matches for.</param>
        /// <returns>A list of DirectoryInfo.</returns>
        public List<DirectoryInfo> FindAllForProjectNamePrefix(string projectNamePrefix)
        {
            return this.ProjectDirectoryInfos.FindAll(directoryInfo => directoryInfo.Name.StartsWith(projectNamePrefix));
        }

        /// <summary>
        /// Gets a FileInfo for each topic found in the project whose name exactly matches the specified
        /// string (or in every project whose name starts with the specified string if you specify that the
        /// string is actually a prefix).
        /// </summary>
        /// <param name="projectName">A project name (or prefix, if specified) to search for.</param>
        /// <param name="projectNameIsAPrefix">True if the project name is actually a prefix, otherwise false. Default is false. Note, a prefix is not a search pattern.</param>
        /// <param name="getAllFilesInFolderIgnoringXtoc">True if you want to return all files in the project folder, false if you just want unfiltered files in the xtoc. Default is false.</param>
        /// <returns>A list of FileInfo.</returns>
        public List<FileInfo> GetFileInfosForTopicsInProject(string? projectName = null, bool projectNameIsAPrefix = false, bool getAllFilesInFolderIgnoringXtoc = false)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            foreach (DirectoryInfo eachProjectDirectoryInfo in this.ProjectDirectoryInfos)
            {
                if (projectName is null || eachProjectDirectoryInfo.Name == projectName || (projectNameIsAPrefix && eachProjectDirectoryInfo.Name.StartsWith(projectName)))
                {
                    if (getAllFilesInFolderIgnoringXtoc == false)
                    {
                        fileInfos.AddRange(EditorBase.GetFileInfosForTopicsInFolder(eachProjectDirectoryInfo));
                    }
                    else
                    {
                        if (Directory.Exists(Path.Combine(eachProjectDirectoryInfo.FullName, eachProjectDirectoryInfo.Name)))
                        {
                            DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(eachProjectDirectoryInfo.FullName, eachProjectDirectoryInfo.Name));
                            fileInfos.AddRange(targetDir.GetFiles("*.xml").ToList());
                        }
                        else if (Directory.Exists(Path.Combine(eachProjectDirectoryInfo.FullName, "nodepage")))
                        {
                            DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(eachProjectDirectoryInfo.FullName, "nodepage"));
                            fileInfos.AddRange(targetDir.GetFiles("*.xml").ToList());
                        }
                        else
                        {
                            ProgramBase.ConsoleWrite(eachProjectDirectoryInfo.Name + " doesn't have a same-named subfolder.", ConsoleWriteStyle.Error);
                        }
                    }
                }
            }
            return fileInfos;
        }

        /// <summary>
        /// Gets an Editor for each topic found in the project whose name exactly matches the specified
        /// string (or in every project whose name starts with the specified string if you specify that the
        /// string is actually a prefix). You can also pass null to get every project.
        /// </summary>
        /// <param name="projectName">A project name (or prefix, if specified) to search for, or null for all projects.</param>
        /// <param name="projectNameIsAPrefix">True if the project name is actually a prefix, otherwise false. Note, a prefix is not a search pattern.</param>
        /// <param name="getAllFilesInFolderIgnoringXtoc">True if you want to return all files in the project folder, false if you just want unfiltered files in the xtoc. Defaults to false.</param>
        /// <returns>A list of Editor.</returns>
        public List<Editor> GetEditorsForTopicsInProject(string? projectName = null, bool projectNameIsAPrefix = false, bool getAllFilesInFolderIgnoringXtoc = false)
        {
            List<FileInfo> fileInfos = this.GetFileInfosForTopicsInProject(projectName, projectNameIsAPrefix, getAllFilesInFolderIgnoringXtoc);

            List<Editor> editors = new List<Editor>();
            foreach (FileInfo eachFileInfo in fileInfos)
            {
                editors.Add(new Editor(eachFileInfo));
            }
            return editors;
        }

        /// <summary>
        /// Gets a FileInfo representing the topic with the specified id.
        /// </summary>
        /// <param name="id">The id of the topic to get.</param>
        /// <returns>A FileInfo.</returns>
        public FileInfo? GetFileInfoForTopic(string id)
        {
            string[] segments = id.Split('.');
            if (segments is null || segments.Length != 2)
            {
                return null;
            }

            DirectoryInfo? directoryInfo = this.FindForProjectName(segments[0]);
            if (directoryInfo is null)
            {
                directoryInfo = this.FindForProjectName("modern_nodes");
            }
            if (directoryInfo is null) return null;
            if (Directory.Exists(Path.Combine(directoryInfo.FullName, directoryInfo.Name)))
            {
                DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(directoryInfo.FullName, directoryInfo.Name));
                List<FileInfo> fileInfo = targetDir.GetFiles(segments[1].Replace('.', '_') + ".xml").ToList();
                if (fileInfo.Count != 1) return null;

                return fileInfo[0];
            }
            else if (Directory.Exists(Path.Combine(directoryInfo.FullName, "nodepage")))
            {
                DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "nodepage"));
                List<FileInfo> fileInfo = targetDir.GetFiles(segments[1].Replace('.', '_') + ".xml").ToList();
                if (fileInfo.Count != 1) return null;

                return fileInfo[0];
            }
            else
            {
                ProgramBase.ConsoleWrite(directoryInfo.Name + " doesn't have a same-named subfolder.", ConsoleWriteStyle.Error);
            }
            return null;
        }

        /// <summary>
        /// Gets an Editor representing the topic with the specified id.
        /// </summary>
        /// <param name="id">The id of the topic to get.</param>
        /// <returns>An Editor.</returns>
        public Editor? GetEditorForTopic(string id)
        {
            FileInfo? fileInfo = this.GetFileInfoForTopic(id);
            if (fileInfo is not null)
            {
                return new Editor(fileInfo);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a hierarchical object model representing the set of API ref docs in this docset.
        /// </summary>
        public ApiRefModel ApiRefModel
        {
            get
            {
                if (this.apiRefModel is null)
                {
                    this.apiRefModel = new ApiRefModel();

                    foreach (DirectoryInfo eachProjectDirectoryInfo in this.ProjectDirectoryInfos)
                    {
                        // Only process reference projects.
                        string prefix = eachProjectDirectoryInfo.Name.Substring(0, eachProjectDirectoryInfo.Name.IndexOf('_') + 1);
                        if (ProgramBase.ReferenceProjectPrefixes.Contains(prefix))
                        {
                            // For reference projects, the topic files should be in a folder with the same name as the project folder. But use the standard algorithm just to be sure.
                            NamespaceWinRT.ProcessNamespaceWinRT(eachProjectDirectoryInfo.Name, EditorBase.GetEditorsForTopicsInFolder(eachProjectDirectoryInfo), ref this.apiRefModel);
                        }
                    }

                    // Now for some housekeeping. Remove empty namespace/types, and count up the APIs in each API contract.

                    // If a namespace has no name, or no classes, then remove it.
                    List<NamespaceWinRT> namespaceWinRTsToRemove = new List<NamespaceWinRT>();

                    foreach (NamespaceWinRT eachNamespaceWinRT in this.apiRefModel.NamespaceWinRTs)
                    {
                        if (eachNamespaceWinRT.Name == string.Empty || eachNamespaceWinRT.ClassWinRTs.Count == 0)
                        {
                            namespaceWinRTsToRemove.Add(eachNamespaceWinRT);
                            continue;
                        }

                        // If a class has no name, or no id (it's ok for it to have no members), then remove it it.
                        List<ClassWinRT> classWinRTsToRemove = new List<ClassWinRT>();

                        foreach (ClassWinRT eachClassWinRT in eachNamespaceWinRT.ClassWinRTs)
                        {
                            if (eachClassWinRT.Id == string.Empty || eachClassWinRT.Name == string.Empty)
                            {
                                classWinRTsToRemove.Add(eachClassWinRT);
                                continue;
                            }

                            // See if we're meant to ignore this type because it's not part of UWP, even though it's in a namespace that is.
                            if (ProgramBase.UWPExcludedTypes.Contains(eachNamespaceWinRT.Name + '.' + eachClassWinRT.Name))
                            {
                                classWinRTsToRemove.Add(eachClassWinRT);
                                continue;
                            }
                        }
                        foreach (ClassWinRT eachClassWinRT in classWinRTsToRemove)
                        {
                            eachNamespaceWinRT.ClassWinRTs.Remove(eachClassWinRT);
                        }
                    }
                    foreach (NamespaceWinRT eachNamespaceWinRT in namespaceWinRTsToRemove)
                    {
                        this.apiRefModel.NamespaceWinRTs.Remove(eachNamespaceWinRT);
                    }
                }
                return this.apiRefModel;
            }
        }
    }

    /// <summary>
    /// Represents the set of APIs and features in a platform.
    /// </summary>
    internal enum Platform
    {
        /// <summary>
        /// UWP APIs and features supported by Windows 10
        /// </summary>
        UWPWindows10,
        /// <summary>
        /// WinRT APIs and features supported by Windows 8.x, Windows Phone 8.x, and Windows 10
        /// </summary>
        WinRTWindows8xAnd10,
        /// <summary>
        /// Win32 APIs and features supported by Desktop
        /// </summary>
        Win32DesktopDotTxt,
        /// <summary>
        /// Win32 APIs and features supported by Windows Server (only)
        /// </summary>
        Win32WsuaDotTxt,
        /// <summary>
        /// Win32 APIs and features supported by the Windows Driver Kit
        /// </summary>
        Win32WdkDotTxt
    }

    /// <summary>
    /// Represents a content type, whether conceptual or ref or both.
    /// </summary>
    internal enum DocSetType
    {
        ConceptualAndReference,
        ConceptualOnly,
        ReferenceOnly
    }

    /// <summary>
    /// Represents the purpose of a console message to determine how to color it.
    /// </summary>
    internal enum ConsoleWriteStyle
    {
        Default,
        Highlight,
        Success,
        Warning,
        Error,
        OutputFromAnotherProcess
    }

    /// <summary>
    /// Represents an output log file. You declare, initialize, register, and add to the log.
    /// ProgramBase will take care of writing the file to disk. A log can have headers. A log is only
    /// written to disk if it contains rows. ConsoleWriteStyle is the style used by ProgramBase to
    /// inform the user that the log was written, and what its filename is.
    /// </summary>
    internal class Log : List<string>
    {
        /// <summary>
        /// A label that <see cref="ProgramBase"/> writes at the start of the log (unless the log has headers).
        /// </summary>
        public string? Label = null;
        /// <summary>
        /// The filename to use. Make this as descriptive as possible, especially for logs with headers.
        /// </summary>
        public string? Filename = null;
        /// <summary>
        /// The style with which <see cref="ProgramBase"/> should announce the log at the end
        /// of the run. Set this to something conspicuous, or otherwise, as appropriate.
        /// </summary>
        public ConsoleWriteStyle AnnouncementStyle = ConsoleWriteStyle.Default;
        /// <summary>
        /// The headers that <see cref="ProgramBase"/> writes at the start of the log. When you add an entry
        /// to the log, include as many delimited values per entry as there are headers. This will make the
        /// log easy to open and view in Excel.
        /// </summary>
        public string[]? Headers = null;
        /// <summary>
        /// Only applies if the log has headers. Use a delimiter value that's not likely to occur within the
        /// delimited values.
        /// </summary>
        public char HeaderDelimiter = '|';
        /// <summary>
        /// When you add an entry to the log, you can use this format string to format values into an entry.
        /// </summary>
        public string? FormatString
        {
            get
            {
                if (this.Headers is not null)
                {
                    string format = "{0}";
                    for (int i = 1; i < this.Headers.Length; ++i)
                    {
                        format += this.HeaderDelimiter + "{" + i.ToString() + "}";
                    }
                    return format;
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Call this to add a single entry to the log, where the entry consists of the one or more object values supplied. If the log has headers then add as many values as you added headers.
        /// </summary>
        public void AddEntry(params object[] args)
        {
            if (args is null)
            {
                ProgramBase.ConsoleWrite("You called Log.AddEntry with no values.", ConsoleWriteStyle.Error);
                throw new MDSDKException();
            }

            if (this.Headers is not null)
            {
                try
                {
                    this.Add(string.Format(this.FormatString!, args));
                }
                catch (FormatException ex)
                {
                    ProgramBase.ConsoleWrite("You called Log.AddEntry with values that caused an exception:", ConsoleWriteStyle.Error);
                    ProgramBase.ConsoleWrite(ex.Message, ConsoleWriteStyle.Error);
                    ProgramBase.ConsoleWrite("Pass as many values as the log has headers.", ConsoleWriteStyle.Error);
                    throw new MDSDKException();
                }
            }
            else
            {
                if (args.Length != 1)
                {
                    ProgramBase.ConsoleWrite("You called Log.AddEntry but didn't provide exactly one value.", ConsoleWriteStyle.Error);
                    throw new MDSDKException();
                }
                this.Add(args[0].ToString()!);
            }
        }
    }

    /// <summary>
    /// Exception class used by the MDSDK.
    /// </summary>
    internal class MDSDKException : Exception { }
}
