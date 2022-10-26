using System.Management.Automation;
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
            (Area.BLUEPRINTS, new Regex(@"\/feat\/")),
            (Area.BLUEPRINTS, new Regex(@"\/test\/")),
            (Area.BLUEPRINTS, new Regex(@"\/chore\/")),
            (Area.BLUEPRINTS, new Regex(@"\/fix\/")),
            (Area.CORE, new Regex(@"\/feat\/")),
            (Area.CORE, new Regex(@"\/feature\/")),
            (Area.CORE, new Regex(@"\/fix\/")),
            (Area.CORE, new Regex(@"\/contribute\/")),
            (Area.CORE, new Regex(@"\/dist\/")),
            (Area.CORE, new Regex(@"\/test\/")),
            (Area.CORE, new Regex(@"\/refactor\/")),
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
                Console.WriteLine($"Total {area.Key} Branches Count to Delete: {deleteBranches.Count}");

                double percentageRemoved = Math.Round((double) deleteBranches.Count / branches.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} Branches to be Delete: {percentageRemoved}%");

                shell.Commands.Clear();
                BuildGetUnmergedBranchesCommand(shell, area);

                var unmergedBranches = RunPSScript(shell);
                Console.WriteLine($"Total {area.Key} Branches with Unmerged changes Count: {unmergedBranches.Count}");

                var deleteUnmergedBranches = deleteBranches.Where(x => unmergedBranches.Any(y =>
                    x.ImmediateBaseObject.ToString().Contains(y.ImmediateBaseObject.ToString().TrimStart()))).ToList();
                Console.WriteLine(
                    $"Total {area.Key} Branches to be deleted with unmerged changes Count: {deleteUnmergedBranches.Count}");

                var safelyDeleteBranches = deleteBranches.Except(deleteUnmergedBranches).ToList();
                Console.WriteLine($"Total {area.Key} Branches to be deleted with no unmerged changes Count: {safelyDeleteBranches.Count}");

                //WritePowershellLines(branches, area);
                //WritePowershellLines(deleteBranches, area);
                //WritePowershellLines(unmergedBranches, area);
                //WritePowershellLines(deleteUnmergedBranches, area);
                WritePowershellLines(safelyDeleteBranches, area);
            }
        }
    }
}