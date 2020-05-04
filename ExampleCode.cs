//private void RefactorTables()
//{
//    var refactoredTables = new Log()
//    {
//        Label = "Unreadable tables refactored into readable ones.",
//        Filename = "refactored-tables-log.txt",
//        AnnouncementStyle = ConsoleWriteStyle.Default
//    };
//    this.RegisterLog(refactoredTables);

//    var projectDirectoryInfo = new DirectoryInfo(@"C:\Users\stwhi\source\repos\win32-pr\desktop-src\direct3ddxgi");
//    Editor editor = EditorBase.GetEditorForTopicFileName(projectDirectoryInfo, "hardware-support-for-direct3d-12-1-formats.md");

//    Table firstTable = editor.GetFirstTable();
//    if (firstTable != null)
//    {
//        firstTable.RemoveRowNumberOneBased(firstTable.RowCount);
//        firstTable.RemoveRedundantColumns(@"\#", @"Format ( DXGI\_FORMAT\_\* )");

//        List<Table> tablePerRow = null;
//        List<List<string>> skippedCellsPerRow = null;
//        (tablePerRow, skippedCellsPerRow) = firstTable.SliceHorizontally(new List<string> { "Target", "Support" }, 2);

//        for (int tableIndex = 0; tableIndex < tablePerRow.Count; ++tableIndex)
//        {
//            string heading = $"{Environment.NewLine}## DXGI_FORMAT_{skippedCellsPerRow[tableIndex][1]} ({skippedCellsPerRow[tableIndex][0]})";
//            refactoredTables.Add(heading);
//            refactoredTables.Add(tablePerRow[tableIndex].RenderAsMarkdown());
//        }
//    }
//}