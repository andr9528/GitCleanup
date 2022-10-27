using GitCleanup.Enums;
using GitCleanup.Services;

namespace GitCleanup
{
    internal class Program
    {
        public const string GIT_FETCH_ALL = @"git fetch --all";

        private const bool SHOULD_ALLOW_DELETE = false;
        private const bool SHOULD_CREATE_PULL_REQUEST = false;

        private readonly Dictionary<Area, string> areas = new()
        {
            {Area.CORE, @"C:\Code\Tv2\tv-automation-server-core"},
            {Area.GATEWAY_INEWS, @"C:\Code\Tv2\inews-ftp-gateway"},
            {Area.BLUEPRINTS, @"C:\Code\Tv2\sofie-blueprints-inews"},
            {Area.TSR, @"C:\Code\Tv2\tv-automation-state-timeline-resolver"},
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