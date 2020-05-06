﻿using MDSDKBase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MDSDK
{
    /// <summary>
    /// Represents a state during attempted parsing for a markdown table.
    /// </summary>
    internal enum TableParseState
    {
        NothingFound,
        HeaderColumnHeadingsRowFound,
        HeaderUnderlineRowFound,
        BodyFound,
        EndFound
    }

    /// <summary>
    /// A class representing a markdown table row.
    /// </summary>
    internal class TableRow
    {
        /// <summary>
        /// A string collection representing each cell in the row.
        /// </summary>
        public List<string> RowCells { get; private set; }

        public TableRow(List<string> rowCells)
        {
            this.RowCells = rowCells;
        }
    }

    /// <summary>
    /// A class representing a markdown table.
    /// This version of the class ignores (discards) alignment; I've never seen a need for it.
    /// </summary>
    internal class Table
    {
        /// <summary>
        /// A string collection representing the column headings.
        /// </summary>
        public List<string> ColumnHeadings { get; private set; }
        /// <summary>
        /// A string collection representing each cell in a row.
        /// </summary>
        public List<TableRow> Rows { get; private set; }

        public int RowCount { get { return this.Rows.Count; } }

        public int FirstLineNumberOneBased { get; private set; }
        public int LastLineNumberOneBased { get; private set; }
        private static Regex TableRowRegex = new Regex(@"/\|.*\|/", RegexOptions.Compiled);
        private static Regex TableCellRegex = new Regex(@"/\|[^\|]*/", RegexOptions.Compiled);

        private Table(List<string> columnHeadings = null, List<TableRow> rows = null, int firstLineNumberOneBased = -1)
        {
            this.ColumnHeadings = columnHeadings ?? new List<string>();
            this.Rows = rows ?? new List<TableRow>();
            this.FirstLineNumberOneBased = firstLineNumberOneBased;
            this.LastLineNumberOneBased = -1;
        }

        public void RemoveRowNumberOneBased(int rowNumberOneBased)
        {
            this.Rows.RemoveAt(rowNumberOneBased - 1);
        }

        public void RemoveRedundantColumns(params string[] redundantColumnHeadings)
        {
            var redundantColumnHeadingList = new List<string>();
            var alreadyEncounteredFirstOccurrence = new List<bool>();
            foreach (string columnHeading in redundantColumnHeadings)
            {
                redundantColumnHeadingList.Add(columnHeading);
                alreadyEncounteredFirstOccurrence.Add(false);
            }

            var indicesToDelete = new List<int>();
            for (int ix = 0; ix < this.ColumnHeadings.Count; ++ix)
            {
                int indexOfRedundantColumnHeading = -1;
                if (-1 != (indexOfRedundantColumnHeading = redundantColumnHeadingList.IndexOf(this.ColumnHeadings[ix])))
                {
                    if (alreadyEncounteredFirstOccurrence[indexOfRedundantColumnHeading])
                    {
                        indicesToDelete.Add(ix);
                    }
                    else
                    {
                        alreadyEncounteredFirstOccurrence[indexOfRedundantColumnHeading] = true;
                    }
                }
            }

            for (int ix = indicesToDelete.Count - 1; ix >= 0; --ix)
            {
                this.ColumnHeadings.RemoveAt(indicesToDelete[ix]);
                foreach (TableRow row in this.Rows)
                {
                    row.RowCells.RemoveAt(indicesToDelete[ix]);
                }
            }
        }

        public string RenderAsMarkdown()
        {
            var markdown = new StringBuilder();

            markdown.Append("|");
            foreach (var columnHeading in this.ColumnHeadings)
            {
                markdown.Append($" {columnHeading} |");
            }

            markdown.Append($"{Environment.NewLine}|");
            foreach (var columnHeading in this.ColumnHeadings)
            {
                markdown.Append($" - |");
            }

            foreach (TableRow row in this.Rows)
            {
                markdown.Append($"{Environment.NewLine}|");
                foreach (var cell in row.RowCells)
                {
                    markdown.Append($" {cell} |");
                }
            }

            return markdown.ToString();
        }

        public (List<Table> tablePerRow, List<List<string>> skippedCellsPerRow) SliceHorizontally(List<string> columnHeadings, int firstColumnIndexZeroBased = 0)
        {
            var tablePerRow = new List<Table>();
            var skippedCellsPerRow = new List<List<string>>();

            foreach (TableRow row in this.Rows)
            {
                var skippedCellsThisRow = new List<string>();
                var rows = new List<TableRow>();

                for (int columnIndex = 0; columnIndex < this.ColumnHeadings.Count; ++columnIndex)
                {
                    if (columnIndex < firstColumnIndexZeroBased)
                    {
                        skippedCellsThisRow.Add(row.RowCells[columnIndex]);
                    }
                    else
                    {
                        var rowCells = new List<string>() { this.ColumnHeadings[columnIndex], row.RowCells[columnIndex] };
                        rows.Add(new TableRow(rowCells));
                    }
                }

                tablePerRow.Add(new Table(columnHeadings, rows));
                skippedCellsPerRow.Add(skippedCellsThisRow);
            }

            return (tablePerRow, skippedCellsPerRow);
        }

        public static Table GetFirstTable(List<string> fileLines)
        {
            Table table = null;
            TableParseState tableParseState = TableParseState.NothingFound;

            int lineNumber = 1;
            string currentTableRowString = null;
            foreach (string eachLine in fileLines)
            {
                string eachLineTrimmed = eachLine.Trim();

                switch (tableParseState)
                {
                    case TableParseState.NothingFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString != null)
                        {
                            tableParseState = TableParseState.HeaderColumnHeadingsRowFound;
                            table = new Table(Table.RowToCells(currentTableRowString), null, lineNumber);
                        }
                        break;
                    case TableParseState.HeaderColumnHeadingsRowFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString != null)
                        {
                            tableParseState = TableParseState.HeaderUnderlineRowFound;
                            if (table.ColumnHeadings.Count != Table.RowToCells(currentTableRowString).Count)
                            {
                                ProgramBase.ConsoleWrite("Cell counts in underline row and headings row and underline row don't match.", ConsoleWriteStyle.Error);
                                throw new MDSDKException();
                            }
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table.LastLineNumberOneBased = lineNumber - 1;
                        }
                        break;
                    case TableParseState.HeaderUnderlineRowFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString != null)
                        {
                            tableParseState = TableParseState.BodyFound;
                            List<string> rowCells = Table.RowToCells(currentTableRowString);
                            if (table.ColumnHeadings.Count != rowCells.Count)
                            {
                                ProgramBase.ConsoleWrite("Cell counts in body row and header don't match.", ConsoleWriteStyle.Error);
                                throw new MDSDKException();
                            }
                            table.Rows.Add(new TableRow(rowCells));
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table.LastLineNumberOneBased = lineNumber - 1;
                        }
                        break;
                    case TableParseState.BodyFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString != null)
                        {
                            List<string> rowCells = Table.RowToCells(currentTableRowString);
                            if (table.ColumnHeadings.Count != rowCells.Count)
                            {
                                ProgramBase.ConsoleWrite("Cell counts in body row and header don't match.", ConsoleWriteStyle.Error);
                                throw new MDSDKException();
                            }
                            table.Rows.Add(new TableRow(rowCells));
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table.LastLineNumberOneBased = lineNumber - 1;
                        }
                        break;
                    case TableParseState.EndFound:
                        break;
                }
                ++lineNumber;
            }

            return table;
        }

        private static string LineToTableRow(string line)
        {
            var rowMatches = Table.TableRowRegex.Matches(line);
            if (rowMatches.Count == 1)
                return rowMatches[0].Value;
            else
                return null;
        }

        private static List<string> RowToCells(string row)
        {
            var cells = new List<string>();
            var cellMatches = Table.TableCellRegex.Matches(row);

            for (int ix = 0; ix < cellMatches.Count - 1; ++ix)
            {
                // To normalize the cell, remove the leading pipe and then trim.
                // If that results in the empty string, then that's the cell contents.
                string normalizedCell = cellMatches[ix].Value.Substring(1).Trim();

                var twoSpacesMatches = EditorBase.TwoSpacesRegex.Matches(normalizedCell);
                if (twoSpacesMatches.Count != 0)
                {
                    ProgramBase.ConsoleWrite(normalizedCell);
                }

                cells.Add(normalizedCell);
            }
            return cells;
        }
    }
}