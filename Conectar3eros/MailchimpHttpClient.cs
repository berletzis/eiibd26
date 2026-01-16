using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Conectar3eros
{
    public partial class MailchimpHttpClient : Form
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _dataCenter; // e.g., "us1", "us2"
        public MailchimpHttpClient(string apiKey, string dataCenter)
        {
            InitializeComponent();

            _apiKey = apiKey;
            _dataCenter = dataCenter;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri($"https://{_dataCenter}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"anystring:{_apiKey}")));

        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetAudiencesRawAsync();
        }

        public async Task GetAudiencesRawAsync()
        {
            var response = await _httpClient.GetAsync("audiences");
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            // You can parse the JSON response here using Newtonsoft.Json or System.Text.Json
            JObject json = JObject.Parse(responseBody);
            Console.WriteLine(json.ToString());
        }
    }
}
