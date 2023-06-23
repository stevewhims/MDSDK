using MDSDKBase;
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
            var sanitizedRowCells = new List<string>();
            foreach (string cell in rowCells)
            {
                string trimmedCell = cell.Trim();

                while (trimmedCell.EndsWith(@"<br/>") || trimmedCell.EndsWith(@"<br>"))
                {
                    if (trimmedCell.EndsWith(@"<br/>"))
                    {
                        trimmedCell = trimmedCell.Substring(0, trimmedCell.LastIndexOf(@"<br/>"));
                    }
                    if (trimmedCell.EndsWith(@"<br>"))
                    {
                        trimmedCell = trimmedCell.Substring(0, trimmedCell.LastIndexOf(@"<br>"));
                    }
                    trimmedCell = trimmedCell.Trim();
                }

                sanitizedRowCells.Add(trimmedCell);
            }
            this.RowCells = sanitizedRowCells;
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
        private static Regex TableRowRegex = new Regex(@"\|.*\|", RegexOptions.Compiled);
        private static Regex TableCellRegex = new Regex(@"\|[^\|]*", RegexOptions.Compiled);

        public Table(List<string>? columnHeadings = null, List<TableRow>? rows = null, int firstLineNumberOneBased = -1)
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

        public string Render()
        {
            var rendered = new StringBuilder();

            rendered.Append("|");
            foreach (var columnHeading in this.ColumnHeadings)
            {
                rendered.Append($" {columnHeading} |");
            }

            rendered.Append($"{Environment.NewLine}|");
            foreach (var columnHeading in this.ColumnHeadings)
            {
                rendered.Append($" - |");
            }

            foreach (TableRow row in this.Rows)
            {
                rendered.Append($"{Environment.NewLine}|");
                foreach (var cell in row.RowCells)
                {
                    if (cell.Length > 0)
                    {
                        rendered.Append($" {cell} |");
                    }
                    else
                    {
                        rendered.Append($" |");
                    }
                }
            }

            return rendered.ToString();
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

        public static Table? GetNextTable(string filename, List<string> fileLines, ref int lineNumberToStartAtZeroBased)
        {
            Table? table = null;
            TableParseState tableParseState = TableParseState.NothingFound;

            int currentLineNumberOneBased = lineNumberToStartAtZeroBased + 1;
            string? currentTableRowString;
            for (int ix = lineNumberToStartAtZeroBased; ix < fileLines.Count; ++ix)
            //for (; lineNumberToStartAtZeroBased < fileLines.Count; ++lineNumberToStartAtZeroBased)
            {
                string eachLineTrimmed = fileLines[ix].Trim();

                switch (tableParseState)
                {
                    case TableParseState.NothingFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString is not null)
                        {
                            tableParseState = TableParseState.HeaderColumnHeadingsRowFound;
                            table = new Table(Table.RowToCells(filename, currentTableRowString), null, currentLineNumberOneBased);
                        }
                        break;
                    case TableParseState.HeaderColumnHeadingsRowFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString is not null)
                        {
                            tableParseState = TableParseState.HeaderUnderlineRowFound;
                            table!.ConfirmCellCountsMatch(filename, currentTableRowString);
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table!.LastLineNumberOneBased = currentLineNumberOneBased - 1;
                        }
                        break;
                    case TableParseState.HeaderUnderlineRowFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString is not null)
                        {
                            tableParseState = TableParseState.BodyFound;
                            table!.AddRowIfCellCountsMatch(filename, currentTableRowString);
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table!.LastLineNumberOneBased = currentLineNumberOneBased - 1;
                        }
                        break;
                    case TableParseState.BodyFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString is not null)
                        {
                            table!.AddRowIfCellCountsMatch(filename, currentTableRowString);
                            if (ix + 1 == fileLines.Count)
                            {
                                tableParseState = TableParseState.EndFound;
                                table!.LastLineNumberOneBased = currentLineNumberOneBased - 1;
                            }
                        }
                        else
                        {
                            tableParseState = TableParseState.EndFound;
                            table!.LastLineNumberOneBased = currentLineNumberOneBased - 1;
                        }
                        break;
                    case TableParseState.EndFound:
                        break;
                }
                ++currentLineNumberOneBased;
            }

            lineNumberToStartAtZeroBased = table!.LastLineNumberOneBased - 1; // To make zero-based.

            return table;
        }

        private TableRow ConfirmCellCountsMatch(string filename, string currentTableRowString)
        {
            List<string> rowCells = Table.RowToCells(filename, currentTableRowString);
            if (this.ColumnHeadings.Count == rowCells.Count)
            {
                return new TableRow(rowCells);
            }
            else
            {
                ProgramBase.ConsoleWrite($"{Environment.NewLine}Row found with unexpected number of cells.", ConsoleWriteStyle.Error);
                ProgramBase.ConsoleWrite(filename, ConsoleWriteStyle.Error);
                ProgramBase.ConsoleWrite(currentTableRowString, ConsoleWriteStyle.Error, 2);
                throw new MDSDKException();
            }
        }

        private void AddRowIfCellCountsMatch(string filename, string currentTableRowString)
        {
            this.Rows.Add(ConfirmCellCountsMatch(filename, currentTableRowString));
        }

        private static string? LineToTableRow(string line)
        {
            var rowMatches = Table.TableRowRegex.Matches(line);
            if (rowMatches.Count == 1)
                return rowMatches[0].Value;
            else
                return null;
        }

        private static List<string> RowToCells(string filename, string row)
        {
            // Replace \| with &#x007C;.
            row = row.Replace(@"\|", "&#x007C;");

            var cells = new List<string>();
            var cellMatches = Table.TableCellRegex.Matches(row);

            for (int ix = 0; ix < cellMatches.Count - 1; ++ix)
            {
                // To normalize the cell, remove the leading pipe and then trim.
                // If that results in the empty string, then that's the cell contents.
                string normalizedCell = cellMatches[ix].Value.Substring(1).Trim();

                if (EditorBase.RetrieveMatchesForTwoSpaces(normalizedCell).Count != 0)
                {
                    ProgramBase.ConsoleWrite($"{Environment.NewLine}Two spaces found in table cell.", ConsoleWriteStyle.Warning);
                    ProgramBase.ConsoleWrite(filename, ConsoleWriteStyle.Warning);
                    ProgramBase.ConsoleWrite(normalizedCell, ConsoleWriteStyle.Warning, 2);
                }

                cells.Add(normalizedCell);
            }
            return cells;
        }
    }
}
