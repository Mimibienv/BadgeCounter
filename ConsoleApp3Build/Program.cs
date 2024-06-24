using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ez
{
    class Program
    {
        private static string username;

        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            // Parse command line arguments
            if (args.Length == 2 && args[0] == "-u")
            {
                username = args[1];
            }
            else
            {
                Console.WriteLine("Usage: badgecounter -u <username>");
                return;
            }

            string userId = await GetUserIdByUsername();
            string baseUrl = $"https://badges.roproxy.com/v1/users/{userId}/badges?limit=100&sortOrder=Asc&cursor=";
            string nextPageCursor = null;
            int totalBadges = 0;

            Console.WriteLine($"Getting every badge for the user {username} ({userId})");

            do
            {
                string url = baseUrl + (nextPageCursor ?? string.Empty);
                string json = await FetchJsonAsync(url);

                JObject data = JObject.Parse(json);
                JArray badges = (JArray)data["data"];
                nextPageCursor = (string?)data["nextPageCursor"];

                foreach (var badge in badges)
                {
                    totalBadges++;
                    string badgeName = (string?)badge["name"];
                    string badgeId = (string?)badge["id"];
                    Console.WriteLine($"{UngayifyNumber(totalBadges)} badge is: {badgeName} ({badgeId})");
                }
            }
            while (!string.IsNullOrEmpty(nextPageCursor));

            Console.WriteLine($"Total number of badges: {totalBadges}");
        }

        private static async Task<string> FetchJsonAsync(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task<string> GetUserIdByUsername()
        {
            dynamic searches;
            searches = (HttpResponseMessage)await client.GetAsync($"https://users.roproxy.com/v1/users/search?keyword={username}&limit=10");
            searches.EnsureSuccessStatusCode();
            searches = (string)await searches.Content.ReadAsStringAsync();
            JObject searchesJson = JObject.Parse(searches);
            JArray searchesArray = (JArray)searchesJson["data"];
            return (string)searchesArray[0]["id"];
        }

        private static string UngayifyNumber(int num)
        {
            int lastdigit = num % 10;
            int last2digits = num % 100;

            if (last2digits >= 11 && last2digits <= 13) { return num + "th"; }

            switch (lastdigit)
            {
                case 1: return num + "st";
                case 2: return num + "nd";
                case 3: return num + "rd";
                default: return num + "th";
            }
        }
    }
}