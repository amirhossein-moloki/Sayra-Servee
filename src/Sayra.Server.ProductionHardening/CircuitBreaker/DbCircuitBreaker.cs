namespace Sayra.Server.ProductionHardening.CircuitBreaker;

public enum CircuitState { Closed, Open, HalfOpen }

public class DbCircuitBreaker
{
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private const int FailureThreshold = 5;
    private DateTime _lastFailureTime;
    private readonly TimeSpan _openDuration = TimeSpan.FromSeconds(30);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        EnsureCircuitClosed();

        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        EnsureCircuitClosed();

        try
        {
            await action();
            OnSuccess();
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    private void EnsureCircuitClosed()
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _openDuration)
            {
                _state = CircuitState.HalfOpen;
            }
            else
            {
                throw new Exception("Circuit is OPEN. Database operations suspended.");
            }
        }
    }

    private void OnSuccess()
    {
        if (_state == CircuitState.HalfOpen)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
        }
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= FailureThreshold)
        {
            _state = CircuitState.Open;
        }
    }
}
