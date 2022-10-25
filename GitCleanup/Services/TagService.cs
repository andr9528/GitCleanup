using System.Management.Automation;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public class TagService : BaseService
    {
        private const string GIT_GET_ALL_TAGS =
            "git for-each-ref --sort=creatordate --format '%(refname) %(creatordate)' refs/tags";

        private readonly IEnumerable<(Area Area, Regex Pattern)> tagPatterns = new List<(Area Area, Regex Pattern)>
        {
            (Area.BLUEPRINTS, new Regex(@"v\d{8}_")),
            (Area.GATEWAY_INEWS, new Regex(@"v\d{8}_")),
            (Area.CORE, new Regex(@"v\d{8}_")),
            (Area.CORE, new Regex(@"dist_blueprints_integration\d{8}_\d")),
            (Area.CORE, new Regex(@"v\d\.\d\d?\.\d_\d{8}_\d")),
        };

        private void BuildGetTagsCommand(PowerShell shell, KeyValuePair<Area, string> area)
        {
            shell.AddScript($"cd {area.Value}");
            shell.AddScript($"{GIT_GET_ALL_TAGS}");
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
                //WritePowershellLines(tags, area);

                var deleteTags = FindMatchingPowershellLines(tags, tagPatterns.Where(x => x.Area == area.Key).ToList());
                Console.WriteLine($"Total {area.Key} Tags Count to Delete: {deleteTags.Count}");
                //WritePowershellLines(deleteTags, area);

                double percentageRemoved = Math.Round((double) deleteTags.Count / tags.Count * 100, 3);
                Console.WriteLine($"Percentage {area.Key} Tags to be Delete: {percentageRemoved}%");
            }
        }
    }
}