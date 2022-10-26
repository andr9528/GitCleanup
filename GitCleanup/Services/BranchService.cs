﻿using System.Management.Automation;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class BranchService : BaseService
    {
        private const string GIT_GET_ALL_REMOTE_BRANCHES =
            @"git for-each-ref --sort=creatordate --format '%(refname) %(creatordate)' refs/remotes/origin";

        private const string GIT_GET_ALL_REMOTE_BRANCHES_WITH_UNMERGED = @"git branch -r --no-merged";

        private readonly IEnumerable<(Area Area, Regex Pattern)> branchPatterns = new List<(Area Area, Regex Pattern)>
        {
            (Area.GATEWAY_INEWS, new Regex(@"\/feat\/")),
            (Area.GATEWAY_INEWS, new Regex(@"\/fix\/")),
            (Area.GATEWAY_INEWS, new Regex(@"\/SOF-")),
            (Area.BLUEPRINTS, new Regex(@"\/feat\/")),
            (Area.BLUEPRINTS, new Regex(@"\/test\/")),
            (Area.BLUEPRINTS, new Regex(@"\/chore\/")),
            (Area.BLUEPRINTS, new Regex(@"\/fix\/")),
            (Area.BLUEPRINTS, new Regex(@"\/SOF-")),
            (Area.CORE, new Regex(@"\/feat\/")),
            (Area.CORE, new Regex(@"\/feature\/")),
            (Area.CORE, new Regex(@"\/fix\/")),
            (Area.CORE, new Regex(@"\/contribute\/")),
            (Area.CORE, new Regex(@"\/dist\/")),
            (Area.CORE, new Regex(@"\/test\/")),
            (Area.CORE, new Regex(@"\/refactor\/")),
            (Area.CORE, new Regex(@"\/SOF-")),
            (Area.TSR, new Regex(@"\/feat\/")),
            (Area.TSR, new Regex(@"\/feature\/")),
            (Area.TSR, new Regex(@"\/fix\/")),
            (Area.TSR, new Regex(@"\/test\/")),
            (Area.TSR, new Regex(@"\/hotfix\/")),
            (Area.TSR, new Regex(@"\/contribute\/")),
        };

        private void BuildGetBranchesCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{GIT_GET_ALL_REMOTE_BRANCHES}");
        }

        private void BuildGetUnmergedBranchesCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{GIT_GET_ALL_REMOTE_BRANCHES_WITH_UNMERGED}");
        }

        public void WriteBranches(Dictionary<Area, string> areas)
        {
            foreach (var area in areas)
            {
                Console.WriteLine($"Running Branch commands for: {area.Key}");

                using var shell = PowerShell.Create();
                BuildGetBranchesCommand(shell, area);

                var branches = RunPSScript(shell);
                Console.WriteLine($"Total {area.Key} Branches Count: {branches.Count}");
                var deleteBranches =
                    FindMatchingPowershellLines(branches, branchPatterns.Where(x => x.Area == area.Key).ToList());
                Console.WriteLine($"Total {area.Key} Branches to be deleted Count: {deleteBranches.Count}");

                shell.Commands.Clear();
                BuildGetUnmergedBranchesCommand(shell, area);
                var unmergedBranches = RunPSScript(shell);
                Console.WriteLine($"Total {area.Key} Branches with Unmerged changes Count: {unmergedBranches.Count}");

                var unsafeDeleteBranches = deleteBranches.Where(x => unmergedBranches.Any(y =>
                    x.ImmediateBaseObject.ToString().Contains(y.ImmediateBaseObject.ToString().TrimStart()))).ToList();
                Console.WriteLine(
                    $"Total {area.Key} Branches to be deleted with unmerged changes Count: {unsafeDeleteBranches.Count}");
                var safelyDeleteBranches = deleteBranches.Except(unsafeDeleteBranches).ToList();
                Console.WriteLine($"Total {area.Key} Branches to be deleted with no unmerged changes Count: {safelyDeleteBranches.Count}");

                double totalPercentageRemoved = Math.Round((double) deleteBranches.Count / branches.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} Branches to be Delete: {totalPercentageRemoved}%");
                double unsafePercentageRemoved =
                    Math.Round((double) unsafeDeleteBranches.Count / branches.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} unmerged Branches to be Delete: {unsafePercentageRemoved}%");
                double safePercentageRemoved =
                    Math.Round((double) safelyDeleteBranches.Count / branches.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} merged Branches to be Delete: {safePercentageRemoved}%");

                //WritePowershellLines(branches, area, $"All branches for {area}.");
                //WritePowershellLines(deleteBranches, area, $"All branches for {area}, that is marked for deletion.");
                //WritePowershellLines(unmergedBranches, area, $"All branches for {area}, with unmerged changes.");
                //WritePowershellLines(unsafeDeleteBranches, area, $"All branches for {area}, that is marked for deletion and has unmerged changes.");
                WritePowershellLines(safelyDeleteBranches, area, $"All branches for {area}, that is marked for deletion and has no unmerged changes.");
                Console.WriteLine($"");
            }
        }
    }
}