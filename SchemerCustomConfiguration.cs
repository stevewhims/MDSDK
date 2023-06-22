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
    internal class SchemerCustomConfigurationAppendixElement
    {
        public string? Name { get; private set; }
        public string? Url { get; private set; }
        public string? ComesAfter { get; private set; }

        public SchemerCustomConfigurationAppendixElement(StreamReader streamReader)
        {
            string? value = null;

            string currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIX_ELEMENT_NAME_CONFIG_KEY, ref value))
            {
                this.Name = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIX_ELEMENT_URL_CONFIG_KEY, ref value))
            {
                this.Url = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIX_ELEMENT_COMESAFTER_CONFIG_KEY, ref value))
            {
                this.ComesAfter = value;
            }
        }
    }

    /// <summary>
    /// Reads custom configuration info for XML schema content.
    /// </summary>
    internal class SchemerCustomConfiguration
    {
        private static List<SchemerCustomConfigurationAppendixElement> _schemerCustomConfigurationAppendixElements = new List<SchemerCustomConfigurationAppendixElement>();

        public const string H1_APPENDIX_ELEMENT_CONFIG_KEY = "# AppendixElement";
        public const string H2_APPENDIX_ELEMENT_NAME_CONFIG_KEY = "## AppendixElement.Name";
        public const string H2_APPENDIX_ELEMENT_URL_CONFIG_KEY = "## AppendixElement.Url";
        public const string H2_APPENDIX_ELEMENT_COMESAFTER_CONFIG_KEY = "## AppendixElement.ComesAfter";

        public static bool GetConfigValue(string currentLine, string key, ref string? value)
        {
            if (currentLine.StartsWith(key))
            {
                value = currentLine.Substring(key.Length).Trim();
                return true;
            }
            return false;
        }

        public static void ReadConfigurationFile(Schemer schemer)
        {
            ProgramBase.SetCurrentDirectory(ProgramBase.ExeFolderPath);

            FileInfo fileInfo = new FileInfo($"{SchemerEditorBase.SchemaDisplayName}-configuration.txt");
            if (fileInfo.Exists)
            {
                ProgramBase.ConsoleWrite($"FOUND CUSTOM CONFIGURATION FILE {fileInfo.Name} in the Output Directory.", ConsoleWriteStyle.Highlight);
            }
            else
            {
                ProgramBase.ConsoleWrite($"NO CUSTOM CONFIGURATION FILE. If you need one, create {fileInfo.Name} in the Output Directory.", ConsoleWriteStyle.Warning);
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                string? currentLine;

                while (null != (currentLine = streamReader.ReadLine()))
                {
                    currentLine = currentLine.Trim();
                    if (!currentLine.StartsWith("//") && currentLine.Length > 0)
                    {
                        if (currentLine == SchemerCustomConfiguration.H1_APPENDIX_ELEMENT_CONFIG_KEY)
                        {
                            SchemerCustomConfiguration._schemerCustomConfigurationAppendixElements.Add(new SchemerCustomConfigurationAppendixElement(streamReader));
                        }
                    }
                }
            }
            ProgramBase.SetCurrentDirectory(ProgramBase.MyContentReposFolderDirectoryInfo!);
        }

        public static SchemerCustomConfigurationAppendixElement? FindSchemerCustomConfigurationAppendixElementForName(string name)
        {
            foreach (var schemerCustomConfigurationAppendixElement in SchemerCustomConfiguration._schemerCustomConfigurationAppendixElements)
            {
                if (schemerCustomConfigurationAppendixElement.ComesAfter == name) return schemerCustomConfigurationAppendixElement;

            }
            return null;
        }
    }
}
