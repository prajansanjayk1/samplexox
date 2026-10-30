using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Services;

public sealed class ExtensionPolicyDto
{
    public bool IsFocusActive { get; set; }
    public string SessionMode { get; set; } = "Deep Focus";
    public bool IsAdultShieldActive { get; set; } = true;
    public bool BlockShortsOnly { get; set; }
    public List<string> BlockedWebsites { get; set; } = new();
    public List<string> AllowlistedDomains { get; set; } = new();
    public int PolicyVersion { get; set; } = 4;
    public string UpdatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
}

public sealed class ExtensionSyncServer : IDisposable
{
    private const string Prefix = "http://127.0.0.1:41789/";
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private ExtensionPolicyDto _currentPolicy = new();
    private readonly object _lock = new();

    public bool IsRunning => _listener?.IsListening ?? false;
    public DateTime? LastSyncRequestUtc { get; private set; }

    public void Start()
    {
        try
        {
            if (!HttpListener.IsSupported)
            {
                AppLogger.LogWarning("HttpListener is not supported on this platform.");
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add(Prefix);
            _listener.Start();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
            AppLogger.LogInfo($"Extension sync server active on {Prefix}");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Could not start ExtensionSyncServer on {Prefix}: {ex.Message}");
        }
    }

    public void UpdatePolicy(ExtensionPolicyDto policy)
    {
        lock (_lock)
        {
            policy.PolicyVersion = _currentPolicy.PolicyVersion + 1;
            policy.UpdatedUtc = DateTime.UtcNow.ToString("O");
            _currentPolicy = policy;
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error accepting extension sync context", ex);
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var req = context.Request;
            var res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = (int)HttpStatusCode.NoContent;
                res.Close();
                return;
            }

            if (req.Url?.AbsolutePath.Equals("/api/policy", StringComparison.OrdinalIgnoreCase) == true)
            {
                LastSyncRequestUtc = DateTime.UtcNow;
                string json;
                lock (_lock)
                {
                    json = JsonSerializer.Serialize(_currentPolicy, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }

                var bytes = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                res.ContentLength64 = bytes.Length;
                res.StatusCode = (int)HttpStatusCode.OK;
                res.OutputStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
            }

            res.Close();
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Extension sync request failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch { }
    }
}
