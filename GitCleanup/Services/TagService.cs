using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class TagService : BaseService
    {
        private const string GIT_GET_ALL_TAGS =
            "git for-each-ref --sort=committerdate --format '%(refname)|%(creatordate:iso8601)|%(creator)' refs/tags";

        private const string GIT_DELETE_TAGS_BASE = @"git push origin ";

        private readonly bool shouldAllowDelete;

        private readonly IEnumerable<(Area Area, Regex Pattern)> tagPatterns = new List<(Area Area, Regex Pattern)>
        {
            (Area.BLUEPRINTS, new Regex(@"v\d{8}")),
            (Area.GATEWAY_INEWS, new Regex(@"v\d{8}")),
            (Area.CORE, new Regex(@"v\d{8}")),
            (Area.CORE, new Regex(@"dist_blueprints_integration\d{8}_\d")),
            (Area.CORE, new Regex(@"v\d\.\d\d?\.\d_\d{8}_\d")),
            (Area.TSR, new Regex(@"dist-test\/")),
            (Area.TSR, new Regex(@"dist_types\d{8}")),
            (Area.TSR, new Regex(@"dist\d{8}")),
            (Area.TSR, new Regex(@"v\d{8}")),
        };

        public TagService(bool shouldAllowDelete)
        {
            this.shouldAllowDelete = shouldAllowDelete;
        }

        public void WriteTags(Dictionary<Area, string> areas)
        {
            foreach (var area in areas)
            {
                Console.WriteLine($"Running Tags commands for: {area.Key}");
                var passAround = new Dictionary<PassAroundContent, IEnumerable<PSObject>>();

                WriteTotals(area, passAround);
                WritePercentages(passAround, area);
                WriteAllLines(passAround, area);
                ProcessBranches(passAround, area);
                Console.WriteLine(
                    "-----------------------------------------------------------------------------------");
            }
        }

        private void BuildDeleteTagsCommand(PowerShell shell, KeyValuePair<Area, string> area, IEnumerable<string> tags)
        {
            var builder = new StringBuilder(GIT_DELETE_TAGS_BASE);
            foreach (string name in tags)
                builder.Append($":{name} ");

            shell.Commands.Clear();
            shell.AddScript($"cd {area.Value}");
            shell.AddScript(builder.ToString());
        }

        private void BuildGetTagsCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{Program.GIT_FETCH_ALL}");
            shell.AddScript($"{GIT_GET_ALL_TAGS}");
        }

        private void DeleteTags(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            var toDelete = passAround[PassAroundContent.ALL_DELETE]
                .Select(raw => raw.ImmediateBaseObject.ToString().Split('|')[0]);
            using var shell = PowerShell.Create();

            Console.WriteLine("");
            BuildDeleteTagsCommand(shell, area, toDelete);

            var result = RunPSScript(shell, false);
            foreach (string deleted in toDelete)
                WritePowershellLines(result, area, $"Deleted: {deleted[10..]}", false);
        }

        private void ProcessBranches(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            if (shouldAllowDelete) DeleteTags(passAround, area);
        }

        private void WriteAllLines(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            WritePowershellLines(passAround[PassAroundContent.ALL], area, $"All tags for {area.Key}.");
            WritePowershellLines(passAround[PassAroundContent.ALL_DELETE], area,
                $"All tags for {area.Key}, that is marked for deletion.");
        }

        private static void WritePercentages(
            Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround, KeyValuePair<Area, string> area)
        {
            double percentageRemoved =
                Math.Round(
                    (double) passAround[PassAroundContent.ALL_DELETE].Count() /
                    passAround[PassAroundContent.ALL].Count() * 100, 3);
            Console.WriteLine($"Percentage {area.Key} Tags to be Delete: {percentageRemoved}%");
        }

        private void WriteTotals(
            KeyValuePair<Area, string> area, Dictionary<PassAroundContent, IEnumerable<PSObject>> passAround)
        {
            using var shell = PowerShell.Create();
            BuildGetTagsCommand(shell, area);

            passAround.Add(PassAroundContent.ALL, RunPSScript(shell));
            Console.WriteLine($"Total {area.Key} Tags Count: {passAround[PassAroundContent.ALL].Count()}");

            passAround.Add(PassAroundContent.ALL_DELETE,
                FindMatchingPowershellLines(passAround[PassAroundContent.ALL],
                    tagPatterns.Where(x => x.Area == area.Key).ToList()));
            Console.WriteLine(
                $"Total {area.Key} Tags Count to Delete: {passAround[PassAroundContent.ALL_DELETE].Count()}");
        }

        #region Nested type: PassAroundContent

        private enum PassAroundContent
        {
            ALL,
            ALL_DELETE,
        }

        #endregion
    }
}