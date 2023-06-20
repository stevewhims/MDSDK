using MDSDK;
using MDSDKDerived;
using Microsoft.VisualBasic;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace MDSDKBase
{
    internal enum EditorObjectModelTopicSection
    {
        NothingFound,
        YamlFrontmatterFound,
        ContentAfterYamlFrontmatterFound,
        DescriptionFound,
        SyntaxFound,
        HeadingFound,
        RemarksFound,
        RequirementsFound,
        EndFound
    }

    internal class TopicLines : List<string>
    {
        public void AppendLine(string line)
        {
            this.Add(line);
        }
    }

    internal class EditorObjectModel
    {
        public static string YamlFrontmatterDelimiter = "---";
        public static string SyntaxStartDelimiter = "```syntax";
        public static string CodeBlockEndDelimiter = "```";

        public EditorObjectModelYamlFrontmatter? YamlFrontmatter { get; private set; }
        public TopicLines Description { get; private set; }
        public TopicLines Remarks { get; private set; }
        public Table? RequirementsTable { get; private set; }

        public EditorObjectModel()
        {
            this.YamlFrontmatter = new EditorObjectModelYamlFrontmatter();
            this.Description = new TopicLines();
            this.Remarks = new TopicLines();
        }

        public void AppendLineToDescription(string line)
        {
            this.Description.AppendLine(line);
        }

        public void AppendLineToRemarks(string line)
        {
            this.Remarks.AppendLine(line);
        }

        public void SetRequirementsTable(Table? requirementsTable)
        {
            this.RequirementsTable = requirementsTable;
        }
    }

    internal class EditorObjectModelYamlKeyValueBase
    {
        public string KeyText { get; private set; }

        public EditorObjectModelYamlKeyValueBase(string keyText)
        {
            this.KeyText = keyText;
        }
    }

    internal class EditorObjectModelYamlKeyValuePair : EditorObjectModelYamlKeyValueBase
    {
        public string ValueText { get; private set; }

        public EditorObjectModelYamlKeyValuePair(string keyText, string valueText) : base(keyText)
        {
            this.ValueText = valueText;
        }
    }

    internal class EditorObjectModelYamlKeyValues : EditorObjectModelYamlKeyValueBase
    {
        public List<string> Values { get; private set; }

        public EditorObjectModelYamlKeyValues(string keyText) : base(keyText)
        {
            this.Values = new List<string>();
        }

        public void AddValue(string valueText)
        {
            this.Values.Add(valueText);
        }
    }

    internal class EditorObjectModelYamlFrontmatter
    {
        public List<EditorObjectModelYamlKeyValueBase> YamlKeyValueBases { get; private set; }
        public EditorObjectModelYamlKeyValueBase? YamlTitle { get; private set; }
        public EditorObjectModelYamlKeyValueBase? YamlDescription { get; private set; }
        public EditorObjectModelYamlKeyValueBase? YamlMsAssetId { get; private set; }
        public EditorObjectModelYamlKeyValueBase? YamlMsDate { get; private set; }

        public EditorObjectModelYamlFrontmatter()
        {
            this.YamlKeyValueBases = new List<EditorObjectModelYamlKeyValueBase>();
        }
    }
}
