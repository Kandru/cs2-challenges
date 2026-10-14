using BenchmarkDotNet.Running;

namespace Challenges.Bench
{
    public static class Program
    {
        /// <summary>Runs every benchmark unless the arguments select some with <c>--filter</c>.</summary>
        public static void Main(string[] args)
        {
            string[] effective = Array.Exists(args, a => a.Equals("--filter", StringComparison.OrdinalIgnoreCase)
                || a.Equals("-f", StringComparison.Ordinal))
                ? args
                : [.. args, "--filter", "*"];
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(effective);
        }
    }
}
