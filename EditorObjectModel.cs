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
        H1Found,
        SyntaxFound,
        ChildElementsH2Found,
        ChildElementsH2BetweenSections,
        ChildElementH3Found,
        OtherHeadingFound,
        RemarksH2Found,
        ExamplesH2Found,
        RequirementsH2Found,
        BetweenSections,
        EndFound
    }

    internal class TopicLines : List<string>
    {
        public void AppendLine(string line)
        {
            this.Add(line);
        }
    }

    internal class EditorObjectModelChildElementsH3Section
    {
        public string Element { get; private set; }
        public TopicLines Lines { get; private set; }

        public EditorObjectModelChildElementsH3Section(string elementName)
        {
            this.Element = elementName.Substring(4); // Remove the "### ".
            this.Lines = new TopicLines();
        }

        public void AppendLineToChildElementsH3Section(string line)
        {
            this.Lines.AppendLine(line);
        }
    }

    internal class EditorObjectModel
    {
        public EditorObjectModelYamlFrontmatter? YamlFrontmatter { get; private set; }
        public TopicLines Description { get; private set; }
        public TopicLines Remarks { get; private set; }
        public TopicLines Examples { get; private set; }
        public Table? ChildElementsTable { get; private set; }
        public Table? RequirementsTable { get; private set; }

        public List<EditorObjectModelChildElementsH3Section>? ChildElementsH3Sections { get; private set; }

        public EditorObjectModel()
        {
            this.YamlFrontmatter = new EditorObjectModelYamlFrontmatter();
            this.Description = new TopicLines();
            //this.Remarks = new TopicLines();
            //this.Examples = new TopicLines();
        }

        public void AppendLineToDescription(string line)
        {
            this.Description.AppendLine(line);
        }

        public EditorObjectModelChildElementsH3Section AppendChildElementsH3Section(string elementName)
        {
            var childElementsH3Section = new EditorObjectModelChildElementsH3Section(elementName);
            this.ChildElementsH3Sections ??= new List<EditorObjectModelChildElementsH3Section>();
            this.ChildElementsH3Sections.Add(childElementsH3Section);
            return childElementsH3Section;
        }

        public TopicLines? FindChildElementsH3SectionTopicLinesForElement(string element)
        {
            if (this.ChildElementsH3Sections is not null)
            {
                foreach (var childElementsH3Section in this.ChildElementsH3Sections)
                {
                    if (childElementsH3Section.Element == element) return childElementsH3Section.Lines;
                }
            }
            return null;
        }

        public void AppendLineToRemarks(string line)
        {
            this.Remarks ??= new TopicLines();
            this.Remarks.AppendLine(line);
        }

        public void AppendLineToExamples(string line)
        {
            this.Examples ??= new TopicLines();
            this.Examples.AppendLine(line);
        }

        public void SetChildElementsTable(Table? childElementsTable)
        {
            this.ChildElementsTable = childElementsTable;
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
        public EditorObjectModelYamlKeyValuePair? YamlTitle { get; private set; }
        public EditorObjectModelYamlKeyValuePair? YamlDescription { get; private set; }
        public EditorObjectModelYamlKeyValuePair? YamlMsAssetId { get; private set; }
        public EditorObjectModelYamlKeyValuePair? YamlMsDate { get; private set; }

        public EditorObjectModelYamlFrontmatter()
        {
            this.YamlKeyValueBases = new List<EditorObjectModelYamlKeyValueBase>();
        }
    }
}
