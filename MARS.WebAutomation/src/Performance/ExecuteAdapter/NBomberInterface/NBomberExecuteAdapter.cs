using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MARS.WebAutomation.Models;
using MARS.WebAutomation.Services;
using NBomber.Contracts;
using NBomber.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface
{
    public sealed class NBomberExecuteAdapter : INBomberExecuteAdapter
    {
        public NBomberExecutionPlan BuildExecutionPlan(
            IReadOnlyCollection<PerformanceRequestRecord> requests,
            int simulatedUsers,
            TimeSpan duration)
        {
            var plan = new NBomberExecutionPlan
            {
                SimulatedUsers = Math.Max(1, simulatedUsers),
                Duration = duration <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : duration
            };

            if (requests == null || requests.Count == 0)
            {
                return plan;
            }

            foreach (var group in requests
                         .Where(r => r != null && !r.IsFiltered && !string.Equals(r.Action, "Ignore", StringComparison.OrdinalIgnoreCase))
                         .GroupBy(r => string.IsNullOrWhiteSpace(r.AnchorGroup) ? "General" : r.AnchorGroup))
            {
                var transaction = new NBomberTransactionPlan
                {
                    Name = SanitizeName(group.Key)
                };

                var index = 1;
                foreach (var req in group)
                {
                    transaction.Steps.Add(new NBomberRequestStep
                    {
                        Name = BuildStepName(req, index++),
                        Method = string.IsNullOrWhiteSpace(req.Method) ? "GET" : req.Method.Trim().ToUpperInvariant(),
                        Url = req.Url ?? string.Empty,
                        Headers = req.Headers,
                        Payload = req.Payload,
                        ExpectedStatusCodes = BuildStatusExpectation(req.Status),
                        // Unlink means "not linked to current UI step", not "do not execute in performance run".
                        Skip = string.Equals(req.Action, "Ignore", StringComparison.OrdinalIgnoreCase)
                    });
                }

                if (transaction.Steps.Count > 0)
                {
                    plan.Transactions.Add(transaction);
                }
            }

            return plan;
        }

        public async Task<NBomberExecutionResult> ExecuteAsync(
            NBomberExecutionPlan plan,
            Action<NBomberProgressSnapshot> onProgress = null,
            CancellationToken cancellationToken = default)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var startedUtc = DateTime.UtcNow;
            var result = new NBomberExecutionResult
            {
                StartedUtc = startedUtc,
                SimulatedUsers = Math.Max(1, plan.SimulatedUsers)
            };

            var transactions = (plan.Transactions ?? new List<NBomberTransactionPlan>())
                .Where(t => t != null && t.Enabled && !string.IsNullOrWhiteSpace(t.Name) && t.Steps != null && t.Steps.Count > 0)
                .ToList();

            if (transactions.Count == 0)
            {
                result.Success = false;
                result.Message = "No enabled transactions to execute.";
                result.CompletedUtc = DateTime.UtcNow;
                return result;
            }

            var progressState = new ProgressState();
            var scenarios = new List<ScenarioProps>();

            foreach (var transaction in transactions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scenarios.Add(BuildScenario(plan, transaction, progressState, onProgress, cancellationToken));
                result.ExecutedTransactions.Add(transaction.Name);
            }

            var runTask = Task.Run(() =>
            {
                var context = NBomberRunner
                    .RegisterScenarios(scenarios.ToArray());

                context = NBomberRunner.WithTestSuite(context, string.IsNullOrWhiteSpace(plan.TestSuite) ? "MARS.WebAutomation" : plan.TestSuite);
                context = NBomberRunner.WithTestName(context, string.IsNullOrWhiteSpace(plan.TestName) ? "RecordedPerformanceRun" : plan.TestName);

                if (plan.WithoutReports)
                {
                    context = NBomberRunner.WithoutReports(context);
                }
                else if (!string.IsNullOrWhiteSpace(plan.ReportFolder))
                {
                    context = NBomberRunner.WithReportFolder(context, plan.ReportFolder);
                }

                NBomberRunner.Run(context);
            }, cancellationToken);

            await runTask.ConfigureAwait(false);

            result.TotalOk = progressState.TotalOk;
            result.TotalFail = progressState.TotalFail;
            result.Success = progressState.TotalFail == 0;
            result.Message = result.Success ? "NBomber completed successfully." : "NBomber completed with failed steps.";
            result.CompletedUtc = DateTime.UtcNow;

            onProgress?.Invoke(new NBomberProgressSnapshot
            {
                TimestampUtc = DateTime.UtcNow,
                Stage = "completed",
                TotalStarted = progressState.TotalStarted,
                TotalOk = result.TotalOk,
                TotalFail = result.TotalFail,
                SimulatedUsers = result.SimulatedUsers,
                Detail = result.Message
            });

            return result;
        }

        private static ScenarioProps BuildScenario(
            NBomberExecutionPlan plan,
            NBomberTransactionPlan transaction,
            ProgressState state,
            Action<NBomberProgressSnapshot> onProgress,
            CancellationToken cancellationToken)
        {
            var scenarioName = SanitizeName(transaction.Name);

            var scenario = Scenario.Create(
                scenarioName,
                async context =>
                {
                    foreach (var step in transaction.Steps.Where(s => s != null && !s.Skip))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var response = await ExecuteStepAsync(plan, transaction, step, context, state, onProgress, cancellationToken).ConfigureAwait(false);
                        if (!response.IsError)
                        {
                            continue;
                        }

                        return response;
                    }

                    return Response.Ok();
                });

            var users = transaction.SimulatedUsersOverride.GetValueOrDefault(Math.Max(1, plan.SimulatedUsers));
            var duration = transaction.DurationOverride.GetValueOrDefault(plan.Duration <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : plan.Duration);
            if (duration <= TimeSpan.Zero)
            {
                duration = TimeSpan.FromMinutes(1);
            }

            var ramp = TimeSpan.FromSeconds(Math.Max(0, plan.RampUpSeconds));

            var loads = new List<LoadSimulation>();
            if (ramp > TimeSpan.Zero)
            {
                loads.Add(Simulation.RampingConstant(users, ramp));
            }

            loads.Add(Simulation.KeepConstant(users, duration));
            scenario = Scenario.WithLoadSimulations(scenario, loads.ToArray());
            // NBomber 4: WithWarmUpDuration(TimeSpan.Zero) schedules System.Timers.Timer(0) and throws ArgumentException.
            scenario = Scenario.WithRestartIterationOnFail(scenario, false);
            return scenario;
        }

        private static async Task<IResponse> ExecuteStepAsync(
            NBomberExecutionPlan plan,
            NBomberTransactionPlan transaction,
            NBomberRequestStep step,
            IScenarioContext context,
            ProgressState state,
            Action<NBomberProgressSnapshot> onProgress,
            CancellationToken cancellationToken)
        {
            return await Step.Run(
                step.Name ?? "http_step",
                context,
                async () =>
                {
                    using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(Math.Max(1000, step.TimeoutMs)) })
                    {
                        var tel = plan?.Telemetry;
                        tel?.Metrics?.RecordRequestStarted();
                        state.IncrementStarted();
                        var startedUtc = DateTime.UtcNow;
                        var sw = Stopwatch.StartNew();
                        var method = new HttpMethod(string.IsNullOrWhiteSpace(step.Method) ? "GET" : step.Method);
                        var resolvedUrl = ResolveTokens(step.Url ?? string.Empty, plan, context);
                        var request = new HttpRequestMessage(method, resolvedUrl);

                        ApplyHeaders(request, step.Headers, plan, context);
                        AttachPayloadIfAny(request, step, plan, context);

                        HttpResponseMessage httpResponse;
                        string body = string.Empty;
                        try
                        {
                            httpResponse = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                            body = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            tel?.Metrics?.RecordRequestCompleted(sw.Elapsed.TotalMilliseconds, false);
                            tel?.Metrics?.RecordRequestSample(new PerformanceRequestSample
                            {
                                StartedUtc = startedUtc,
                                DurationMs = sw.Elapsed.TotalMilliseconds,
                                Success = false,
                                StatusCode = 0,
                                Method = step.Method ?? "GET",
                                Url = resolvedUrl,
                                Payload = step.Payload ?? string.Empty,
                                ResponseBody = ex.Message ?? string.Empty,
                                Transaction = transaction.Name ?? string.Empty,
                                StepName = step.Name ?? string.Empty
                            });
                            state.IncrementFail();
                            onProgress?.Invoke(new NBomberProgressSnapshot
                            {
                                TimestampUtc = DateTime.UtcNow,
                                Stage = "fail",
                                Transaction = transaction.Name,
                                StepName = step.Name,
                                SimulatedUsers = plan.SimulatedUsers,
                                TotalStarted = state.TotalStarted,
                                TotalOk = state.TotalOk,
                                TotalFail = state.TotalFail,
                                Detail = $"{step.Method} {resolvedUrl} => error: {ex.Message}"
                            });
                            return Response.Fail(statusCode: "0", message: ex.Message);
                        }

                        sw.Stop();
                        var elapsedMs = sw.Elapsed.TotalMilliseconds;
                        var statusCode = (int)httpResponse.StatusCode;
                        var statusOk = IsExpectedStatus(statusCode, step.ExpectedStatusCodes);
                        var bodyNeedle = tel?.ResponseBodyMustContain;
                        var bodyOk = true;
                        if (!string.IsNullOrWhiteSpace(bodyNeedle) && statusCode >= 200 && statusCode <= 299)
                        {
                            bodyOk = (body ?? string.Empty).IndexOf(bodyNeedle.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        var finalOk = statusOk && bodyOk;
                        tel?.Metrics?.RecordRequestCompleted(elapsedMs, finalOk);
                        tel?.Metrics?.RecordRequestSample(new PerformanceRequestSample
                        {
                            StartedUtc = startedUtc,
                            DurationMs = elapsedMs,
                            Success = finalOk,
                            StatusCode = statusCode,
                            Method = step.Method ?? "GET",
                            Url = resolvedUrl,
                            Payload = step.Payload ?? string.Empty,
                            ResponseBody = body ?? string.Empty,
                            Transaction = transaction.Name ?? string.Empty,
                            StepName = step.Name ?? string.Empty
                        });
                        if (tel != null && tel.SaveResponseBodies && !string.IsNullOrWhiteSpace(tel.ResponseLogDirectory))
                        {
                            TryWriteResponseSample(tel.ResponseLogDirectory, step.Name, statusCode, resolvedUrl, body);
                        }

                        var snapshot = new NBomberProgressSnapshot
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Stage = finalOk ? "ok" : "fail",
                            Transaction = transaction.Name,
                            StepName = step.Name,
                            SimulatedUsers = plan.SimulatedUsers,
                            Detail = $"{step.Method} {resolvedUrl} => {statusCode} ({elapsedMs:0} ms)"
                        };

                        if (finalOk)
                        {
                            state.IncrementOk();
                            snapshot.TotalStarted = state.TotalStarted;
                            snapshot.TotalOk = state.TotalOk;
                            snapshot.TotalFail = state.TotalFail;
                            onProgress?.Invoke(snapshot);
                            return Response.Ok(statusCode: statusCode.ToString());
                        }

                        state.IncrementFail();
                        snapshot.TotalStarted = state.TotalStarted;
                        snapshot.TotalOk = state.TotalOk;
                        snapshot.TotalFail = state.TotalFail;
                        if (!statusOk)
                            snapshot.Detail += " (status)";
                        else if (!bodyOk)
                            snapshot.Detail += " (body missing substring)";
                        onProgress?.Invoke(snapshot);
                        return Response.Fail(statusCode: statusCode.ToString(), message: snapshot.Detail);
                    }
                }).ConfigureAwait(false);
        }

        private static string BuildStepName(PerformanceRequestRecord req, int index)
        {
            var method = string.IsNullOrWhiteSpace(req.Method) ? "GET" : req.Method.Trim().ToUpperInvariant();
            var url = req.Url ?? string.Empty;
            return $"{index:D2}_{method}_{SanitizeName(url)}";
        }

        private static string BuildStatusExpectation(int? status)
        {
            if (!status.HasValue || status.Value <= 0)
            {
                return "200-399";
            }

            return status.Value.ToString();
        }

        private static bool IsExpectedStatus(int statusCode, string expected)
        {
            var exp = string.IsNullOrWhiteSpace(expected) ? "200-399" : expected.Trim();
            if (exp.IndexOf('-') > 0)
            {
                var parts = exp.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var min) &&
                    int.TryParse(parts[1], out var max))
                {
                    return statusCode >= min && statusCode <= max;
                }
            }

            if (exp.IndexOf(',') > 0 || exp.IndexOf(';') > 0)
            {
                var tokens = exp.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                return tokens.Any(t => int.TryParse(t.Trim(), out var code) && code == statusCode);
            }

            return int.TryParse(exp, out var oneCode) && oneCode == statusCode;
        }

        private static void ApplyHeaders(
            HttpRequestMessage request,
            string headersJson,
            NBomberExecutionPlan plan,
            IScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return;
            }

            try
            {
                var token = JToken.Parse(headersJson);
                if (!(token is JObject obj))
                {
                    return;
                }

                foreach (var property in obj.Properties())
                {
                    var value = ResolveTokens(property.Value?.ToString(), plan, context);
                    if (string.IsNullOrWhiteSpace(property.Name) || value == null)
                    {
                        continue;
                    }

                    request.Headers.TryAddWithoutValidation(property.Name, value);
                }
            }
            catch (JsonException)
            {
            }
        }

        private static void AttachPayloadIfAny(
            HttpRequestMessage request,
            NBomberRequestStep step,
            NBomberExecutionPlan plan,
            IScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(step.Payload))
            {
                return;
            }

            var method = request.Method.Method ?? string.Empty;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var contentType = string.IsNullOrWhiteSpace(step.ContentType) ? "application/json" : step.ContentType;
            var payload = ResolveTokens(step.Payload, plan, context);
            request.Content = new StringContent(payload ?? string.Empty, Encoding.UTF8, contentType);
        }

        private static string ResolveTokens(string input, NBomberExecutionPlan plan, IScenarioContext context)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = input;
            if (plan?.Variables != null)
            {
                foreach (var pair in plan.Variables)
                {
                    var token = "${" + pair.Key + "}";
                    output = output.Replace(token, pair.Value ?? string.Empty);
                }
            }

            output = output.Replace("${threadId}", context.ScenarioInfo.ThreadId.ToString());
            return output;
        }

        private static void TryWriteResponseSample(string logDirectory, string stepName, int statusCode, string url, string body)
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                var safeStep = SanitizeName(stepName ?? "step");
                var file = Path.Combine(logDirectory, $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{safeStep}_{statusCode}.txt");
                var sb = new StringBuilder();
                sb.AppendLine("url: " + url);
                sb.AppendLine("status: " + statusCode);
                sb.AppendLine("--- body ---");
                sb.AppendLine(body ?? string.Empty);
                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // logging must not break load test
            }
        }

        private static string SanitizeName(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "Item";
            }

            var invalid = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars());
            var sb = new StringBuilder(source.Length);
            foreach (var ch in source)
            {
                if (char.IsWhiteSpace(ch) || invalid.Contains(ch) || ch == '/' || ch == '\\' || ch == ':' || ch == '?')
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private sealed class ProgressState
        {
            private long _totalStarted;
            private long _totalOk;
            private long _totalFail;
            public long TotalStarted => Interlocked.Read(ref _totalStarted);
            public long TotalOk => Interlocked.Read(ref _totalOk);
            public long TotalFail => Interlocked.Read(ref _totalFail);
            public long IncrementStarted() => Interlocked.Increment(ref _totalStarted);
            public long IncrementOk() => Interlocked.Increment(ref _totalOk);
            public long IncrementFail() => Interlocked.Increment(ref _totalFail);
        }
    }
}
