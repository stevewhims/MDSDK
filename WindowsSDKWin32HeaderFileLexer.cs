using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MDSDKBase;
using MDSDKDerived;
using Microsoft.SqlServer.Server;
using System.Text.RegularExpressions;

namespace MDSDK
{
    // See comments in sal.h header file.

    /// <summary>
    /// A class that performs lexical analysis on a Windows SDK Win32 header file.
    /// </summary>
    internal class WindowsSDKWin32HeaderFileLexer : EditorBase
    {
        private static Regex StructureRegex = new Regex(@"(typedef struct.*?[^\{]*)\{(?<structure_body>[^\}]*)\}(?<structure_names>.*?);", RegexOptions.Compiled);

        private WindowsSDKWin32HeaderFileLexer(FileInfo fileInfo) : base(fileInfo) { }

        public static WindowsSDKWin32HeaderFileLexer MakeWindowsSDKWin32HeaderFileLexer(DirectoryInfo eachHeaderDirectoryInfo, bool throwIfNotFound = true)
        {
            DirectoryInfo windowsSDKWin32HeaderFilesDirectoryInfo = new DirectoryInfo(ProgramBase.WindowsSDKWin32HeaderFilesFolderName);

            List<FileInfo> headerFileInfos = windowsSDKWin32HeaderFilesDirectoryInfo.GetFiles(eachHeaderDirectoryInfo.Name + ".h").ToList();
            if (headerFileInfos.Count == 1)
            {
                return new WindowsSDKWin32HeaderFileLexer(headerFileInfos[0]);
            }

            ProgramBase.ConsoleWrite($"{Environment.NewLine}folder \"{windowsSDKWin32HeaderFilesDirectoryInfo.FullName}\" doesn't contain \"{eachHeaderDirectoryInfo.Name + ".h"}\" ", ConsoleWriteStyle.Error, 2);
            if (throwIfNotFound) throw new MDSDKException();
            return null;
        }

        public (string, string) FindStructureDefinition(string structureName)
        {
            // TODO try to use structureName right inside the regex to find typedef struct {...} <structureName>.

            var structureDefinitionatches = WindowsSDKWin32HeaderFileLexer.StructureRegex.Matches(this.fileContents);
            if (structureDefinitionatches.Count > 0)
                return (structureDefinitionatches[0].Groups["structure_body"].Value.Trim(), structureDefinitionatches[0].Groups["structure_names"].Value.Trim());
            else
                return (null, null);

            // TODO trim those names. Look for ones beginning with * because those are the pointer-to types.

            //for (int ix = 0; ix < fileLines.Count; ++ix)
            //{
            //    string eachLineTrimmed = fileLines[ix].Trim();

            //    structureDefinition = WindowsSDKWin32HeaderFileLexer.LineToStructureDefinition(eachLineTrimmed);
            //    if (structureDefinition != null)
            //    {
            //        break;
            //    }
            //}
        }
    }
}
