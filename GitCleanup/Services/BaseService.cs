using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.RegularExpressions;
using GitCleanup.Enums;

namespace GitCleanup.Services
{
    public abstract class BaseService
    {
        protected IList<PSObject> FindMatchingPowershellLines(
            IEnumerable<PSObject> basePsObjects, List<(Area Area, Regex Pattern)> specifiedPatterns)
        {
            var results = new List<PSObject>();
            var enumerated = basePsObjects.ToArray();
            foreach ((Area area, Regex? regex) in specifiedPatterns)
                results.AddRange(enumerated.Where(x => regex.IsMatch(x.ImmediateBaseObject.ToString())).ToList());
            return results.Distinct().ToList();
        }

        protected Collection<PSObject> RunPSScript(PowerShell shell, bool checkErrors = true)
        {
            Console.WriteLine("");
            Console.WriteLine("Running commands in Powershell:");
            foreach (Command command in shell.Commands.Commands)
                Console.WriteLine(command.CommandText);
            Console.WriteLine("");

            var result = shell.Invoke();
            if (checkErrors) CheckForErrors(shell);
            return result;
        }

        protected void WritePowershellLines(
            IEnumerable<PSObject> tags, KeyValuePair<Area, string> area, string description = "",
            bool applySpacing = true)
        {
            if (applySpacing) Console.WriteLine("");
            if (description != string.Empty) Console.WriteLine(description);
            foreach (PSObject tag in tags)
                Console.WriteLine($"{area.Key}: {tag.ImmediateBaseObject.ToString().Trim()}");
        }

        private void CheckForErrors(PowerShell shell)
        {
            if (!shell.HadErrors)
                return;

            var errors = shell.Streams.Error.ReadAll();

            foreach (ErrorRecord error in errors)
                Console.WriteLine($"ERROR: {error}");
        }
    }
}