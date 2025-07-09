using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using aacs.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.AnalyticsData.v1beta;
using Google.Apis.AnalyticsData.v1beta.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace aacs.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IMongoCollection<Blog> _blogCollection;
        private readonly IMongoCollection<Contact> _contactCollection;
        private readonly IMongoCollection<Service> _servicesCollection;
        private readonly IMongoCollection<AdminLog> _adminLogCollection;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IMongoDatabase database, IConfiguration configuration, ILogger<DashboardController> logger)
        {
            _blogCollection = database.GetCollection<Blog>("Blog");
            _contactCollection = database.GetCollection<Contact>("Contact");
            _servicesCollection = database.GetCollection<Service>("Service");
            _adminLogCollection = database.GetCollection<AdminLog>("AdminLog");
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Fetch recent admin logs
                var adminLogs = await _adminLogCollection.Find(FilterDefinition<AdminLog>.Empty)
                    .SortByDescending(x => x.Timestamp)
                    .Limit(10)
                    .ToListAsync();
                ViewBag.AdminLogs = adminLogs;

                // Fetch Google Analytics total sessions
                var totalSessions = await GetAnalyticsData();
                ViewBag.TotalSessions = totalSessions;

                return View("~/Views/Admin/Dashboard.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data");
                ViewBag.ErrorMessage = $"Error fetching dashboard data: {ex.Message}";
                return View("~/Views/Admin/Dashboard.cshtml");
            }
        }

        private async Task<long> GetAnalyticsData()
        {
            try
            {
                // Validate configuration
                var propertyId = _configuration["GA_PROPERTY_ID"];
                if (string.IsNullOrEmpty(propertyId))
                {
                    _logger.LogError("GA_PROPERTY_ID is missing in configuration.");
                    throw new InvalidOperationException("GA_PROPERTY_ID is not configured.");
                }

                // Construct service account credentials JSON
                var credentialJson = JsonConvert.SerializeObject(new
                {
                    type = _configuration["GA_TYPE"] ?? "service_account",
                    project_id = _configuration["GA_PROJECT_ID"],
                    private_key_id = _configuration["GA_PRIVATE_KEY_ID"],
                    private_key = _configuration["GA_PRIVATE_KEY"]?.Replace("\\n", "\n"),
                    client_email = _configuration["GA_CLIENT_EMAIL"],
                    client_id = _configuration["GA_CLIENT_ID"],
                    auth_uri = _configuration["GA_AUTH_URI"],
                    token_uri = _configuration["GA_TOKEN_URI"],
                    auth_provider_x509_cert_url = _configuration["GA_AUTH_PROVIDER_CERT_URL"],
                    client_x509_cert_url = _configuration["GA_CLIENT_CERT_URL"],
                    universe_domain = _configuration["GA_UNIVERSE_DOMAIN"] ?? "googleapis.com"
                }, Formatting.None);

                _logger.LogInformation("Generated credential JSON: {CredentialJson}", credentialJson);

                // Load service account credentials
                var credential = GoogleCredential.FromJson(credentialJson)
                    .CreateScoped(AnalyticsDataService.Scope.AnalyticsReadonly);

                // Initialize Analytics Data API service
                using var service = new AnalyticsDataService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential
                });

                // Create the RunReport request for total sessions
                var request = new RunReportRequest
                {
                    Metrics = new List<Metric> { new Metric { Name = "sessions" } },
                    DateRanges = new List<DateRange> { new DateRange { StartDate = "2024-01-01", EndDate = "today" } }
                };

                _logger.LogInformation("Sending GA4 API request for property: properties/{PropertyId}", propertyId);

                // Execute the request
                var response = await service.Properties.RunReport(request, $"properties/{propertyId}").ExecuteAsync();

                // Process the response
                long totalSessions = 0;
                if (response.Rows != null && response.Rows.Any())
                {
                    totalSessions = long.Parse(response.Rows[0].MetricValues[0].Value);
                    _logger.LogInformation("Total sessions retrieved: {TotalSessions}", totalSessions);
                }
                else
                {
                    _logger.LogWarning("No data returned from GA4 API for property {PropertyId}.", propertyId);
                }

                return totalSessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Google Analytics data");
                ViewBag.ErrorMessage = $"Error fetching Google Analytics data: {ex.Message}";
                return 0;
            }
        }
    }
}