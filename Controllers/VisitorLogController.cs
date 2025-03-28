using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using aacs.Models;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace aacs.Controllers
{
    public class VisitorLogController : Controller
    {
        private readonly IMongoCollection<VisitorsLog> _visitorCollection;

        public VisitorLogController(IMongoDatabase database)
        {
            _visitorCollection = database.GetCollection<VisitorsLog>("VisitorsLog");
        }

        public Task<IActionResult> Index()
        {
            return Task.FromResult<IActionResult>(RedirectToAction("VisitorLogsManagement"));
        }

        public async Task<IActionResult> VisitorLogsManagement(int page = 1)
        {
            const int pageSize = 10;
            var allLogs = await _visitorCollection.Find(_ => true)
                                .SortByDescending(v => v.VisitDate)
                                .ToListAsync();
            // Group by IP to show only one record per IP
            var aggregatedLogs = allLogs.GroupBy(x => x.IpAddress)
                .Select(g => new AggregatedVisitorLog {
                    IpAddress = g.Key,
                    LastVisitDate = g.Max(x => x.VisitDate),
                    Country = g.First().Country,
                    Browser = g.First().Browser,
                    Blocked = g.Any(x => x.Blocked),
                    VisitCount = g.Count(),
                    UserType = g.First().UserType, // Added new property initializer
                    Sessions = g.ToList()
                })
                .OrderByDescending(a => a.LastVisitDate)
                .ToList();

            // Apply pagination on aggregated logs using Count() extension
            var pagedLogs = aggregatedLogs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            int totalPages = (int)Math.Ceiling(aggregatedLogs.Count() / (double)pageSize);
            var model = new PaginatedList<AggregatedVisitorLog>(pagedLogs, aggregatedLogs.Count(), page, pageSize);
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            return View("~/Views/Admin/VisitorLogsManagement.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Block(string id)
        {
            var visitor = await _visitorCollection.Find(v => v.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
            if (visitor != null)
            {
                var filter = Builders<VisitorsLog>.Filter.Eq(v => v.IpAddress, visitor.IpAddress);
                var update = Builders<VisitorsLog>.Update
                    .Set(v => v.Blocked, true)
                    .Set(v => v.UserType, "Flagged"); // Set UserType to "" or "Flagged"
                await _visitorCollection.UpdateManyAsync(filter, update);
            }
            return RedirectToAction("VisitorLogsManagement");
        }

        [HttpPost]
        public async Task<IActionResult> Unblock(string id)
        {
            var visitor = await _visitorCollection.Find(v => v.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
            if (visitor != null)
            {
                var filter = Builders<VisitorsLog>.Filter.Eq(v => v.IpAddress, visitor.IpAddress);
                var update = Builders<VisitorsLog>.Update
                    .Set(v => v.Blocked, false)
                    .Set(v => v.UserType, "Visitor"); // Revert UserType to "Visitor"
                await _visitorCollection.UpdateManyAsync(filter, update);
            }
            return RedirectToAction("VisitorLogsManagement");
        }

        [HttpGet]
        public async Task<IActionResult> Details(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return BadRequest("IP is required.");
            }
            var logs = await _visitorCollection.Find(v => v.IpAddress == ip)
                                               .SortByDescending(v => v.VisitDate)
                                               .ToListAsync();
            if (logs == null || !logs.Any())
            {
                return NotFound("Visitor logs not found for this IP.");
            }
            // Aggregate logs for this IP
            var aggregated = new AggregatedVisitorLog
            {
                IpAddress = ip,
                LastVisitDate = logs.Max(x => x.VisitDate),
                Country = logs.First().Country,
                Browser = logs.First().Browser,
                Blocked = logs.Any(x => x.Blocked),
                VisitCount = logs.Count,
                Sessions = logs
            };
            return View("~/Views/Admin/VisitorDetails.cshtml", aggregated);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVisitor(string id)
        {
            // Delete all session history for the visitor's IP
            var visitor = await _visitorCollection.Find(v => v.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
            if(visitor == null)
            {
                TempData["ErrorMessage"] = "Visitor not found.";
                return RedirectToAction("VisitorLogsManagement");
            }
            var filter = Builders<VisitorsLog>.Filter.Eq(v => v.IpAddress, visitor.IpAddress);
            await _visitorCollection.DeleteManyAsync(filter);
            TempData["SuccessMessage"] = "Visitor history deleted successfully.";
            return RedirectToAction("VisitorLogsManagement");
        }
    }
}
