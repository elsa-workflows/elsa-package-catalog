using System.Collections.Concurrent;

namespace Elsa.Catalog.Core.Sync;

public sealed class SyncConcurrencyGuard
{
    private readonly ConcurrentDictionary<string, byte> _runningScopes = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> TryRunAsync(string scope, Func<Task> action)
    {
        if (!_runningScopes.TryAdd(scope, 0))
            return false;

        try
        {
            await action();
            return true;
        }
        finally
        {
            _runningScopes.TryRemove(scope, out _);
        }
    }
}
