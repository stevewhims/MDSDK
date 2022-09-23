using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MDSDKBase;
using MDSDKDerived;

namespace MDSDK
{
    /// <summary>
    /// Represents a process in which to perform Git commands.
    /// </summary>
    internal class GitProcess
    {
        private static ProcessStartInfo ProcessStartInfo = new ProcessStartInfo("cmd");

        static GitProcess()
        {
            GitProcess.ProcessStartInfo.CreateNoWindow = true;
            GitProcess.ProcessStartInfo.RedirectStandardInput = true;
            GitProcess.ProcessStartInfo.RedirectStandardOutput = true;
            GitProcess.ProcessStartInfo.UseShellExecute = false;
            GitProcess.ProcessStartInfo.WorkingDirectory = $"{ProgramBase.MyContentReposFolderDirectoryInfo.FullName}\\{ProgramBase.ContentRepoName}";
        }

        static internal void CreatePersonalBranch(bool makeItATrackingBranch = true)
        {
            ProgramBase.ConsoleWrite($"{Environment.NewLine}GITGITGIT :: ", ConsoleWriteStyle.Default, 0);
            ProgramBase.ConsoleWrite("Creating a personal branch", ConsoleWriteStyle.Highlight, 0);
            if (makeItATrackingBranch) ProgramBase.ConsoleWrite(", then making it a tracking branch", ConsoleWriteStyle.Highlight, 0);
            else ProgramBase.ConsoleWrite(", local only", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite($" :: GITGITGIT{Environment.NewLine}", ConsoleWriteStyle.Default);
            using (var createPersonalBranchProcess = Process.Start(GitProcess.ProcessStartInfo))
            {
                createPersonalBranchProcess.StandardInput.WriteLine($"git checkout -b {ProgramBase.MyAlias}-{ProgramBase.BaseBranchName}/{ProgramBase.PersonalBranchName} origin/{ProgramBase.BaseBranchName}");
                if (makeItATrackingBranch) createPersonalBranchProcess.StandardInput.WriteLine($"git push --set-upstream origin {ProgramBase.MyAlias}-{ProgramBase.BaseBranchName}/{ProgramBase.PersonalBranchName}");
                createPersonalBranchProcess.StandardInput.Close();
                createPersonalBranchProcess.WaitForExit();
                ProgramBase.ConsoleWrite(createPersonalBranchProcess.StandardOutput.ReadToEnd(), ConsoleWriteStyle.OutputFromAnotherProcess);
                createPersonalBranchProcess.WaitForExit();
            }
        }

        static internal void StageAndCommit()
        {
            ProgramBase.ConsoleWrite($"{Environment.NewLine}GITGITGIT :: ", ConsoleWriteStyle.Default, 0);
            ProgramBase.ConsoleWrite("Staging and committing", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite($" :: GITGITGIT{Environment.NewLine}", ConsoleWriteStyle.Default);
            using (var stageAndCommitProcess = Process.Start(GitProcess.ProcessStartInfo))
            {
                stageAndCommitProcess.StandardInput.WriteLine("git add .");
                stageAndCommitProcess.StandardInput.WriteLine($"git commit -a -m \"{ProgramBase.CommitMessage}\"");
                stageAndCommitProcess.StandardInput.WriteLine("git push");
                stageAndCommitProcess.StandardInput.Close();
                stageAndCommitProcess.WaitForExit();
                ProgramBase.ConsoleWrite(stageAndCommitProcess.StandardOutput.ReadToEnd(), ConsoleWriteStyle.OutputFromAnotherProcess);
                stageAndCommitProcess.WaitForExit();
            }
        }

        static internal void DeletePersonalBranch(bool deleteRemoteBranch = true)
        {
            ProgramBase.ConsoleWrite($"{Environment.NewLine}GITGITGIT :: ", ConsoleWriteStyle.Default, 0);
            ProgramBase.ConsoleWrite("Deleting personal branch", ConsoleWriteStyle.Highlight, 0);
            if (deleteRemoteBranch) ProgramBase.ConsoleWrite(", including remote branch", ConsoleWriteStyle.Highlight, 0);
            else ProgramBase.ConsoleWrite(", local only", ConsoleWriteStyle.Highlight, 0);
            ProgramBase.ConsoleWrite($" :: GITGITGIT{Environment.NewLine}", ConsoleWriteStyle.Default);
            using (var deletePersonalBranchProcess = Process.Start(GitProcess.ProcessStartInfo))
            {
                if (deleteRemoteBranch) deletePersonalBranchProcess.StandardInput.WriteLine($"git push origin --delete {ProgramBase.MyAlias}-{ProgramBase.BaseBranchName}/{ProgramBase.PersonalBranchName}");
                deletePersonalBranchProcess.StandardInput.WriteLine("git checkout main");
                deletePersonalBranchProcess.StandardInput.WriteLine($"git branch -D {ProgramBase.MyAlias}-{ProgramBase.BaseBranchName}/{ProgramBase.PersonalBranchName}");
                deletePersonalBranchProcess.StandardInput.WriteLine("git pull -p");
                deletePersonalBranchProcess.StandardInput.Close();
                deletePersonalBranchProcess.WaitForExit();
                ProgramBase.ConsoleWrite(deletePersonalBranchProcess.StandardOutput.ReadToEnd(), ConsoleWriteStyle.OutputFromAnotherProcess);
                deletePersonalBranchProcess.WaitForExit();
            }
        }
    }
}