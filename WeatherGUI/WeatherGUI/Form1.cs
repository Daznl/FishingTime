namespace WeatherGUI
{
    using System.Net;
    using System.IO;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using System.Text;
    using System.Net.Http;
    using System.Text.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using System.Security.Cryptography.X509Certificates;

    //using WeatherDataFetcher;


    public class DayInfo
    {
        public string Weather { get; set; } = string.Empty;
        public string Tides { get; set; } = string.Empty;
        public string Swell { get; set; } = string.Empty;
        public string Wind { get; set; } = string.Empty;
        public List<WindData> WindMinima { get; set; }
        public string Rainfall { get; set; } = string.Empty;
        public string RainfallProbability { get; set; } = string.Empty;
    }

    public class WindData
    {
        public DateTime Time { get; set; }
        public double Speed { get; set; }
        public double AverageWindChangeAroundMinima { get; set; } = 0.0;
    }

    /*The script is a component of a GUI application for gathering and presenting weather information.
     * It reacts to user inputs to fetch weather data from various sources, including XML and JSON 
     * formats, covering aspects like tides, wind, rainfall, and swell conditions. Utilizing the 
     * DataExtraction class, it parses and consolidates this data into a daily summary, which is then
     * displayed to the user. The script also decides whether to retrieve detailed rainfall probability
     * based on initial rainfall data, ensuring a comprehensive weather overview. This process is aimed
     * at providing users with detailed and actionable weather insights.
     */
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Event handler for the button click event to start the process of fetching and displaying weather data.
        private async void button1_Click(object sender, EventArgs e)
        {
            // Creates an instance of the data fetcher class
            WeatherDataFetcher dataFetcher = new WeatherDataFetcher();
            // Fetch the XML data from BOM
            string xmlData = await dataFetcher.FetchXMLData();

            // Fetch WillyWeather data for tides
            string tideData = await dataFetcher.FetchTideData();

            // Define weather types to fetch closest locations that match these criteria.
            string[] weatherTypes = new[] { "swell", "tides", "general" };

            // fetch data for the closest locations based on specified weather types.
            string closestLocationsData = await dataFetcher.FetchClosestLocations(weatherTypes);

            // Parse the JSON string of closest locations to find the ID of the closest swell location.
            JObject parsedClosestLocationData = JObject.Parse(closestLocationsData);
            int closestLocationId = parsedClosestLocationData["swell"][0]["id"].Value<int>();

            string swellData = await dataFetcher.FetchSwellData(closestLocationId);
            string windData = await dataFetcher.FetchWindData();
            string rainfallData = await dataFetcher.FetchRainfallSummaryData();

            // Initialize the calculations class to use its methods for data analysis.
            Calculations calculations = new Calculations();
            // Determine if detailed rainfall probability data should be fetched based on the summary rainfall data.
            bool shouldFetchRainfallProbability = calculations.ShouldFetchRainfallProbabilityData(rainfallData);

            string rainfallProbabilityData = "";

            // If detailed rainfall probability data is needed, fetch it asynchronously.
            if (shouldFetchRainfallProbability)
            {
                rainfallProbabilityData = await dataFetcher.FetchRainfallProbabilityData();
            }

            // Combine and process all fetched weather data for display.
            ExtractPerthWeatherData(xmlData, tideData, windData, rainfallData, rainfallProbabilityData, swellData, closestLocationsData); // Pass closestLocationsData too
        }

        // This method processes and combines the weather data fetched from different sources, then displays it.
        private void ExtractPerthWeatherData(string xmlData, string tideData, string windData, string rainfallData, string rainfallProbabilityData, string swellData, string closestLocationsData)
        {
            // Instance of the DataExtraction class to use its methods.
            DataExtraction dataExtraction = new DataExtraction();
            // Dictionary to hold combined info for each day
            Dictionary<DateTime, DayInfo> combinedData = new Dictionary<DateTime, DayInfo>();

            // Extract and combine the data
            dataExtraction.XMLExtraction(xmlData, combinedData);
            dataExtraction.TideDetailExtraction(JObject.Parse(tideData), combinedData);
            dataExtraction.RainfallDetailExtraction(JObject.Parse(rainfallData), combinedData);

            // Check if rainfallProbabilityData is not empty or null before attempting extraction
            if (!string.IsNullOrEmpty(rainfallProbabilityData))
            {
                dataExtraction.RainfallProbabilityDetailExtraction(JObject.Parse(rainfallProbabilityData), combinedData);
            }

            // Extracts swell data using the closest location information.
            dataExtraction.SwellDetailExtraction(JObject.Parse(swellData), JObject.Parse(closestLocationsData), combinedData);

            // Extracts wind data and identifies local minima.
            List<WindData> windDataList = dataExtraction.WindDetailExtraction(JObject.Parse(windData), combinedData);

            // Iterating through the combined data to prepare and display daily weather messages.
            foreach (var entry in combinedData)
            {
                StringBuilder dailyMessage = new StringBuilder();   // Builds the message for each day.
                dailyMessage.AppendLine($"Date: {entry.Key:dddd, dd/MM/yyyy}");
                dailyMessage.AppendLine($"Weather:\n{entry.Value.Weather}");
                dailyMessage.AppendLine($"Tide Details:\n{entry.Value.Tides}");
                dailyMessage.AppendLine($"Wind Details:\n{entry.Value.Wind}");

                //if there is a value for WindMinima
                if (entry.Value.WindMinima != null && entry.Value.WindMinima.Count > 0)
                {
                    dailyMessage.AppendLine("Local Minima for Wind Speed:");
                    foreach (var minima in entry.Value.WindMinima)
                    {
                        dailyMessage.AppendLine($"At {minima.Time:HH:mm}: {minima.Speed} km/h, Average Change of Wind Speed is {minima.AverageWindChangeAroundMinima:0.00}km/h");
                    }
                }
                dailyMessage.AppendLine();  // Add an empty line for separation
                dailyMessage.AppendLine($"Swell Details:\n{entry.Value.Swell}");
                dailyMessage.AppendLine($"Rainfall Details:\n{entry.Value.Rainfall}");

                // Only append Rainfall Probability if there's valid data
                if (!string.IsNullOrEmpty(entry.Value.RainfallProbability))
                {
                    dailyMessage.AppendLine($"Rainfall Probability:\n{entry.Value.RainfallProbability}");
                }

                dailyMessage.AppendLine();  // Add an empty line for separation

                // Show the daily message in a MessageBox
                MessageBox.Show(dailyMessage.ToString(), "Weather Details", MessageBoxButtons.OK);
            }
        }

        // This method displays a given message in a MessageBox.
        private void PrintMessage(string message)
        {
            MessageBox.Show(message);
        }

        // Event handler for a button click that copies JSON data to the clipboard.
        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtJsonData.Text);
            MessageBox.Show("JSON copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}