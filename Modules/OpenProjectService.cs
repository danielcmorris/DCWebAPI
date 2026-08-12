using System.Net.Http.Headers;
using System.Text;
using DCElectricWebAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DCElectricWebAPI.Modules
{
    // Proxies the OpenProject REST API (v3) so the frontend never holds the
    // OpenProject API key. Work packages come back HAL-style under
    // _embedded.elements; linked values (status, type, parent) live in _links.
    public class OpenProjectService
    {
        private readonly OpenProjectSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public OpenProjectService(IOptions<OpenProjectSettings> options, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"apikey:{_settings.apiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            return client;
        }

        // status: "o" = open, "c" = closed (OpenProject status filter operators)
        public async Task<List<SupportTicket>> GetTickets(string statusOperator)
        {
            var tickets = new List<SupportTicket>();
            int pageSize = 100;
            int page = 1;
            int total;

            var client = CreateClient();

            do
            {
                var filters = Uri.EscapeDataString($"[{{\"status\":{{\"operator\":\"{statusOperator}\",\"values\":[]}}}}]");
                var sortBy = Uri.EscapeDataString("[[\"updatedAt\",\"desc\"]]");
                var url = $"{_settings.baseUrl}/api/v3/projects/{_settings.projectId}/work_packages" +
                          $"?filters={filters}&sortBy={sortBy}&pageSize={pageSize}&offset={page}";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                total = (int)(json["total"] ?? 0);

                var elements = json["_embedded"]?["elements"] as JArray ?? new JArray();
                foreach (var e in elements)
                {
                    // Parent link href looks like "/api/v3/work_packages/1138"; null href = no parent.
                    var parentHref = (string)e["_links"]?["parent"]?["href"];
                    int? parentId = null;
                    if (!string.IsNullOrEmpty(parentHref) && int.TryParse(parentHref.Split('/').Last(), out var pid))
                        parentId = pid;

                    tickets.Add(new SupportTicket
                    {
                        ParentId = parentId,
                        ParentSubject = parentId != null ? (string)e["_links"]?["parent"]?["title"] ?? "" : "",
                        Id = (int)e["id"],
                        Subject = (string)e["subject"] ?? "",
                        Type = (string)e["_links"]?["type"]?["title"] ?? "",
                        Status = (string)e["_links"]?["status"]?["title"] ?? "",
                        CreatedAt = (DateTime)e["createdAt"],
                        UpdatedAt = (DateTime)e["updatedAt"],
                        Url = $"{_settings.baseUrl}/work_packages/{(int)e["id"]}"
                    });
                }

                page++;
            } while (tickets.Count < total && tickets.Count < _settings.maxTickets);

            await FillCommentCounts(client, tickets);

            return tickets;
        }

        // Comment counts require one activities call per ticket, so fan out with
        // bounded concurrency and cache per ticket. The cache key includes
        // updatedAt: adding a comment touches the work package, which changes the
        // key and forces a fresh count; untouched tickets hit the cache.
        private async Task FillCommentCounts(HttpClient client, List<SupportTicket> tickets)
        {
            var throttle = new SemaphoreSlim(8);
            var tasks = tickets.Select(async t =>
            {
                var cacheKey = $"op-comments-{t.Id}-{t.UpdatedAt.Ticks}";
                if (_cache.TryGetValue(cacheKey, out int cached))
                {
                    t.CommentCount = cached;
                    return;
                }

                await throttle.WaitAsync();
                try
                {
                    var response = await client.GetAsync($"{_settings.baseUrl}/api/v3/work_packages/{t.Id}/activities");
                    response.EnsureSuccessStatusCode();
                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var elements = json["_embedded"]?["elements"] as JArray ?? new JArray();
                    t.CommentCount = elements.Count(a => (string)a["_type"] == "Activity::Comment");
                    _cache.Set(cacheKey, t.CommentCount, TimeSpan.FromHours(4));
                }
                catch
                {
                    // A failed count shouldn't break the ticket list; show 0 and retry next load.
                    t.CommentCount = 0;
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
