namespace App.Application.Commanding;

public interface ITimeScheduler
{
    Task ScheduleAsync<T>(TimeSpan delay, CommandEnvelope<T> commandEnvelope, CancellationToken ct)
        where T : ICommand;
}