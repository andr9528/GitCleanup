using System.Management.Automation;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class TagService : BaseService
    {
        private const string GIT_GET_ALL_TAGS =
            "git for-each-ref --sort=committerdate --format '%(refname)|%(creatordate)|%(committerdate)|%(creator)' refs/tags";

        private readonly bool shouldAllowDelete;
        private readonly bool shouldCreatePullRequests;

        private readonly IEnumerable<(Area Area, Regex Pattern)> tagPatterns = new List<(Area Area, Regex Pattern)>();

        public TagService(bool shouldAllowDelete, bool shouldCreatePullRequests)
        {
            this.shouldAllowDelete = shouldAllowDelete;
            this.shouldCreatePullRequests = shouldCreatePullRequests;
        }

        public void WriteTags(Dictionary<Area, string> areas)
        {
            foreach (var area in areas)
            {
                Console.WriteLine($"Running Tags commands for: {area.Key}");

                using var shell = PowerShell.Create();
                BuildGetTagsCommand(shell, area);

                var tags = RunPSScript(shell);
                Console.WriteLine($"Total {area.Key} Tags Count: {tags.Count}");

                var deleteTags = FindMatchingPowershellLines(tags, tagPatterns.Where(x => x.Area == area.Key).ToList());
                Console.WriteLine($"Total {area.Key} Tags Count to Delete: {deleteTags.Count}");

                double percentageRemoved = Math.Round((double) deleteTags.Count / tags.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} Tags to be Delete: {percentageRemoved}%");

                //WritePowershellLines(tags, area, $"All tags for {area.Key}.");
                //WritePowershellLines(deleteTags, area, $"All tags for {area.Key}, that is marked for deletion.");
                Console.WriteLine(
                    "-----------------------------------------------------------------------------------");
            }
        }

        private void BuildGetTagsCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{GIT_GET_ALL_TAGS}");
        }
    }
}