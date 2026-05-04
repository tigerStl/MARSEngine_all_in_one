using System;
using System.Collections.Generic;
using System.Linq;

namespace MARS.WebAutomation.Services
{
    /// <summary>Thread-safe counters and latency stats for live performance charts.</summary>
    public sealed class PerformanceMetricsCollector
    {
        private readonly object _lock = new object();
        private readonly List<PerformanceRequestSample> _samples = new List<PerformanceRequestSample>();
        private const int MaxSamples = 20000;
        private long _sent;
        private long _returned;
        private long _errors;
        private long _success;
        private long _prevSent;
        private long _prevReturned;
        private long _prevErrors;
        private long _prevSuccess;
        private double _minMs = double.MaxValue;
        private double _maxMs;
        private double _sumMs;
        private long _latencyCount;
        private double _bucketMinMs = double.MaxValue;
        private double _bucketMaxMs;
        private double _bucketSumMs;
        private int _bucketLatCount;

        public void RecordRequestStarted()
        {
            lock (_lock)
            {
                _sent++;
            }
        }

        public void RecordRequestCompleted(double elapsedMs, bool success)
        {
            lock (_lock)
            {
                _returned++;
                if (success)
                    _success++;
                else
                    _errors++;

                if (elapsedMs >= 0 && elapsedMs < 86400000)
                {
                    if (elapsedMs < _minMs)
                        _minMs = elapsedMs;
                    if (elapsedMs > _maxMs)
                        _maxMs = elapsedMs;
                    _sumMs += elapsedMs;
                    _latencyCount++;

                    _bucketMinMs = Math.Min(_bucketMinMs, elapsedMs);
                    _bucketMaxMs = Math.Max(_bucketMaxMs, elapsedMs);
                    _bucketSumMs += elapsedMs;
                    _bucketLatCount++;
                }
            }
        }

        public void RecordRequestSample(PerformanceRequestSample sample)
        {
            if (sample == null)
                return;
            lock (_lock)
            {
                _samples.Add(sample);
                if (_samples.Count > MaxSamples)
                    _samples.RemoveRange(0, _samples.Count - MaxSamples);
            }
        }

        /// <summary>Consumes deltas since last call (for chart buckets).</summary>
        public PerformanceBucketSnapshot ConsumeBucket()
        {
            lock (_lock)
            {
                var dSent = (int)(_sent - _prevSent);
                var dRet = (int)(_returned - _prevReturned);
                var dErr = (int)(_errors - _prevErrors);
                var dOk = (int)(_success - _prevSuccess);
                _prevSent = _sent;
                _prevReturned = _returned;
                _prevErrors = _errors;
                _prevSuccess = _success;

                double iMin = 0, iMax = 0, iAvg = 0;
                var iCount = _bucketLatCount;
                if (_bucketLatCount > 0)
                {
                    iMin = _bucketMinMs;
                    iMax = _bucketMaxMs;
                    iAvg = _bucketSumMs / _bucketLatCount;
                }

                _bucketMinMs = double.MaxValue;
                _bucketMaxMs = 0;
                _bucketSumMs = 0;
                _bucketLatCount = 0;

                return new PerformanceBucketSnapshot
                {
                    CreatedDelta = dSent,
                    ReturnedDelta = dRet,
                    TotalCumulative = (int)Math.Min(int.MaxValue, _returned),
                    ErrorsDelta = dErr,
                    SuccessDelta = dOk,
                    IntervalLatencySampleCount = iCount,
                    IntervalMinLatencyMs = iMin,
                    IntervalMaxLatencyMs = iMax,
                    IntervalAverageLatencyMs = iAvg
                };
            }
        }

        public PerformanceAggregateSnapshot GetAggregateSnapshot()
        {
            lock (_lock)
            {
                var avg = _latencyCount > 0 ? _sumMs / _latencyCount : 0;
                var min = _latencyCount > 0 ? _minMs : 0;
                var max = _latencyCount > 0 ? _maxMs : 0;
                var rate = _returned > 0 ? 100.0 * _success / _returned : 0;
                return new PerformanceAggregateSnapshot
                {
                    MinLatencyMs = min,
                    MaxLatencyMs = max,
                    AverageLatencyMs = avg,
                    SuccessRatePercent = rate,
                    TotalCompleted = _returned,
                    TotalSuccess = _success,
                    TotalErrors = _errors
                };
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _sent = _returned = _errors = _success = 0;
                _prevSent = _prevReturned = _prevErrors = _prevSuccess = 0;
                _minMs = double.MaxValue;
                _maxMs = 0;
                _sumMs = 0;
                _latencyCount = 0;
                _bucketMinMs = double.MaxValue;
                _bucketMaxMs = 0;
                _bucketSumMs = 0;
                _bucketLatCount = 0;
                _samples.Clear();
            }
        }

        public PerformanceReportSnapshot BuildReportSnapshot()
        {
            lock (_lock)
            {
                var agg = GetAggregateSnapshot();
                var samples = _samples.OrderBy(s => s.StartedUtc).ToList();
                return new PerformanceReportSnapshot
                {
                    Aggregate = agg,
                    Samples = samples
                };
            }
        }
    }

    public struct PerformanceBucketSnapshot
    {
        public int CreatedDelta;
        public int ReturnedDelta;
        public int TotalCumulative;
        public int ErrorsDelta;
        public int SuccessDelta;
        /// <summary>Latency samples recorded during this bucket window.</summary>
        public int IntervalLatencySampleCount;
        public double IntervalMinLatencyMs;
        public double IntervalMaxLatencyMs;
        public double IntervalAverageLatencyMs;
    }

    public struct PerformanceAggregateSnapshot
    {
        public double MinLatencyMs;
        public double MaxLatencyMs;
        public double AverageLatencyMs;
        public double SuccessRatePercent;
        public long TotalCompleted;
        public long TotalSuccess;
        public long TotalErrors;
    }

    public sealed class PerformanceRequestSample
    {
        public DateTime StartedUtc { get; set; }
        public double DurationMs { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string Payload { get; set; }
        public string ResponseBody { get; set; }
        public string Transaction { get; set; }
        public string StepName { get; set; }
    }

    public sealed class PerformanceReportSnapshot
    {
        public PerformanceAggregateSnapshot Aggregate { get; set; }
        public List<PerformanceRequestSample> Samples { get; set; } = new List<PerformanceRequestSample>();
    }
}
