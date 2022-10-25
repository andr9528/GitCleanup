using System.Management.Automation;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class BranchService : BaseService
    {
        private const string GIT_GET_ALL_REMOTE_BRANCHES =
            @"git for-each-ref --sort=creatordate --format '%(refname) %(creatordate)' refs/remotes/origin";

        private readonly IEnumerable<(Area Area, Regex Pattern)> branchPatterns =
            new List<(Area Area, Regex Pattern)>
            {
                (Area.CORE, new Regex(@"\/feat\/")),
                (Area.GATEWAY_INEWS, new Regex(@"\/feat\/")),
                (Area.BLUEPRINTS, new Regex(@"\/feat\/")),
                (Area.BLUEPRINTS, new Regex(@"\/test\/")),
                (Area.BLUEPRINTS, new Regex(@"\/chore\/")),
                (Area.BLUEPRINTS, new Regex(@"\/fix\/")),
                (Area.CORE, new Regex(@"\/feature\/")),
                (Area.CORE, new Regex(@"\/fix\/")),
                (Area.GATEWAY_INEWS, new Regex(@"\/fix\/")),
                (Area.CORE, new Regex(@"\/contribute\/")),
                (Area.CORE, new Regex(@"\/dist\/")),
                (Area.CORE, new Regex(@"\/test\/")),
            };

        private void BuildGetBranchesCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{GIT_GET_ALL_REMOTE_BRANCHES}");
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
                //WritePowershellLines(branches, area);

                var deleteBranches =
                    FindMatchingPowershellLines(branches, branchPatterns.Where(x => x.Area == area.Key).ToList());
                Console.WriteLine($"Total {area.Key} Branches Count to Delete: {deleteBranches.Count}");
                //WritePowershellLines(deleteBranches, area);
            }
        }
    }
}