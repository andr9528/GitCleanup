using GitCleanup.Enums;
using GitCleanup.Services;

namespace GitCleanup
{
    internal class Program
    {
        private readonly string[] args;

        private readonly Dictionary<Area, string> areas = new()
        {
            {Area.CORE, @"C:\Code\Tv2\tv-automation-server-core"},
            {Area.GATEWAY_INEWS, @"C:\Code\Tv2\inews-ftp-gateway"},
            {Area.BLUEPRINTS, @"C:\Code\Tv2\sofie-blueprints-inews"},
            {Area.TSR, @"C:\Code\Tv2\tv-automation-state-timeline-resolver"},
        };

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
            var tag = new TagService();
            var branch = new BranchService();

            tag.WriteTags(areas);
            branch.WriteBranches(areas);
        }
    }
}