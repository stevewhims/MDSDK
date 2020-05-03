using MDSDKBase;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MDSDK
{
    // https://carlalexander.ca/beginners-guide-regular-expressions/
    // https://www.regular-expressions.info/
    // https://regex101.com/
    // https://regexr.com/

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
        public List<string> rowCells = new List<string>();

        public TableRow(List<string> rowCells)
        {
            this.rowCells = rowCells;
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
        private List<string> columnHeadings = new List<string>();
        /// <summary>
        /// A string collection representing each cell in a row.
        /// </summary>
        private List<TableRow> rows = new List<TableRow>();

        public int RowCount { get { return this.rows.Count; } }

        public int FirstLineNumberOneBased { get; private set; }
        public int LastLineNumberOneBased { get; private set; }
        private static Regex RowRegex = new Regex(@"\|.*\|", RegexOptions.Compiled);
        private static Regex CellRegex = new Regex(@"\|[^\|]*", RegexOptions.Compiled);

        public Table()
        {
        }

        public void RemoveRowNumberOneBased(int rowNumberOneBased)
        {
            this.rows.RemoveAt(rowNumberOneBased - 1);
        }

        public void RemoveRedundantColumns(params string[] redundantColumnHeadings)
        {
            var redundantColumnHeadingList = new List<string>();
            var seenOne = new List<bool>();
            foreach (string columnHeading in redundantColumnHeadings)
            {
                redundantColumnHeadingList.Add(columnHeading);
                seenOne.Add(false);
            }

            var indicesToDelete = new List<int>();
            for (int ix = 0; ix < this.columnHeadings.Count; ++ix)
            {
                int indexOfRedundantColumnHeading = -1;
                if (-1 != (indexOfRedundantColumnHeading = redundantColumnHeadingList.IndexOf(this.columnHeadings[ix])))
                {
                    if (seenOne[indexOfRedundantColumnHeading])
                    {
                        indicesToDelete.Add(ix);
                    }
                    else
                    {
                        seenOne[indexOfRedundantColumnHeading] = true;
                    }
                }
            }

            for (int ix = indicesToDelete.Count - 1; ix >= 0; --ix)
            {
                this.columnHeadings.RemoveAt(indicesToDelete[ix]);
                foreach (TableRow row in this.rows)
                {
                    row.rowCells.RemoveAt(indicesToDelete[ix]);
                }
            }
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
                            table = new Table();
                            table.FirstLineNumberOneBased = lineNumber;
                            table.columnHeadings = Table.RowToCells(currentTableRowString);
                        }
                        break;
                    case TableParseState.HeaderColumnHeadingsRowFound:
                        currentTableRowString = Table.LineToTableRow(eachLineTrimmed);
                        if (currentTableRowString != null)
                        {
                            tableParseState = TableParseState.HeaderUnderlineRowFound;
                            if (table.columnHeadings.Count != Table.RowToCells(currentTableRowString).Count)
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
                            if (table.columnHeadings.Count != rowCells.Count)
                            {
                                ProgramBase.ConsoleWrite("Cell counts in body row and header don't match.", ConsoleWriteStyle.Error);
                                throw new MDSDKException();
                            }
                            table.rows.Add(new TableRow(rowCells));
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
                            if (table.columnHeadings.Count != rowCells.Count)
                            {
                                ProgramBase.ConsoleWrite("Cell counts in body row and header don't match.", ConsoleWriteStyle.Error);
                                throw new MDSDKException();
                            }
                            table.rows.Add(new TableRow(rowCells));
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
            var rowMatches = Table.RowRegex.Matches(line);
            if (rowMatches.Count == 1)
                return rowMatches[0].Value;
            else
                return null;
        }

        private static List<string> RowToCells(string row)
        {
            var cells = new List<string>();
            var cellMatches = Table.CellRegex.Matches(row);

            for (int ix = 0; ix < cellMatches.Count - 1; ++ix)
            {
                // To normalize the cell, remove the leading pipe and then trim.
                // If that results in the empty string, then that's the cell contents.
                string normalizedCell = cellMatches[ix].Value.Substring(1).Trim();
                cells.Add(normalizedCell);
            }
            return cells;
        }
    }
}