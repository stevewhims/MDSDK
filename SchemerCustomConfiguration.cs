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
        public string? Element { get; private set; }
        public string? Url { get; private set; }
        public string? Type { get; private set; }
        public string? Description { get; private set; }
        public string? ComesAfter { get; private set; }

        public SchemerCustomConfigurationAppendixElement(StreamReader streamReader)
        {
            string? value = null;

            string currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIXELEMENT_ELEMENT_CONFIG_KEY, ref value))
            {
                this.Element = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIXELEMENT_URL_CONFIG_KEY, ref value))
            {
                this.Url = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIXELEMENT_TYPE_CONFIG_KEY, ref value))
            {
                this.Type = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIXELEMENT_DESCRIPTION_CONFIG_KEY, ref value))
            {
                this.Description = value;
            }

            currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_APPENDIXELEMENT_COMESAFTER_CONFIG_KEY, ref value))
            {
                this.ComesAfter = value;
            }
        }
    }

    internal class SchemerCustomConfigurationSyntaxComment
    {
        public List<string> Lines { get; private set; }
        public string? ComesAfter { get; private set; }

        public SchemerCustomConfigurationSyntaxComment(StreamReader streamReader)
        {
            this.Lines = new List<string>();

            string? value = null;

            string? currentLine;

            while (null != (currentLine = streamReader.ReadLine()))
            {
                currentLine = currentLine.Trim();

                if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_SYNTAXCOMMENT_LINES_CONFIG_KEY, ref value))
                {
                    this.Lines.Add(value!);
                }
                else
                {
                    break;
                }
            }

            if (SchemerCustomConfiguration.GetConfigValue(currentLine!, SchemerCustomConfiguration.H2_SYNTAXCOMMENT_COMESAFTER_CONFIG_KEY, ref value))
            {
                this.ComesAfter = value;
            }
        }

        public string Render(int numberOfCharsToIndent)
        {
            string commentString = string.Empty;
            commentString += EditorBase.RenderIndent(numberOfCharsToIndent);
            commentString += "<!-" + Environment.NewLine;

            foreach (var line in this.Lines)
            {
                commentString += EditorBase.RenderIndent(numberOfCharsToIndent + EditorBase.NumberOfCharsToIndentIncrement);
                commentString += line + Environment.NewLine;
            }

            commentString += EditorBase.RenderIndent(numberOfCharsToIndent);
            commentString += " ->" + Environment.NewLine;

            return commentString;
        }
    }

    internal class SchemerCustomConfigurationUniqueification
    {
        public string? Element { get; private set; }

        public SchemerCustomConfigurationUniqueification(StreamReader streamReader)
        {
            string? value = null;

            string currentLine = streamReader.ReadLine()!;
            currentLine = currentLine.Trim();
            if (SchemerCustomConfiguration.GetConfigValue(currentLine, SchemerCustomConfiguration.H2_UNIQUEIFICATION_ELEMENT_CONFIG_KEY, ref value))
            {
                this.Element = value;
            }
        }
    }

    /// <summary>
    /// Reads custom configuration info for XML schema content.
    /// </summary>
    internal class SchemerCustomConfiguration
    {
        private static List<SchemerCustomConfigurationAppendixElement> _schemerCustomConfigurationAppendixElements = new List<SchemerCustomConfigurationAppendixElement>();
        private static List<SchemerCustomConfigurationSyntaxComment> _schemerCustomConfigurationSyntaxComments = new List<SchemerCustomConfigurationSyntaxComment>();
        private static List<SchemerCustomConfigurationUniqueification> _schemerCustomConfigurationUniqueifications = new List<SchemerCustomConfigurationUniqueification>();

        public const string H1_APPENDIXELEMENT_CONFIG_KEY = "# AppendixElement";
        public const string H2_APPENDIXELEMENT_ELEMENT_CONFIG_KEY = "## AppendixElement.Element";
        public const string H2_APPENDIXELEMENT_URL_CONFIG_KEY = "## AppendixElement.Url";
        public const string H2_APPENDIXELEMENT_TYPE_CONFIG_KEY = "## AppendixElement.Type";
        public const string H2_APPENDIXELEMENT_DESCRIPTION_CONFIG_KEY = "## AppendixElement.Description";
        public const string H2_APPENDIXELEMENT_COMESAFTER_CONFIG_KEY = "## AppendixElement.ComesAfter";

        public const string H1_SYNTAXCOMMENT_CONFIG_KEY = "# SyntaxComment";
        public const string H2_SYNTAXCOMMENT_LINES_CONFIG_KEY = "## SyntaxComment.Lines";
        public const string H2_SYNTAXCOMMENT_COMESAFTER_CONFIG_KEY = "## SyntaxComment.ComesAfter";

        public const string H1_UNIQUEIFICATION_CONFIG_KEY = "# Uniqueification";
        public const string H2_UNIQUEIFICATION_ELEMENT_CONFIG_KEY = "## Uniqueification.Element";

        //## SyntaxComment.Lines Extension point for other namespaces, including the OneX
        //## SyntaxComment.Lines namespace currently used for optional IEEE802.1X configuration.
        //## SyntaxComment.Lines The OneX configuration parameters must be present if the 
        //## SyntaxComment.Lines <OneXEnforced> flag is set to "true" or the <OneXEnabled> flag
        //## SyntaxComment.Lines is set to "true". See the Child elements section below.
        //## SyntaxComment.ComesAfter OneXEnabled (security)

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
                ProgramBase.ConsoleWrite($"FOUND CUSTOM CONFIGURATION FILE {fileInfo.Name} in the Output Directory.", ConsoleWriteStyle.Highlight, 0);
            }
            else
            {
                ProgramBase.ConsoleWrite($"NO CUSTOM CONFIGURATION FILE. If you need one, create {fileInfo.Name} in the Output Directory.", ConsoleWriteStyle.Warning);
                return;
            }

            using (StreamReader streamReader = fileInfo.OpenText())
            {
                string? currentLine;

                while (null != (currentLine = streamReader.ReadLine()))
                {
                    currentLine = currentLine.Trim();
                    if (!currentLine.StartsWith("//") && currentLine.Length > 0)
                    {
                        if (currentLine == SchemerCustomConfiguration.H1_APPENDIXELEMENT_CONFIG_KEY)
                        {
                            SchemerCustomConfiguration._schemerCustomConfigurationAppendixElements.Add(new SchemerCustomConfigurationAppendixElement(streamReader));
                        }
                        else if (currentLine == SchemerCustomConfiguration.H1_SYNTAXCOMMENT_CONFIG_KEY)
                        {
                            SchemerCustomConfiguration._schemerCustomConfigurationSyntaxComments.Add(new SchemerCustomConfigurationSyntaxComment(streamReader));
                        }
                        else if (currentLine == SchemerCustomConfiguration.H1_UNIQUEIFICATION_CONFIG_KEY)
                        {
                            SchemerCustomConfiguration._schemerCustomConfigurationUniqueifications.Add(new SchemerCustomConfigurationUniqueification(streamReader));
                        }
                    }
                }
            }

            ProgramBase.ConsoleWrite($" Found {SchemerCustomConfiguration._schemerCustomConfigurationAppendixElements.Count} AppendixElement(s).", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite($" Found {SchemerCustomConfiguration._schemerCustomConfigurationSyntaxComments.Count} SyntaxComment(s).", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite($" Found {SchemerCustomConfiguration._schemerCustomConfigurationUniqueifications.Count} Uniqueification(s).", ConsoleWriteStyle.Highlight);

            ProgramBase.SetCurrentDirectory(ProgramBase.MyContentReposFolderDirectoryInfo!);
        }

        public static SchemerCustomConfigurationAppendixElement? FindSchemerCustomConfigurationAppendixElementForComesAfterElement(string comesAfterElement)
        {
            foreach (var schemerCustomConfigurationAppendixElement in SchemerCustomConfiguration._schemerCustomConfigurationAppendixElements)
            {
                if (schemerCustomConfigurationAppendixElement.ComesAfter == comesAfterElement) return schemerCustomConfigurationAppendixElement;
            }
            return null;
        }

        public static SchemerCustomConfigurationSyntaxComment? FindSchemerCustomConfigurationSyntaxCommentForComesAfterElement(string comesAfterElement)
        {
            foreach (var schemerCustomConfigurationSyntaxComment in SchemerCustomConfiguration._schemerCustomConfigurationSyntaxComments)
            {
                if (schemerCustomConfigurationSyntaxComment.ComesAfter == comesAfterElement) return schemerCustomConfigurationSyntaxComment;
            }
            return null;
        }

        public static SchemerCustomConfigurationUniqueification? FindSchemerCustomConfigurationUniqueificationForComesAfterElement(string comesAfterElement)
        {
            foreach (var schemerCustomConfigurationUniqueification in SchemerCustomConfiguration._schemerCustomConfigurationUniqueifications)
            {
                if (schemerCustomConfigurationUniqueification.Element == comesAfterElement) return schemerCustomConfigurationUniqueification;
            }
            return null;
        }
    }
}
