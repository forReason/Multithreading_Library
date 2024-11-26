namespace Multithreading_Library.ThreadControl;

public class RunPeriodicTask
{
    public static async Task Run(Func<Task> task, TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await task();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Task canceled.");
        }
    }
}