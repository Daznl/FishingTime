using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

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
            }
        }

        // Extracts tide details from a JSON object and updates the combinedData dictionary.
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
                    string dateTimeString = day["dateTime"].Value<string>();
                    DateTime tideDate;

                    // Try parsing the extracted date string into a DateTime object.
                    if (DateTime.TryParse(dateTimeString, out tideDate))
                    {
                        StringBuilder dayTideInfo = new StringBuilder();

                        // Iterate through each tide entry for the day.
                        foreach (var entry in day["entries"])
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

                                // Compile the tide entry details into a formatted string.
                                dayTideInfo.AppendLine($"{type} tide at {formattedEntryTime} with a height of {height}m");
                            }
                            else
                            {
                                // Handle failed parsing of the tide entry time.
                                dayTideInfo.AppendLine($"Failed to parse tide entry time: {entryDateTimeString}");
                            }
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
                        // Handle the scenario where the tide day could not be parsed (this part is optional)
                        Console.WriteLine($"Failed to parse tide day: {dateTimeString}");
                    }
                }
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
                            //Handle the scenario where the swell day couldn't be parsed.
                            Console.WriteLine($"Failed to parse swell day: {dateTimeString}");
                        }
                    }
                }
            }
        }

        // Extracts wind details from JSON data, updates the combinedData dictionary, and returns a list of all wind data entries.
        public List<WindData> WindDetailExtraction(JObject windJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // To store all wind data across days.
            List<WindData> windDataList = new List<WindData>();

            // Access the wind forecasts within the JSON structure.
            var windDays = windJson["forecasts"]["wind"]["days"];

            // Ensure there are wind forecasts to process.
            if (windDays != null)
            {
                // Iterate through each day's wind forecast.
                foreach (var day in windDays)
                {
                    string dateTimeString = day["dateTime"].Value<string>();
                    DateTime windDate;

                    // Attempt to parse the forecast date.
                    if (DateTime.TryParse(dateTimeString, out windDate))
                    {

                        windDate = windDate.Date;

                        // Ensure there is a DayInfo object in the dictionary for this date.
                        if (!combinedData.ContainsKey(windDate))
                        {
                            combinedData[windDate] = new DayInfo();
                        }

                        StringBuilder dayWindInfo = new StringBuilder();
                        List<WindData> dailyWindData = new List<WindData>();

                        // Iterate through each wind entry for the day.
                        foreach (var entry in day["entries"])
                        {
                            string entryDateTimeString = entry["dateTime"].Value<string>();
                            DateTime entryDateTime;
                            if (DateTime.TryParse(entryDateTimeString, out entryDateTime))
                            {
                                // Format the entry time and extract wind details.
                                string formattedEntryTime = entryDateTime.ToString("HH:mm");
                                double windSpeed = entry["speed"].Value<double>();
                                double direction = entry["direction"].Value<double>();
                                string directionText = entry["directionText"].Value<string>();

                                // Compile the entry details into a formatted string.
                                dayWindInfo.AppendLine($"At {formattedEntryTime}: Speed - {windSpeed} km/h, Direction - {directionText} ({direction}°)");

                                // Store this entry for later analysis.
                                WindData currentData = new WindData { Time = entryDateTime, Speed = windSpeed };
                                dailyWindData.Add(currentData);
                                windDataList.Add(currentData);
                            }
                        }

                        // Update the DayInfo object for the date with the compiled wind details.
                        combinedData[windDate].Wind = dayWindInfo.ToString();

                        // Now that we've collected all the wind data for this day, calculate local minima
                        Calculations calculations = new Calculations();
                        combinedData[windDate].WindMinima = calculations.FindLocalMinima(dailyWindData);
                    }
                }
            }

            return windDataList;  // Returns all wind data across all days
        }

        // Extracts rainfall details from JSON data and updates the combinedData dictionary with the information.
        public void RainfallDetailExtraction(JObject rainfallJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the rainfall forecasts within the JSON structure.
            var rainfallDays = rainfallJson["forecasts"]["rainfall"]["days"];

            // Ensure there are rainfall forecasts to process.
            if (rainfallDays != null)
            {
                // Iterate through each day's rainfall forecast.
                foreach (var day in rainfallDays)
                {
                    string dateTimeString = day["dateTime"].Value<string>();
                    DateTime rainfallDate;

                    // Attempt to parse the forecast date string into a DateTime object.
                    if (DateTime.TryParse(dateTimeString, out rainfallDate))
                    {
                        StringBuilder dayRainfallInfo = new StringBuilder();

                        foreach (var entry in day["entries"])
                        {
                            // Extract rainfall details: range code, probability, divide, and start/end ranges of rainfall amount.
                            string rangeCode = entry["rangeCode"].Value<string>();
                            int probability = entry["probability"].Value<int>();
                            string rangeDivide = entry["rangeDivide"].Value<string>();
                            int? startRange = entry["startRange"].HasValues ? entry["startRange"].Value<int>() : (int?)null;
                            int? endRange = entry["endRange"].HasValues ? entry["endRange"].Value<int>() : (int?)null;

                            // Compile the rainfall entry details based on the presence of start/end range values.
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

                        // Ensure there is a DayInfo object in the dictionary for the parsed date.
                        if (!combinedData.ContainsKey(rainfallDate))
                        {
                            combinedData[rainfallDate] = new DayInfo();
                        }

                        // Update the DayInfo object for the date with the compiled rainfall details.
                        combinedData[rainfallDate].Rainfall = dayRainfallInfo.ToString();
                    }
                    else
                    {
                        // Handle the scenario where the forecast date couldn't be parsed.
                        Console.WriteLine($"Failed to parse rainfall day: {dateTimeString}");
                    }
                }
            }
        }

        // Extracts rainfall probability details from JSON data and updates the combinedData dictionary with this information.
        public void RainfallProbabilityDetailExtraction(JObject rainfallProbabilityJson, Dictionary<DateTime, DayInfo> combinedData)
        {
            // Access the rainfall probability forecasts within the JSON structure.
            var rainfallProbabilityDays = rainfallProbabilityJson["forecasts"]["rainfallprobability"]["days"];

            // Ensure there are rainfall probability forecasts to process.
            if (rainfallProbabilityDays != null)
            {
                // Iterate through each day's rainfall probability forecast.
                foreach (var day in rainfallProbabilityDays)
                {
                    // Extract the forecast date as a string.
                    string dateTimeString = day["dateTime"].Value<string>();
                    DateTime rainfallProbabilityDate;

                    // Attempt to parse the forecast date string into a DateTime object.
                    if (DateTime.TryParse(dateTimeString, out rainfallProbabilityDate))
                    {
                        StringBuilder dayRainfallProbabilityInfo = new StringBuilder();

                        // Iterate through each rainfall probability entry for the day.
                        foreach (var entry in day["entries"])
                        {
                            string entryDateTimeString = entry["dateTime"].Value<string>();
                            DateTime entryDateTime;

                            // Attempt to parse the entry time string into a DateTime object.
                            if (DateTime.TryParse(entryDateTimeString, out entryDateTime))
                            {
                                // Format the entry time for display.
                                string formattedEntryTime = entryDateTime.ToString("HH:mm");

                                // Extract the rainfall probability.
                                int probability = entry["probability"].Value<int>();

                                dayRainfallProbabilityInfo.AppendLine($"Chance of rainfall at {formattedEntryTime} is {probability}%");
                            }
                            else
                            {
                                // Handle the scenario where the entry time couldn't be parsed.
                                dayRainfallProbabilityInfo.AppendLine($"Failed to parse rainfall probability entry time: {entryDateTimeString}");
                            }
                        }

                        // Ensure there is a DayInfo object in the dictionary for the parsed date.
                        if (!combinedData.ContainsKey(rainfallProbabilityDate))
                        {
                            combinedData[rainfallProbabilityDate] = new DayInfo();
                        }

                        // Update the DayInfo object for the date with the compiled rainfall probability details.
                        combinedData[rainfallProbabilityDate].RainfallProbability = dayRainfallProbabilityInfo.ToString();
                    }
                    else
                    {
                        // Handle the scenario where the forecast date couldn't be parsed.
                        Console.WriteLine($"Failed to parse rainfall probability day: {dateTimeString}");
                    }
                }
            }
        }
    }
}
