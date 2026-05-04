using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;

namespace MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface
{
    public interface INBomberExecuteAdapter
    {
        NBomberExecutionPlan BuildExecutionPlan(
            IReadOnlyCollection<PerformanceRequestRecord> requests,
            int simulatedUsers,
            TimeSpan duration);

        Task<NBomberExecutionResult> ExecuteAsync(
            NBomberExecutionPlan plan,
            Action<NBomberProgressSnapshot> onProgress = null,
            CancellationToken cancellationToken = default);
    }
}
