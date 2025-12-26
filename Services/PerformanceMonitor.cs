using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TaskbarGroupTool.Services
{
    public class PerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private static readonly Lazy<PerformanceMonitor> _instance = new Lazy<PerformanceMonitor>(() => new PerformanceMonitor());
        
        public static PerformanceMonitor Instance => _instance.Value;
        
        private PerformanceMonitor()
        {
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
        }
        
        public void StartTimer(string operationName)
        {
            var metric = _metrics.GetOrAdd(operationName, _ => new PerformanceMetric());
            metric.Start();
        }
        
        public void StopTimer(string operationName)
        {
            if (_metrics.TryGetValue(operationName, out var metric))
            {
                metric.Stop();
            }
        }
        
        public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
        {
            StartTimer(operationName);
            try
            {
                return await operation();
            }
            finally
            {
                StopTimer(operationName);
            }
        }
        
        public async Task MeasureAsync(string operationName, Func<Task> operation)
        {
            StartTimer(operationName);
            try
            {
                await operation();
            }
            finally
            {
                StopTimer(operationName);
            }
        }
        
        public void RecordMemoryUsage(string operationName, long bytesUsed)
        {
            var metric = _metrics.GetOrAdd(operationName, _ => new PerformanceMetric());
            metric.RecordMemory(bytesUsed);
        }
        
        public PerformanceReport GetReport()
        {
            return new PerformanceReport
            {
                Metrics = _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetSummary()),
                GeneratedAt = DateTime.UtcNow
            };
        }
        
        public void ResetMetrics()
        {
            _metrics.Clear();
        }
        
        public void LogPerformanceSummary()
        {
            var report = GetReport();
            System.Diagnostics.Debug.WriteLine("=== Performance Summary ===");
            foreach (var metric in report.Metrics)
            {
                System.Diagnostics.Debug.WriteLine($"{metric.Key}: {metric.Value}");
            }
            System.Diagnostics.Debug.WriteLine("==========================");
        }
    }

    public class PerformanceMetric
    {
        private readonly Stopwatch _stopwatch;
        private readonly List<long> _executionTimes;
        private readonly List<long> _memoryUsage;
        private int _callCount;
        
        public PerformanceMetric()
        {
            _stopwatch = new Stopwatch();
            _executionTimes = new List<long>();
            _memoryUsage = new List<long>();
            _callCount = 0;
        }
        
        public void Start()
        {
            _stopwatch.Restart();
        }
        
        public void Stop()
        {
            _stopwatch.Stop();
            lock (_executionTimes)
            {
                _executionTimes.Add(_stopwatch.ElapsedMilliseconds);
                _callCount++;
            }
        }
        
        public void RecordMemory(long bytes)
        {
            lock (_memoryUsage)
            {
                _memoryUsage.Add(bytes);
            }
        }
        
        public PerformanceMetricSummary GetSummary()
        {
            lock (_executionTimes)
            {
                lock (_memoryUsage)
                {
                    return new PerformanceMetricSummary
                    {
                        CallCount = _callCount,
                        TotalTime = _executionTimes.Sum(),
                        AverageTime = _callCount > 0 ? _executionTimes.Average() : 0,
                        MinTime = _executionTimes.Any() ? _executionTimes.Min() : 0,
                        MaxTime = _executionTimes.Any() ? _executionTimes.Max() : 0,
                        CurrentMemory = _memoryUsage.Any() ? _memoryUsage.Last() : 0,
                        PeakMemory = _memoryUsage.Any() ? _memoryUsage.Max() : 0
                    };
                }
            }
        }
    }

    public class PerformanceReport
    {
        public Dictionary<string, PerformanceMetricSummary> Metrics { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class PerformanceMetricSummary
    {
        public int CallCount { get; set; }
        public long TotalTime { get; set; }
        public double AverageTime { get; set; }
        public long MinTime { get; set; }
        public long MaxTime { get; set; }
        public long CurrentMemory { get; set; }
        public long PeakMemory { get; set; }
        
        public override string ToString()
        {
            return $"Calls: {CallCount}, Avg: {AverageTime:F2}ms, Total: {TotalTime}ms, Memory: {CurrentMemory / 1024}KB";
        }
    }
}
