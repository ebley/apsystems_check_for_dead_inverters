using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using static ApSystems.Program;

namespace ApSystems
{
    internal class Work
    {
        public readonly Program.MailSettings _mailSettings;
        public readonly Program.SystemSettings _systemSettings;
        public Work(IOptions<Program.MailSettings> mailSettings, IOptions<Program.SystemSettings> systemSettings)
        {
            _mailSettings = mailSettings.Value;
            _systemSettings = systemSettings.Value;


        }
        public class EnergyData
        {
            public int code { get; set; }
            public EnergyInfo data { get; set; }
        }

        public class EnergyInfo
        {
            public List<string> energy { get; set; }
        }

        public async Task Main(string[] args)
        {

            /* this code is a little lame and hard-coded  but all I needed was this one task done */
            string method = "GET";
            string totalurl = $"/user/api/v2/systems/{_systemSettings.sid}/devices/inverter/batch/energy/{_systemSettings.eid}";
            string formattedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string xCaNonce = Guid.NewGuid().ToString("N");
            string lastPart = Path.GetFileName(totalurl);

            Dictionary<string, string> list = new Dictionary<string, string>();
            
            list.Add("X-CA-Timestamp", formattedDate);
            list.Add("X-CA-Nonce", xCaNonce);
            list.Add("X-CA-AppId", _systemSettings.app);
            list.Add("RequestPath", lastPart);
            list.Add("HTTPMethod", method);
            list.Add("X-CA-Signature-Method", "HmacSHA256");

            string signthis = string.Join("/", list.Values.Cast<string>());
            string signature = GenerateHmacSha256Signature(_systemSettings.s, signthis);

            var queryParams = new Dictionary<string, string>
            {
                { "energy_level", "energy" },
                { "date_range",DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") }
            };
            string queryString = "";
            if (queryParams.Count > 0)
            {
                queryString = "?" + BuildQueryString(queryParams);
            }

            using (HttpClient client = new HttpClient())
            {
                // Define the URI for the GET request
                

                // Add headers to the request
                client.DefaultRequestHeaders.Add("X-CA-AppId", list["X-CA-AppId"]);
                client.DefaultRequestHeaders.Add("X-CA-Timestamp", list["X-CA-Timestamp"]);
                client.DefaultRequestHeaders.Add("X-CA-Nonce", list["X-CA-Nonce"]);
                client.DefaultRequestHeaders.Add("X-CA-Signature-Method", list["X-CA-Signature-Method"]);
                client.DefaultRequestHeaders.Add("X-CA-Signature", signature);

                string largeurl = _systemSettings.apiRoot + totalurl + queryString;
                // Make the GET request
                HttpResponseMessage response = await client.GetAsync(largeurl);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and display the response content
                    string content = await response.Content.ReadAsStringAsync();
                    processJsonContent(content);
                    Console.WriteLine(content);
                }
                else
                {
                    Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                }
            }

            void processJsonContent(string jsonInput)
            {
                // Parse the JSON input
                var data = JsonSerializer.Deserialize<EnergyData>(jsonInput);

                // Call the method to get the two dictionaries: energy sums and percentages
                var result = GetEnergyAndPercentage(data);

                // Output the results
                Console.WriteLine("Energy by Unique ID:");
                foreach (var kvp in result.Item1) // energy sums
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value:F6}");
                }

                Console.WriteLine("\nPercentage by Unique ID:");
                foreach (var kvp in result.Item2) // percentages
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value:F2}%");

                }
                if (result.Item2.Values.Any(p => p < 0.2))
                {
                    // Compose and send email
                    SendEmail(_mailSettings.emailTo, result.Item2);
                }
            }

            void SendEmail(string recipientEmail, Dictionary<string, double> energyPercentages)
            {
                // Create the email body
                string body = "The following unique IDs have an energy percentage below 0.2%:\n\n";

                // Loop through the dictionary and find those with percentages below 0.2%
                foreach (var kvp in energyPercentages)
                {
                    if (kvp.Value < 0.2)
                    {
                        body += $"{kvp.Key}: {kvp.Value:F2}%\n";
                    }
                }
                
                // Create the email message
                var message = new MailMessage();
                message.From = new MailAddress(_mailSettings.fromAddress); // Your email
                message.To.Add(recipientEmail);
                message.Subject = "Alert: Low Energy Percentage Detected";
                message.Body = body;

                // Setup SMTP client
                using (var smtpClient = new SmtpClient(_mailSettings.smtpServer) // Use your SMTP server
                {
                    Port = _mailSettings.emailPort ,// 587, // SMTP port
                    Credentials = new NetworkCredential(_mailSettings.smtpUser, _mailSettings.smtpPs), // Your email credentials
                    EnableSsl = true // SSL/TLS for security
                })
                {
                    try
                    {
                        // Send the email
                        smtpClient.Send(message);
                        Console.WriteLine("Email sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email: {ex.Message}");
                    }
                }
            }
            Tuple<Dictionary<string, double>, Dictionary<string, double>> GetEnergyAndPercentage(EnergyData data)
            {
                // Dictionary to hold the sum of energy for each unique ID
                var energySums = new Dictionary<string, double>();

                // Parse the energy data and aggregate energy by unique ID
                foreach (var entry in data.data.energy)
                {
                    // Split the entry by the dash ("-")
                    var parts = entry.Split('-');
                    if (parts.Length < 3)
                        continue;

                    string uniqueId = parts[0] + "-" + parts[1]; // Use first two parts as the unique ID
                    double energyValue = double.Parse(parts[2]); // The third part is the energy value

                    // Add the energy value to the corresponding unique ID's total in the dictionary
                    if (energySums.ContainsKey(uniqueId))
                    {
                        energySums[uniqueId] += energyValue;
                    }
                    else
                    {
                        energySums[uniqueId] = energyValue;
                    }
                }

                // Calculate the total energy
                double totalEnergy = energySums.Values.Sum();

                // Dictionary to hold the percentage of total energy for each unique ID
                var energyPercentages = new Dictionary<string, double>();

                // Calculate the percentage for each unique ID
                foreach (var kvp in energySums)
                {
                    double percentage = (kvp.Value / totalEnergy) * 100;
                    energyPercentages[kvp.Key] = percentage;
                }

                // Return the two dictionaries (energy sums and percentages)
                return new Tuple<Dictionary<string, double>, Dictionary<string, double>>(energySums, energyPercentages);
            }
            Console.WriteLine("Generated Signature: " + signature);

        }
        // Method to build the query string from a dictionary
        string BuildQueryString(Dictionary<string, string> queryParams)
        {
            var queryList = new List<string>();

            foreach (var param in queryParams)
            {
                string encodedKey = Uri.EscapeDataString(param.Key);   // URL encode key
                string encodedValue = Uri.EscapeDataString(param.Value); // URL encode value
                queryList.Add($"{encodedKey}={encodedValue}");
            }

            // Join the encoded key-value pairs with '&' and return the query string
            return string.Join("&", queryList);
        }

        public string GenerateHmacSha256Signature(string appSecret, string stringToSign)
        {
            // Convert the appSecret to bytes using UTF-8 encoding
            byte[] appSecretBytes = Encoding.UTF8.GetBytes(appSecret);

            // Initialize the HMACSHA256 algorithm with the appSecret as the key
            using (HMACSHA256 hmacSha256 = new HMACSHA256(appSecretBytes))
            {
                // Convert the stringToSign to bytes using UTF-8 encoding
                byte[] stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);

                // Compute the HMACSHA256 hash
                byte[] hashBytes = hmacSha256.ComputeHash(stringToSignBytes);

                // Convert the hash result to Base64 string
                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}
