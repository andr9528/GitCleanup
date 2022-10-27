using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class BranchService : BaseService
    {
        private const string GIT_GET_ALL_REMOTE_BRANCHES =
            @"git for-each-ref --sort=committerdate --format '%(refname)|%(creatordate)|%(committerdate)|%(creator)' refs/remotes/origin";

        private const string GIT_GET_ALL_REMOTE_BRANCHES_WITH_UNMERGED = @"git branch -r --no-merged";
        private const string GIT_DELETE_REMOTE_BRANCH_BASE = @"git push origin --delete ";

        private readonly IEnumerable<(Area Area, Regex Pattern)> branchPatterns = new List<(Area Area, Regex Pattern)>
        {
            (Area.ACTION, new Regex("delete")),
            (Area.ACTION, new Regex("develop")),
        };

        private readonly bool shouldAllowDelete;
        private readonly bool shouldCreatePullRequests;

        public BranchService(bool shouldAllowDelete, bool shouldCreatePullRequests)
        {
            this.shouldAllowDelete = shouldAllowDelete;
            this.shouldCreatePullRequests = shouldCreatePullRequests;
        }

        public void WriteBranches(Dictionary<Area, string> areas)
        {
            foreach (var area in areas)
            {
                Console.WriteLine($"Running Branch commands for: {area.Key}");
                var passAround = new Dictionary<PassAroundContent, IEnumerable<PSObject>>();

                WriteTotals(area, passAround);
                WritePercentages(passAround, area);
                WriteALlLines(passAround, area);
                ProcessBranches(passAround, area);
                Console.WriteLine(
                    "-----------------------------------------------------------------------------------");
            }
        }

        private void BuildDeleteBranchCommand(
            PowerShell shell, KeyValuePair<Area, string> area, IEnumerable<string> names)
        {
            var builder = new StringBuilder(GIT_DELETE_REMOTE_BRANCH_BASE);
            foreach (string name in names)
                builder.Append($"{name} ");

            shell.Commands.Clear();
            shell.AddScript($"cd {area.Value}");
            shell.AddScript(builder.ToString());
        }

        private void BuildGetBranchesCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.Commands.Clear();
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{Program.GIT_FETCH_ALL}");
            shell.AddScript($"{GIT_GET_ALL_REMOTE_BRANCHES}");
        }

        private void BuildGetUnmergedBranchesCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.Commands.Clear();
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{Program.GIT_FETCH_ALL}");
            shell.AddScript($"{GIT_GET_ALL_REMOTE_BRANCHES_WITH_UNMERGED}");
        }

        private void CreatePullRequests(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            throw new NotImplementedException(
                "Creating Pull Request is undesired at the moment, so the method to do so, has not been fully implemented.");
            var toPull = passAround[PassAroundContent.UNSAFE_DELETE];
            using var shell = PowerShell.Create();
        }

        private void DeleteBranches(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            var toDelete = passAround[PassAroundContent.SAFE_DELETE].Select(raw =>
            {
                string refName = raw.ImmediateBaseObject.ToString().Split('|')[0];
                string name = refName[20..];
                return name;
            });
            using var shell = PowerShell.Create();

            Console.WriteLine("");
            BuildDeleteBranchCommand(shell, area, toDelete);
            var result = RunPSScript(shell, false);

            foreach (string deleted in toDelete)
                WritePowershellLines(result, area, $"Deleted: {deleted}", false);
        }

        private void ProcessBranches(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            try
            {
                if (shouldAllowDelete) DeleteBranches(passAround, area);
                if (shouldCreatePullRequests) CreatePullRequests(passAround, area);
            }
            catch (NotImplementedException nie)
            {
                Console.WriteLine(nie);
            }
        }

        private void WriteALlLines(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            WritePowershellLines(passAround[PassAroundContent.ALL], area, $"All branches for {area.Key}.");
            WritePowershellLines(passAround[PassAroundContent.ALL_DELETE], area,
                $"All branches for {area.Key}, that is marked for deletion.");
            WritePowershellLines(passAround[PassAroundContent.ALL_UNMERGED], area,
                $"All branches for {area.Key}, with unmerged changes.");
            WritePowershellLines(passAround[PassAroundContent.UNSAFE_DELETE], area,
                $"All branches for {area.Key}, that is marked for deletion and has unmerged changes.");
            WritePowershellLines(passAround[PassAroundContent.SAFE_DELETE], area,
                $"All branches for {area.Key}, that is marked for deletion and has no unmerged changes.");
        }

        private static void WritePercentages(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            double totalPercentageRemoved =
                Math.Round(
                    (double) passAround[PassAroundContent.ALL_DELETE].Count() /
                    passAround[PassAroundContent.ALL].Count() * 100, 3);
            Console.WriteLine($"Percentage {area.Key} Branches to be Delete: {totalPercentageRemoved}%");
            double unsafePercentageRemoved =
                Math.Round(
                    (double) passAround[PassAroundContent.UNSAFE_DELETE].Count() /
                    passAround[PassAroundContent.ALL].Count() * 100, 3);
            Console.WriteLine($"Percentage {area.Key} unmerged Branches to be Delete: {unsafePercentageRemoved}%");
            double safePercentageRemoved =
                Math.Round(
                    (double) passAround[PassAroundContent.SAFE_DELETE].Count() /
                    passAround[PassAroundContent.ALL].Count() * 100, 3);
            Console.WriteLine($"Percentage {area.Key} merged Branches to be Delete: {safePercentageRemoved}%");
        }

        private void WriteTotals(
            KeyValuePair<Area, string> area, Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround)
        {
            using var shell = PowerShell.Create();

            BuildGetBranchesCommand(shell, area);
            passAround.Add(PassAroundContent.ALL, RunPSScript(shell));
            Console.WriteLine($"Total {area.Key} Branches Count: {passAround[PassAroundContent.ALL].Count()}");

            passAround.Add(PassAroundContent.ALL_DELETE,
                FindMatchingPowershellLines(passAround[PassAroundContent.ALL],
                    branchPatterns.Where(x => x.Area == area.Key).ToList()));
            Console.WriteLine(
                $"Total {area.Key} Branches to be deleted Count: {passAround[PassAroundContent.ALL_DELETE].Count()}");

            BuildGetUnmergedBranchesCommand(shell, area);
            passAround.Add(PassAroundContent.ALL_UNMERGED, RunPSScript(shell));
            Console.WriteLine(
                $"Total {area.Key} Branches with Unmerged changes Count: {passAround[PassAroundContent.ALL_UNMERGED].Count()}");

            passAround.Add(PassAroundContent.UNSAFE_DELETE,
                passAround[PassAroundContent.ALL_DELETE].Where(all =>
                    passAround[PassAroundContent.ALL_UNMERGED].Any(unmerged =>
                        all.ImmediateBaseObject.ToString()!
                            .Contains(unmerged.ImmediateBaseObject.ToString()!.TrimStart()))).ToList());
            Console.WriteLine(
                $"Total {area.Key} Branches to be deleted with unmerged changes Count: {passAround[PassAroundContent.UNSAFE_DELETE].Count()}");

            passAround.Add(PassAroundContent.SAFE_DELETE,
                passAround[PassAroundContent.ALL_DELETE].Except(passAround[PassAroundContent.UNSAFE_DELETE]).ToList());
            Console.WriteLine(
                $"Total {area.Key} Branches to be deleted with no unmerged changes Count: {passAround[PassAroundContent.SAFE_DELETE].Count()}");
        }

        #region Nested type: PassAroundContent

        private enum PassAroundContent
        {
            ALL,
            ALL_DELETE,
            ALL_UNMERGED,
            SAFE_DELETE,
            UNSAFE_DELETE,
        }

        #endregion
    }
}