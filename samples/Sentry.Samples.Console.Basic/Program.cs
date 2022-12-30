namespace Sentry.Samples.Console.Basic;

public static class Program
{
    public static void Main()
    {
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            o.Debug = true;
        });

        throw new Exception("test");
    }
}
