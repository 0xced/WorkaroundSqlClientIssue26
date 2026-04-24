using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Spectre.Console;

namespace efmssql;

public sealed class CancellationEventListener(string cancellationMessage) : EventListener
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private DateTime? _initialTimeStamp;

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft.Data.SqlClient.EventSource")
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        base.OnEventWritten(eventData);

        _initialTimeStamp ??= eventData.TimeStamp;

        var messageIndex = eventData.PayloadNames?.IndexOf("message");
        if (messageIndex >= 0)
        {
            var message = eventData.Payload?[messageIndex.Value] as string;
            var cancel = message?.Contains(cancellationMessage) == true;
            AnsiConsole.WriteLine($"{(cancel ? "🛑" : "💬")} {(eventData.TimeStamp - _initialTimeStamp.Value).TotalMicroseconds:N0} {message}");
            if (cancel)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            throw new InvalidOperationException($"No message matching \"{cancellationMessage}\" was written by the Microsoft.Data.SqlClient event source.");
        }

        _cancellationTokenSource.Dispose();
    }
}