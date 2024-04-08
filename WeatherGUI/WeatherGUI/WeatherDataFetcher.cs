using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Net.WebRequestMethods;

namespace WeatherGUI
{
    internal class WeatherDataFetcher
    {
        // Identifier for Perth location used in API calls.
        public int locationIdForPerth = 14576;
        // API key placeholder for authenticating requests. Users must replace this with their own API key.
        public string APIKey = "";
        // Base URL for the weather data API.
        public string BASE_URL = "https://api.willyweather.com.au/v2/";
        // Stores the current date in YYYY-MM-DD format, used in API requests.
        private string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

    // Asynchronously fetches weather data in XML format from the Bureau of Meteorology (BOM).
    public async Task<string> FetchXMLData()
    {
           
            string bomUrl = "ftp://ftp.bom.gov.au/anon/gen/fwo/IDW11160.xml"; // URL for the BOM FTP server to download weather data.
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(bomUrl);// Sets up an FTP request to the BOM URL.
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpRequest.Credentials = new NetworkCredential("anonymous", "email@example.com");// Anonymous credentials for BOM FTP access. Replace with actual credentials if necessary.

            string xmlData = string.Empty;

            try
            {
                // Executes the FTP request and reads the response.
                using (FtpWebResponse ftpResponse = (FtpWebResponse)await ftpRequest.GetResponseAsync())
                using (Stream responseStream = ftpResponse.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    xmlData = await reader.ReadToEndAsync();
                }
                return xmlData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching weather data: {ex.Message}");
                return null; // Returns null to indicate failure.

            }
        }
        // Asynchronously fetches tide data for Perth from the weather API.
        public async Task<string> FetchTideData()
        {
            // Constructs the URL to request tide data.
            string apiUrl = $"{BASE_URL}{APIKey}/locations/{locationIdForPerth}/weather.json?forecasts=tides&days=3&startDate={currentDate}";
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // Reads the error content if the request fails.
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch tide data. Status code: {response.StatusCode}. Error content: {errorContent}");
                }
            }
        }

        // Each of the following methods follows a similar pattern to `FetchTideData`, adjusting only the specific forecast type requested.
        // Asynchronously fetches swell data for a given location.
        public async Task<string> FetchSwellData(int locationNumber)
        {
            string apiUrl = $"{BASE_URL}{APIKey}/locations/{locationNumber}/weather.json?forecasts=swell&days=3&startDate={currentDate}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch swell data. Status code: {response.StatusCode}. Error content: {errorContent}");
                }
            }
        }

        public async Task<string> FetchClosestLocations( string[] weatherTypes)
        {
            string types = string.Join(",", weatherTypes);
            string url = $"{BASE_URL}{APIKey}/search/closest.json?id={locationIdForPerth}&weatherTypes={types}&units=distance:km";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                return response;
            }
        }

        public async Task<string> FetchWindData()
        {
            string apiUrl = $"{BASE_URL}{APIKey}/locations/{locationIdForPerth}/weather.json?forecasts=wind&days=3&startDate={currentDate}";
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch wind data. Status code: {response.StatusCode}. Error content: {errorContent}");
                }
            }
        }
        public async Task<string> FetchRainfallSummaryData()
        {
            string apiUrl = $"{BASE_URL}{APIKey}/locations/{locationIdForPerth}/weather.json?forecasts=rainfall&days=3&startDate={currentDate}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch rainfall data. Status code: {response.StatusCode}. Error content: {errorContent}");
                }
            }
        }
        public async Task<string> FetchRainfallProbabilityData()
        {
            string apiUrl = $"{BASE_URL}{APIKey}/locations/{locationIdForPerth}/weather.json?forecasts=rainfallprobability&days=3&startDate={currentDate}";
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch rainfall probability data. Status code: {response.StatusCode}. Error content: {errorContent}");
                }
            }
        }
    }
}
