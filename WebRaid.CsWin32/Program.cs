namespace WebRaid.CsWin32
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var sync = new SyncRootManager();
            var connect = sync.Connect();

            Console.WriteLine($"Pfad: {sync.Pfad}");

            Console.ReadLine();
        }
    }
}
