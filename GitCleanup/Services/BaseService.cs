using GitCleanup.Enums;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace GitCleanup.Services
{
    public abstract class BaseService
    {
        protected void WritePowershellLines(IEnumerable<PSObject> tags, KeyValuePair<Area, string> area)
        {
            foreach (PSObject tag in tags)
                Console.WriteLine($"{area.Key}: {tag.ImmediateBaseObject}");
        }

        protected Collection<PSObject> RunPSScript(PowerShell shell)
        {
            return shell.Invoke();
        }

        protected IList<PSObject> FindMatchingPowershellLines(
            Collection<PSObject> branches, List<(Area Area, Regex Pattern)> specifiedPatterns)
        {
            var results = new List<PSObject>();
            var enumeratedTags = branches.ToArray();
            foreach ((Area area, Regex? regex) in specifiedPatterns)
                results.AddRange(enumeratedTags.Where(x => regex.IsMatch(x.ImmediateBaseObject.ToString())).ToList());
            return results.Distinct().ToList();
        }
    }
}