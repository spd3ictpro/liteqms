using LiteQMS.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace LiteQMS.Services;

public class QueueStateService
{
    private readonly IHubContext<QueueHub> _hubContext;
    private readonly ILogger<QueueStateService> _logger;
    private readonly ConcurrentQueue<CallState> _pendingQueue = new();
    private readonly object _lock = new();
    private CallState? _currentState;
    private System.Timers.Timer? _displayTimer;
    private const int DisplayDurationMs = 8000;

    public CallState? CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _currentState;
            }
        }
    }

    public QueueStateService(IHubContext<QueueHub> hubContext, ILogger<QueueStateService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public void UpdateState(CallState state)
    {
        lock (_lock)
        {
            _currentState = state;
        }
    }

    public async Task BroadcastStateAsync(CallState state)
    {
        bool broadcast = false;

        lock (_lock)
        {
            if (_displayTimer != null)
            {
                if (_currentState != null && state.PatientNumber == _currentState.PatientNumber)
                    return;

                _pendingQueue.Enqueue(state);
                return;
            }

            _currentState = state;
            broadcast = true;
            StartTimerLocked();
        }

        if (broadcast)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("NewCall", state);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR broadcast failed in BroadcastStateAsync for patient {PatientNumber}", state.PatientNumber);
            }
        }
    }

    private void StartTimerLocked()
    {
        _displayTimer?.Dispose();
        _displayTimer = new System.Timers.Timer(DisplayDurationMs) { AutoReset = false };
        _displayTimer.Elapsed += OnTimerElapsed;
        _displayTimer.Start();
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        CallState? next = null;

        lock (_lock)
        {
            if (_pendingQueue.TryDequeue(out var dequeued))
            {
                next = dequeued;
                _currentState = next;
                StartTimerLocked();
            }
            else
            {
                _displayTimer?.Dispose();
                _displayTimer = null;
            }
        }

        if (next != null)
        {
            try
            {
                _hubContext.Clients.All.SendAsync("NewCall", next);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR broadcast failed in OnTimerElapsed for patient {PatientNumber}", next.PatientNumber);
            }
        }
    }

    public async Task ResetStateAsync()
    {
        lock (_lock)
        {
            while (_pendingQueue.TryDequeue(out _)) { }

            _displayTimer?.Stop();
            _displayTimer?.Dispose();
            _displayTimer = null;
            _currentState = null;
        }

        await _hubContext.Clients.All.SendAsync("QueueReset");
    }
}
