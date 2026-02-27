using BlazePort.Data;
using BlazePort.Models;
using BlazePort.Runtime;
using BlazePort.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazePort.Components.Pages;

public partial class Home
{
    private bool IsAdminMode => AppArgs.Mode == AppMode.Admin;
    private string TargetHost { get; set; } = "localhost"; // The target host to scan
    private bool IsRunning { get; set; } // Check if the scanner is running
    private string? ErrorMessage { get; set; } // The error message to display

    private List<ServiceEndpoint> Ports { get; set; } = new(); // The list of ports to scan
    private List<PortRow> Results { get; } = new(); // The list of results

    private bool CustomPortExpanded { get; set; }
    private string CustomPortText { get; set; } = string.Empty; // The text to display in the custom port input
    private string CustomPortName { get; set; } = string.Empty; // The name to display in the custom port input
    private HashSet<string> CustomPorts { get; } = new(); // The list of custom ports

    private bool IsExporting { get; set; } // PDF dışa aktarma işlemi devam ediyor mu?

    private bool CanExportPdf => Results.Count > 0 && Results.All(r => r.Checked) && !IsRunning && !IsExporting;

    [Inject]
    private PdfExportService PdfExporter { get; set; } = default!;

    protected override void OnInitialized()
    {
        LoadPorts(); // Load the ports
        RebuildResults(); // Rebuild the results
    }

    private void ToggleCustomPortSection() => CustomPortExpanded = !CustomPortExpanded; // Toggle the custom port section

    private void OnCustomPortHeaderKeydown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            CustomPortExpanded = !CustomPortExpanded;
        }
    }

    private void ReloadPorts()
    {
        try
        {
            ErrorMessage = null;
            LoadPorts();
            RebuildResults();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task ExportPdfAsync()
    {
        try
        {
            if (!CanExportPdf)
            {
                return;
            }

            IsExporting = true;
            await InvokeAsync(StateHasChanged);

            var rows = Results
                .Select(r => new PdfExportService.ExportRow(
                    r.Name,
                    r.Port,
                    r.Modes,
                    r.PortStatus?.ToString() ?? string.Empty,
                    r.PingMs,
                    r.Error))
                .ToList();

            var pdfBytes = PdfExporter.BuildResultsPdf(TargetHost ?? string.Empty, rows);
            var base64 = Convert.ToBase64String(pdfBytes);

            var safeHost = string.IsNullOrWhiteSpace(TargetHost)
                ? "host"
                : TargetHost.Replace(" ", "_");
            var fileName = $"blazeport_{safeHost}.pdf";

            await JS.InvokeVoidAsync("blazePortExport.downloadPdf", fileName, base64);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsExporting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void LoadPorts()
    {
        CustomPorts.Clear();
        Ports = PortProvider.GetPorts(AppArgs.Mode).ToList();
    }

    private void RebuildResults()
    {
        Results.Clear();
        foreach (var ep in Ports)
            Results.Add(PortRow.NotChecked(ep.ServiceName, ep.Port, ep.Modes));
    }

    private void AddCustomPort()
    {
        try
        {
            ErrorMessage = null;

            if (!int.TryParse((CustomPortText ?? string.Empty).Trim(), out var port))
            {
                ErrorMessage = "Custom port must be a number.";
                return;
            }

            if (port < 1 || port > 65535)
            {
                ErrorMessage = "Custom port must be between 1 and 65535.";
                return;
            }

            if (Ports.Any(p => p.Port == port))
            {
                ErrorMessage = $"Port {port} is already in the list.";
                return;
            }

            var name = (CustomPortName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = $"Custom-{port}";

            Ports.Add(new ServiceEndpoint(name, port));
            CustomPorts.Add(PortRow.MakeKey(port));

            RebuildResults();

            CustomPortText = string.Empty;
            CustomPortName = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void RemoveCustomPort(string key)
    {
        try
        {
            if (!CustomPorts.Contains(key))
                return;

            var port = PortRow.ParseKey(key);

            Ports.RemoveAll(p => p.Port == port);
            CustomPorts.Remove(key);

            RebuildResults();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task RunAsync()
    {
        ErrorMessage = null;
        RebuildResults();

        var host = (TargetHost ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            ErrorMessage = "Target host is required.";
            return;
        }

        if (Ports.Count == 0)
        {
            ErrorMessage = "No ports configured.";
            return;
        }

        IsRunning = true;

        try
        {
            foreach (var ep in Ports)
            {
                // Three attempts for ping and port
                var pingAttempts = new List<PingResult>(capacity: 3);
                var portAttempts = new List<PortResult>(capacity: 3);

                for (var i = 0; i < 3; i++)
                {
                    // Use 1 second (1000 ms) timeout for each attempt
                    pingAttempts.Add(await Scanner.PingAsync(host, timeoutMs: 1000));
                    portAttempts.Add(await Scanner.CheckAsync(host, ep.Port, timeoutMs: 1000));
                }

                // Ping result: if at least one is successful, accept success
                PingResult aggregatedPing;
                if (pingAttempts.Any(p => p.Ok))
                {
                    var minMs = pingAttempts
                        .Where(p => p.Ok && p.RoundtripTime is not null)
                        .Select(p => p.RoundtripTime!.Value)
                        .DefaultIfEmpty(0)
                        .Min();

                    aggregatedPing = PingResult.Success(minMs);
                }
                else
                {
                    var errorMessage = string.Join("; ",
                        pingAttempts
                            .Select(p => p.Error)
                            .Where(e => !string.IsNullOrWhiteSpace(e)))
                        ?? "Ping failed.";

                    aggregatedPing = PingResult.Fail(errorMessage);
                }

                // Port result: if at least one is open, accept open
                PortResult aggregatedPort;
                if (portAttempts.Any(p => p.Status == PortStatus.Open))
                {
                    var minConnect = portAttempts
                        .Where(p => p.Status == PortStatus.Open && p.ConnectTimeMs is not null)
                        .Select(p => p.ConnectTimeMs!.Value)
                        .DefaultIfEmpty(0)
                        .Min();

                    aggregatedPort = PortResult.Open(minConnect);
                }
                else if (portAttempts.Any(p => p.Status == PortStatus.Timeout))
                {
                    // All attempts have at least one Timeout, show the number
                    var timeoutCount = portAttempts.Count(p => p.Status == PortStatus.Timeout);

                    var otherErrorGroups = portAttempts
                        .Where(p => p.Status != PortStatus.Timeout)
                        .Select(p => p.Error)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .GroupBy(e => e!);

                    var parts = new List<string>();

                    // Timeout (N)
                    parts.Add(timeoutCount == 1 ? "Timeout" : $"Timeout ({timeoutCount})");

                    // Other errors
                    parts.AddRange(otherErrorGroups.Select(g =>
                        g.Count() == 1 ? g.Key : $"{g.Key} ({g.Count()})"));

                    var combinedError = string.Join("; ", parts);
                    aggregatedPort = PortResult.Fail(PortStatus.Timeout, combinedError);
                }
                else
                {
                    var first = portAttempts.First();

                    var errorGroups = portAttempts
                        .Select(p => p.Error)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .GroupBy(e => e!);

                    var parts = errorGroups
                        .Select(g => g.Count() == 1
                            ? g.Key
                            : $"{g.Key} ({g.Count()})")
                        .ToList();

                    var combinedError = parts.Count > 0
                        ? string.Join("; ", parts)
                        : first.Error ?? "Port check failed.";

                    aggregatedPort = PortResult.Fail(first.Status, combinedError);
                }

                var idx = Results.FindIndex(r => r.Port == ep.Port);
                if (idx >= 0)
                    Results[idx] = PortRow.FromResults(ep.ServiceName, ep.Port, ep.Modes, aggregatedPing, aggregatedPort);

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string GetRowDetails(PortRow r)
    {
        if (!r.Checked) return "Not checked";
        if (!string.IsNullOrWhiteSpace(r.Error)) return r.Error!;
        if (r.ConnectTimeMs is not null) return $"Connect: {r.ConnectTimeMs} ms";
        return "-";
    }

    private RenderFragment RenderStatusBadge(bool? ok) => builder =>
    {
        var (symbol, css, title) = ok switch
        {
            null => ("–", "bg-light text-dark border", "Not checked"),
            true => ("✓", "bg-success", "OK"),
            false => ("✗", "bg-danger", "FAIL")
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge {css}");
        builder.AddAttribute(2, "title", title);
        builder.AddContent(3, symbol);
        builder.CloseElement();
    };

    private RenderFragment RenderPortBadge(PortRow row) => builder =>
    {
        var (symbol, css, title) = row.Checked switch
        {
            false => ("–", "bg-light text-dark border", "Not checked"),
            true when row.PortStatus == PortStatus.Open => ("✓", "bg-success", "Open"),
            _ => ("✗", "bg-danger", "Closed")
        };

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge {css}");
        builder.AddAttribute(2, "title", title);
        builder.AddContent(3, symbol);
        builder.CloseElement();
    };

    private string GetModeBadgeClass() => AppArgs.Mode switch
    {
        AppMode.Client => "bg-primary",
        AppMode.Server => "bg-warning text-dark",
        AppMode.Admin => "bg-danger",
        _ => "bg-secondary"
    };

    private sealed record PortRow(
        string Name,
        int Port,
        string Modes,
        bool Checked,
        bool? PingOk,
        long? PingMs,
        PortStatus? PortStatus,
        long? ConnectTimeMs,
        string? Error)
    {
        public string Key => MakeKey(Port); // The key of the port

        public static string MakeKey(int port) => port.ToString(); // Make the key of the port

        public static int ParseKey(string key) => int.Parse(key); // Parse the key of the port

        public static PortRow NotChecked(string name, int port, string modes)
            => new(name, port, modes, false, null, null, null, null, null); // Create a new port row

        public static PortRow FromResults(string name, int port, string modes, PingResult ping, PortResult pr)
            => new(name, port, modes, true, ping.Ok, ping.RoundtripTime, pr.Status, pr.ConnectTimeMs, pr.Error); // Create a new port row from the results
    }
}

