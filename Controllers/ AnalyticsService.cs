using Google.Apis.Auth.OAuth2;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Services;
using System.Text;

public class AnalyticsService
{
    private readonly AnalyticsReportingService _analyticsService;

    public AnalyticsService()
    {
        var credential = CreateScopedCredentialFromEnv();
        _analyticsService = new AnalyticsReportingService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AACS Dashboard"
        });
    }

    private GoogleCredential CreateScopedCredentialFromEnv()
    {
        var privateKey = Environment.GetEnvironmentVariable("GA_PRIVATE_KEY")?.Replace("\\n", "\n");

        var jsonObj = new
        {
            type = Environment.GetEnvironmentVariable("GA_TYPE"),
            project_id = Environment.GetEnvironmentVariable("GA_PROJECT_ID"),
            private_key_id = Environment.GetEnvironmentVariable("GA_PRIVATE_KEY_ID"),
            private_key = privateKey,
            client_email = Environment.GetEnvironmentVariable("GA_CLIENT_EMAIL"),
            client_id = Environment.GetEnvironmentVariable("GA_CLIENT_ID"),
            auth_uri = Environment.GetEnvironmentVariable("GA_AUTH_URI"),
            token_uri = Environment.GetEnvironmentVariable("GA_TOKEN_URI"),
            auth_provider_x509_cert_url = Environment.GetEnvironmentVariable("GA_AUTH_PROVIDER_CERT_URL"),
            client_x509_cert_url = Environment.GetEnvironmentVariable("GA_CLIENT_CERT_URL"),
            universe_domain = Environment.GetEnvironmentVariable("GA_UNIVERSE_DOMAIN")
        };

        using var stream = new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(jsonObj));
        return GoogleCredential.FromStream(stream).CreateScoped("https://www.googleapis.com/auth/analytics.readonly");
    }

    public async Task<Dictionary<string, int>> GetUsersByCountryAsync()
    {
        var viewId = Environment.GetEnvironmentVariable("GA_VIEW_ID");
        var request = new ReportRequest
        {
            ViewId = viewId,
            DateRanges = new List<DateRange> {
                new DateRange { StartDate = "7daysAgo", EndDate = "today" }
            },
            Dimensions = new List<Dimension> {
                new Dimension { Name = "ga:country" }
            },
            Metrics = new List<Metric> {
                new Metric { Expression = "ga:users" }
            }
        };

        var getReportsRequest = new GetReportsRequest
        {
            ReportRequests = new List<ReportRequest> { request }
        };

        var response = await _analyticsService.Reports.BatchGet(getReportsRequest).ExecuteAsync();
        var result = new Dictionary<string, int>();

        foreach (var row in response.Reports[0].Data.Rows)
        {
            var country = row.Dimensions[0];
            var users = int.Parse(row.Metrics[0].Values[0]);
            result[country] = users;
        }

        return result;
    }
}