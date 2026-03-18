using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Conectar3eros
{
    public partial class MailchimpHttpClient : Form
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "SET_YOUR_MAILCHIMP_API_KEY_HERE"; // Set via config or environment
        private readonly string _audienceId = "SET_YOUR_AUDIENCE_ID_HERE"; // Set via config or environment
        private readonly string _defaultCsvPath = @"D:\mailchimp_emails.csv";

        public MailchimpHttpClient()
        {
            InitializeComponent();

            _httpClient = new HttpClient();

            var parts = _apiKey?.Split('-');
            if (parts == null || parts.Length < 2)
            {
                MessageBox.Show("API key inválida. Debe contener '-<datacenter>' al final.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var dataCenter = parts[1];

            // BaseAddress correcto para la API de Mailchimp
            _httpClient.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/3.0/");

            // Autenticación básica (usuario cualquiera y la API key como contraseña)
            var byteArray = Encoding.ASCII.GetBytes($"anystring:{_apiKey}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        // Asegúrate de que en el diseñador el botón se llama button1 y su evento Click está enlazado a este método.
        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            var originalText = button1.Text;
            try
            {
                await GetAudiencesRawAsync(_defaultCsvPath);
            }
            finally
            {
                button1.Text = originalText;
                button1.Enabled = true;
            }
        }

        public async Task GetAudiencesRawAsync(string csvPath)
        {
            var allRows = new List<Dictionary<string, string>>();
            var headersSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int count = 100;
            int offset = 0;
            bool moreRecords = true;
            int fetched = 0;
            int totalItemsKnown = -1;

            try
            {
                while (moreRecords)
                {
                    var relativeUri = $"lists/{_audienceId}/members?count={count}&offset={offset}";
                    var fullUri = new Uri(_httpClient.BaseAddress, relativeUri);
                    Console.WriteLine("Requesting: " + fullUri);

                    UpdateButtonProgress(fetched, totalItemsKnown, false);

                    var response = await _httpClient.GetAsync(relativeUri);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error: {(int)response.StatusCode} {response.ReasonPhrase}\n{errorContent}", "Error HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var root = JObject.Parse(jsonResponse);

                    if (root.TryGetValue("total_items", out var totalToken) && totalToken.Type == JTokenType.Integer)
                    {
                        totalItemsKnown = totalToken.Value<int>();
                    }

                    var members = root["members"] as JArray;
                    if (members == null || members.Count == 0)
                    {
                        moreRecords = false;
                        break;
                    }

                    foreach (var memberToken in members.OfType<JObject>())
                    {
                        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var prop in memberToken.Properties())
                        {
                            var name = prop.Name;

                            if (prop.Value == null || prop.Value.Type == JTokenType.Null)
                            {
                                row[name] = "";
                                headersSet.Add(name);
                                continue;
                            }

                            if (prop.Value is JValue jv)
                            {
                                row[name] = jv.ToString();
                                headersSet.Add(name);
                                continue;
                            }

                            if (prop.Value is JObject jo)
                            {
                                foreach (var inner in jo.Properties())
                                {
                                    var innerName = $"{name}.{inner.Name}";
                                    if (inner.Value == null || inner.Value.Type == JTokenType.Null)
                                    {
                                        row[innerName] = "";
                                    }
                                    else if (inner.Value is JValue innerJv)
                                    {
                                        row[innerName] = innerJv.ToString();
                                    }
                                    else
                                    {
                                        row[innerName] = inner.Value.ToString(Formatting.None);
                                    }
                                    headersSet.Add(innerName);
                                }
                                continue;
                            }

                            if (prop.Value is JArray ja)
                            {
                                var items = new List<string>();
                                foreach (var item in ja)
                                {
                                    if (item is JValue itemVal)
                                        items.Add(itemVal.ToString());
                                    else
                                        items.Add(item.ToString(Formatting.None));
                                }
                                row[name] = string.Join(";", items);
                                headersSet.Add(name);
                                continue;
                            }

                            row[name] = prop.Value.ToString(Formatting.None);
                            headersSet.Add(name);
                        }

                        if (!row.ContainsKey("email_address"))
                        {
                            row["email_address"] = "";
                            headersSet.Add("email_address");
                        }

                        allRows.Add(row);
                        fetched++;
                    }

                    UpdateButtonProgress(fetched, totalItemsKnown, false);

                    // Línea corregida: no usar interpolación anidada con comillas escapadas
                    if (totalItemsKnown > 0)
                        Console.WriteLine("Progreso recuperado: " + fetched + "/" + totalItemsKnown);
                    else
                        Console.WriteLine("Progreso recuperado: " + fetched);

                    if (members.Count < count)
                        moreRecords = false;
                    else
                        offset += count;
                }

                var headers = headersSet.ToList();
                headers.Sort(StringComparer.OrdinalIgnoreCase);
                if (headers.Contains("email_address"))
                {
                    headers.Remove("email_address");
                    headers.Insert(0, "email_address");
                }

                try
                {
                    var dir = Path.GetDirectoryName(csvPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var sb = new StringBuilder();
                    sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsv(h))));

                    foreach (var row in allRows)
                    {
                        var values = new List<string>(headers.Count);
                        foreach (var h in headers)
                        {
                            row.TryGetValue(h, out var v);
                            values.Add(EscapeCsv(v ?? ""));
                        }
                        sb.AppendLine(string.Join(",", values));
                    }

                    File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Se recuperaron {allRows.Count} registros y se guardaron en:\n{csvPath}", "Exportación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    MessageBox.Show($"Error al guardar CSV:\n{ex.Message}", "Error al guardar CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show("Error en la petición HTTP: " + ex.Message, "Error HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateButtonProgress(fetched, totalItemsKnown, true);
            }
        }

        private string EscapeCsv(string value)
        {
            if (value == null) return "\"\"";
            var safe = value.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }

        private void UpdateButtonProgress(int currentCount, int totalItems, bool finished = false)
        {
            if (button1.InvokeRequired)
            {
                button1.Invoke((Action)(() => UpdateButtonProgress(currentCount, totalItems, finished)));
                return;
            }

            if (finished)
            {
                button1.Text = $"Listo ({currentCount})";
                return;
            }

            if (totalItems > 0)
            {
                var percent = Math.Min(100, (totalItems == 0) ? 0 : (currentCount * 100) / totalItems);
                button1.Text = $"Obteniendo... {currentCount}/{totalItems} ({percent}%)";
            }
            else
            {
                button1.Text = $"Obteniendo... {currentCount}";
            }
        }
    }
}