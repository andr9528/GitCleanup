using GitCleanup.Enums;
using GitCleanup.Services;

namespace GitCleanup
{
    internal class Program
    {
        public const string GIT_FETCH_ALL = @"git fetch --all";

        private const bool SHOULD_ALLOW_DELETE = true;
        private const bool SHOULD_CREATE_PULL_REQUEST = false;

        private readonly Dictionary<Area, string> areas = new()
        {
            {Area.ACTION, @"C:\Code\Personal\GithubActionsTests"},
        };

        private readonly string[] args;

        private Program(string[] args)
        {
            this.args = args;
        }

        private static void Main(string[] args)
        {
            ConsoleAndFileService.Init();
            var program = new Program(args);
            program.Run();
            Console.ReadKey();
        }

        private void Run()
        {
            var tag = new TagService(SHOULD_ALLOW_DELETE);
            var branch = new BranchService(SHOULD_ALLOW_DELETE, SHOULD_CREATE_PULL_REQUEST);

            tag.WriteTags(areas);
            branch.WriteBranches(areas);
        }
    }
}