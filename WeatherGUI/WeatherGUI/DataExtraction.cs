using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Diagnostics;
using System.Collections;

namespace WeatherGUI
{
/*
 * The DataExtraction script is designed to aggregate and process weather-related 
 * data from multiple sources, providing detailed forecasts for specified locations. 
 * It parses both XML and JSON data formats to extract and organize information on tide 
 * conditions, swell data, wind characteristics, rainfall amounts, and rainfall 
 * probabilities into a daily structured overview. This systematic extraction and compilation
 * of weather data facilitate informed 
 * decision-making and planning based on comprehensive environmental conditions.
 */
    internal class DataExtraction
    {
        // Extracts specific weather details from XML data and updates the combinedData dictionary.
        public void XMLExtraction(string xmlData, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Parse the XML data into a navigable document structure.
            XDocument xmlDoc = XDocument.Parse(xmlData);

            // Find the XML element that corresponds to the Perth Coast area.
            var perthArea = xmlDoc.Descendants("area").FirstOrDefault(a => a.Attribute("description")?.Value == "Perth Coast: Two Rocks to Dawesville");

            if (perthArea != null)  // Check if the Perth Coast area is found.
            {
                // Iterate through each forecast period within the Perth Coast area element.
                foreach (var forecastPeriod in perthArea.Descendants("forecast-period"))
                {
                    ProcessForecastPeriod(forecastPeriod, combinedData);
                }
            }
        }

        //Processes and adds a forecast period's weather details to the corresponding day's entry in a dictionary of DayInfo objects.
        private void ProcessForecastPeriod(XElement forecastPeriod, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Parse the start time of the forecast period.
            DateTime startTime = DateTime.Parse(forecastPeriod.Attribute("start-time-local")?.Value);
            DateTime dateKey = startTime.Date;

            // Ensure there is a DayInfo object in the dictionary for this date.
            if (!combinedData.ContainsKey(dateKey))
            {
                combinedData[dateKey] = new DayInfo();
            }

            // Extract weather details like winds, seas, swell, and weather from the forecast.
            string forecastWinds = forecastPeriod.Descendants("text").FirstOrDefault(t => t.Attribute("type")?.Value == "forecast_winds")?.Value;
            string forecastSeas = forecastPeriod.Descendants("text").FirstOrDefault(t => t.Attribute("type")?.Value == "forecast_seas")?.Value;
            string forecastSwell = forecastPeriod.Descendants("text").FirstOrDefault(t => t.Attribute("type")?.Value == "forecast_swell1")?.Value;
            string forecastWeather = forecastPeriod.Descendants("text").FirstOrDefault(t => t.Attribute("type")?.Value == "forecast_weather")?.Value;

            // Compile the extracted weather details into a message string.
            string message = $"Winds: {forecastWinds}\n";
            message += $"Seas: {forecastSeas}\n";
            message += $"Swell: {forecastSwell}\n";
            message += $"Weather: {forecastWeather}\n";

            // Update the DayInfo object for the date with the compiled weather details.
            combinedData[dateKey].Weather += message;
        }

        //Iterates through tide forecast data in JSON and processes each day's tides into a dictionary of DayInfo objects.
        public void TideDetailExtraction(JObject tideJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the tide forecasts within the JSON structure.
            var tideDays = tideJson["forecasts"]["tides"]["days"];

            // Check if tide forecast data is present.
            if (tideDays != null)
            {
                // Iterate through each day's tide data.
                foreach (var day in tideDays)
                {
                    ProcessTideDay(day, combinedData);
                }
            }
        }

        //Processes tide entries for a single day and updates or adds tide information to the corresponding DayInfo object in a dictionary.
        private void ProcessTideDay(JToken day, Dictionary<DateTime, DayInfo> combinedData)
        {
            string dateTimeString = day["dateTime"].Value<string>();
            DateTime tideDate;

            // Try parsing the extracted date string into a DateTime object.
            if (DateTime.TryParse(dateTimeString, out tideDate))
            {
                StringBuilder dayTideInfo = new StringBuilder();

                // Iterate through each tide entry for the day.
                foreach (var entry in day["entries"])
                {
                    string formattedEntry = FormatTideEntry(entry);
                    dayTideInfo.AppendLine(formattedEntry);
                }

                // Ensure there is a DayInfo object in the dictionary for this date.
                if (!combinedData.ContainsKey(tideDate))
                {
                    combinedData[tideDate] = new DayInfo();
                }

                // Update the DayInfo object for the date with the compiled tide details.
                combinedData[tideDate].Tides = dayTideInfo.ToString();
            }
            else
            {
                // Optional: Handle the scenario where the tide day couldn't be parsed.
                Console.WriteLine($"Failed to parse tide day: {dateTimeString}");
            }
        }

        //Formats a single tide entry into a string with its type, time, and height, or indicates a parsing failure.
        private string FormatTideEntry(JToken entry)
        {
            string entryDateTimeString = entry["dateTime"].Value<string>();
            DateTime entryDateTime;

            // Try parsing the tide entry time string.
            if (DateTime.TryParse(entryDateTimeString, out entryDateTime))
            {
                // Format the tide entry time and extract tide height and type.
                string formattedEntryTime = entryDateTime.ToString("HH:mm");
                double height = entry["height"].Value<double>();
                string type = entry["type"].Value<string>();

                // Return the compiled tide entry details as a formatted string.
                return $"{type} tide at {formattedEntryTime} with a height of {height}m";
            }
            else
            {
                // Return a message for failed parsing of the tide entry time.
                return $"Failed to parse tide entry time: {entryDateTimeString}";
            }
        }

        // Extracts swell details from JSON data and updates the combinedData dictionary.
        public void SwellDetailExtraction(JObject swellJson, JObject closestLocationsJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the swell forecasts within the JSON structure.
            var swellDays = swellJson["forecasts"]["swell"]["days"];

            // Check for and extract the closest swell location details.
            if (closestLocationsJson["swell"] is JArray swellLocations && swellLocations.Count > 0)
            {
                var closestSwellLocation = swellLocations[0];
                // Extract the name of the closest swell location.
                string locationName = closestSwellLocation["name"].Value<string>();

                // Ensure there are swell forecasts to process.
                if (swellDays != null)
                {
                    // Iterate through each day's swell forecast.
                    foreach (var day in swellDays)
                    {
                        ProcessSwellDay(day, locationName, combinedData);
                    }
                }
            }
        }

        //Processes swell entries for a day, compiles them with location name, and updates the corresponding DayInfo object in a dictionary.
        private void ProcessSwellDay(JToken day, string locationName, Dictionary<DateTime, DayInfo> combinedData)
        {
            string dateTimeString = day["dateTime"].Value<string>();
            DateTime swellDate;

            // Attempt to parse the forecast date.
            if (DateTime.TryParse(dateTimeString, out swellDate))
            {
                StringBuilder daySwellInfo = new StringBuilder();
                // Add the closest swell location's name to the day's swell info.
                daySwellInfo.AppendLine($"Location: {locationName}");

                // Iterate through each swell entry for the day.
                foreach (var entry in day["entries"])
                {
                    ProcessSwellEntry(entry, daySwellInfo);
                }

                // Ensure there is a DayInfo object in the dictionary for this date.
                if (!combinedData.ContainsKey(swellDate))
                {
                    combinedData[swellDate] = new DayInfo();
                }

                // Update the DayInfo object for the date with the compiled swell details.
                combinedData[swellDate].Swell = daySwellInfo.ToString();
            }
            else
            {
                // Handle the scenario where the swell day couldn't be parsed.
                Console.WriteLine($"Failed to parse swell day: {dateTimeString}");
            }
        }

        //Formats a single swell entry with time, height, direction, and period, appending it to a StringBuilder.
        private void ProcessSwellEntry(JToken entry, StringBuilder daySwellInfo)
        {
            string entryDateTimeString = entry["dateTime"].Value<string>();
            DateTime entryDateTime;

            // Attempt to parse the entry time.
            if (DateTime.TryParse(entryDateTimeString, out entryDateTime))
            {
                // Format the entry time and extract swell details.
                string formattedEntryTime = entryDateTime.ToString("HH:mm");
                double height = entry["height"].Value<double>();
                string directionText = entry["directionText"].Value<string>();
                double period = entry["period"].Value<double>();

                // Compile the entry details into a formatted string.
                daySwellInfo.AppendLine($"{directionText} swell at {formattedEntryTime}. Height: {height}m, Period: {period}s");
            }
            else
            {
                // Handle failed parsing of the entry time.
                daySwellInfo.AppendLine($"Failed to parse swell entry time: {entryDateTimeString}");
            }
        }

        //Extracts wind details from JSON, processes each day's wind data, and compiles it into a list of WindData objects.
        public List<WindData> WindDetailExtraction(JObject windJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Initialize a list to hold all processed wind data.
            List<WindData> windDataList = new List<WindData>();

            // Access the "days" array from the wind forecast section of the JSON structure.
            var windDays = windJson["forecasts"]["wind"]["days"];
            if (windDays != null) // Check if there are any days to process.
            {
                // Iterate through each day in the "days" array.
                foreach (var day in windDays)
                {
                    // Process the wind data for the current day, returning a list of WindData objects.
                    List<WindData> dailyWindData = ProcessWindDay(day, combinedData);
                    // Add the daily wind data to the overall list of wind data.
                    windDataList.AddRange(dailyWindData);
                }
            }

            // Return the compiled list of WindData objects.
            return windDataList;
        }

        //Processes each wind entry for a day, updates the corresponding DayInfo object with wind details, and returns a list of WindData objects.
        private List<WindData> ProcessWindDay(JToken day, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Extract the date string from the day's JSON data and attempt to parse it into a DateTime object.
            string dateTimeString = day["dateTime"].Value<string>();
            DateTime windDate;
            List<WindData> dailyWindData = new List<WindData>();

            if (DateTime.TryParse(dateTimeString, out windDate))
            {
                windDate = windDate.Date; // Normalize the date to remove time of day.

                // Check if the dictionary already contains a DayInfo object for this date, and if not, create a new one.
                if (!combinedData.ContainsKey(windDate))
                {
                    combinedData[windDate] = new DayInfo();
                }

                StringBuilder dayWindInfo = new StringBuilder(); // Initialize a StringBuilder to compile wind details.

                // Iterate through each wind entry in the day's data.
                foreach (var entry in day["entries"])
                {
                    // Process the wind entry, adding its data to the daily wind data list and compiling a summary string.
                    WindData currentData = ProcessWindEntry(entry, dayWindInfo);
                    if (currentData != null) // If the entry was successfully processed.
                    {
                        dailyWindData.Add(currentData); // Add the WindData object to the list.
                    }
                }

                // Update the DayInfo object for this date with the compiled wind details.
                combinedData[windDate].Wind = dayWindInfo.ToString();

                // Perform calculations to find wind minima and update the DayInfo object accordingly.
                Calculations calculations = new Calculations();
                combinedData[windDate].WindMinima = calculations.FindLocalMinima(dailyWindData);
            }

            // Return the list of WindData objects.
            return dailyWindData;
        }

        //Processes a single wind entry, formats its details, appends to a StringBuilder, and returns a WindData object or null if invalid.
        private WindData ProcessWindEntry(JToken entry, StringBuilder dayWindInfo)
        {
            // Attempt to parse the entry's dateTime string into a DateTime object.
            string entryDateTimeString = entry["dateTime"].Value<string>();
            if (DateTime.TryParse(entryDateTimeString, out DateTime entryDateTime))
            {
                // Format the entry time and extract wind speed, direction, and textual description of the direction.
                string formattedEntryTime = entryDateTime.ToString("HH:mm");
                double windSpeed = entry["speed"].Value<double>();
                double direction = entry["direction"].Value<double>();
                string directionText = entry["directionText"].Value<string>();

                // Compile the extracted information into a formatted string and append it to the StringBuilder.
                dayWindInfo.AppendLine($"At {formattedEntryTime}: Speed - {windSpeed} km/h, Direction - {directionText} ({direction}°)");

                // Return a new WindData object populated with the extracted and parsed data.
                return new WindData { Time = entryDateTime, Speed = windSpeed };
            }

            // If the dateTime string could not be parsed, return null.
            return null;
        }

        //Extracts rainfall details from JSON and processes each day's rainfall data into a dictionary of DayInfo objects.
        public void RainfallDetailExtraction(JObject rainfallJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the array of days with rainfall forecasts within the JSON structure.
            var rainfallDays = rainfallJson["forecasts"]["rainfall"]["days"];
            // Check if the rainfallDays array is not null to ensure there is data to process.
            if (rainfallDays != null)
            {
                // Iterate over each day in the rainfallDays array.
                foreach (var day in rainfallDays)
                {
                    // Attempt to parse the dateTime string from the current day's data.
                    string dateTimeString = day["dateTime"].Value<string>();
                    if (DateTime.TryParse(dateTimeString, out DateTime rainfallDate))
                    {
                        // If parsing is successful, process the rainfall data for the current day.
                        ProcessRainfallDay(day, rainfallDate, combinedData);
                    }
                    else
                    {
                        // If the dateTime string cannot be parsed, log an error message.
                        Console.WriteLine($"Failed to parse rainfall day: {dateTimeString}");
                    }
                }
            }
        }

        //Processes rainfall entries for a day, compiles them into a summary, and updates the corresponding DayInfo object in a dictionary.
        private void ProcessRainfallDay(JToken day, DateTime rainfallDate, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Initialize a StringBuilder to compile the rainfall information into a detailed message.
            StringBuilder dayRainfallInfo = new StringBuilder();

            // Iterate through each rainfall entry for the day.
            foreach (var entry in day["entries"])
            {
                // Extract the range code, probability, and start/end range values from the entry.
                string rangeCode = entry["rangeCode"].Value<string>();
                int probability = entry["probability"].Value<int>();
                int? startRange = entry["startRange"].HasValues ? entry["startRange"].Value<int>() : (int?)null;
                int? endRange = entry["endRange"].HasValues ? entry["endRange"].Value<int>() : (int?)null;

                // Format and append the extracted rainfall information to the StringBuilder.
                if (startRange.HasValue && endRange.HasValue)
                {
                    dayRainfallInfo.AppendLine($"Rainfall expected: {startRange}-{endRange}mm with a probability of {probability}%");
                }
                else if (startRange.HasValue)
                {
                    dayRainfallInfo.AppendLine($"Rainfall expected: >{startRange}mm with a probability of {probability}%");
                }
                else if (endRange.HasValue)
                {
                    dayRainfallInfo.AppendLine($"Rainfall expected: <{endRange}mm with a probability of {probability}%");
                }
                else
                {
                    dayRainfallInfo.AppendLine($"Rainfall expected: {rangeCode}mm with a probability of {probability}%");
                }
            }

            // Check if the combinedData dictionary already contains an entry for the rainfall date; if not, create a new DayInfo object.
            if (!combinedData.ContainsKey(rainfallDate))
            {
                combinedData[rainfallDate] = new DayInfo();
            }

            // Update the DayInfo object for the rainfall date with the compiled rainfall details.
            combinedData[rainfallDate].Rainfall = dayRainfallInfo.ToString();
        }

        //Extracts rainfall probability details from JSON and processes each day's data into a dictionary of DayInfo objects.
        public void RainfallProbabilityDetailExtraction(JObject rainfallProbabilityJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the array of days with rainfall probability forecasts within the JSON structure.
            var rainfallProbabilityDays = rainfallProbabilityJson["forecasts"]["rainfallprobability"]["days"];
            // Ensure there are days to process.
            if (rainfallProbabilityDays != null)
            {
                // Iterate over each day in the rainfall probability forecasts.
                foreach (var day in rainfallProbabilityDays)
                {
                    // Attempt to parse the dateTime string from the current day's data.
                    string dateTimeString = day["dateTime"].Value<string>();
                    if (DateTime.TryParse(dateTimeString, out DateTime rainfallProbabilityDate))
                    {
                        // If parsing is successful, process the rainfall probability data for the current day.
                        ProcessRainfallProbabilityDay(day, rainfallProbabilityDate, combinedData);
                    }
                    else
                    {
                        // Log an error message if the dateTime string cannot be parsed.
                        Console.WriteLine($"Failed to parse rainfall probability day: {dateTimeString}");
                    }
                }
            }
        }

        //Processes rainfall probability entries for a day, compiles them, and updates the corresponding DayInfo object in a dictionary.
        private void ProcessRainfallProbabilityDay(JToken day, DateTime rainfallProbabilityDate, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Initialize a StringBuilder to compile the rainfall probability information into a detailed message.
            StringBuilder dayRainfallProbabilityInfo = new StringBuilder();

            // Iterate through each rainfall probability entry for the day.
            foreach (var entry in day["entries"])
            {
                // Attempt to parse the entry's dateTime string into a DateTime object.
                string entryDateTimeString = entry["dateTime"].Value<string>();
                if (DateTime.TryParse(entryDateTimeString, out DateTime entryDateTime))
                {
                    // Format the entry time and extract the probability of rainfall.
                    string formattedEntryTime = entryDateTime.ToString("HH:mm");
                    int probability = entry["probability"].Value<int>();

                    // Append the formatted rainfall probability information to the StringBuilder.
                    dayRainfallProbabilityInfo.AppendLine($"Chance of rainfall at {formattedEntryTime} is {probability}%");
                }
                else
                {
                    // Append a message to the StringBuilder if the entry's dateTime string cannot be parsed.
                    dayRainfallProbabilityInfo.AppendLine($"Failed to parse rainfall probability entry time: {entryDateTimeString}");
                }
            }

            // Check if the combinedData dictionary already contains an entry for the rainfall probability date; if not, create a new DayInfo object.
            if (!combinedData.ContainsKey(rainfallProbabilityDate))
            {
                combinedData[rainfallProbabilityDate] = new DayInfo();
            }

            // Update the DayInfo object for the rainfall probability date with the compiled information.
            combinedData[rainfallProbabilityDate].RainfallProbability = dayRainfallProbabilityInfo.ToString();
        }
    }
}
