using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WeatherGUI
{
    /*
     * this script contains the calculations for the wind data.
     * it also contains a function to return if the rainfall should be gathered
     */
    public class Calculations
    {
        // Identifies wind data points that are local minima and have a wind speed lower than the average wind speed of the dataset.
        public List<WindData> FindLocalMinima(List<WindData> windData)
        {
            // Stores the identified local minima.
            List<WindData> localMinima = new List<WindData>();
            // Calculates the average wind speed for the entire dataset.
            double averageWindSpeed = windData.Average(w => w.Speed);

            // Iterates through each data point, skipping the first and last points to avoid out-of-range errors.
            for (int i = 1; i < windData.Count - 1; i++)
            {
                // Checks if the current data point is a local minimum by comparing it with its immediate neighbors.
                if (windData[i].Speed < windData[i - 1].Speed && windData[i].Speed < windData[i + 1].Speed)
                {
                    // Calculates the average wind speed change around the local minimum.
                    double averageChange = CalculateAverageWindChange(windData, i);
                    // Assigns the calculated average change to the data point.
                    windData[i].AverageWindChangeAroundMinima = averageChange;

                    // Adds the data point to the list of local minima only if its speed is below the dataset's average wind speed.
                    if (windData[i].Speed < averageWindSpeed)
                    {
                        localMinima.Add(windData[i]);
                    }
                }
            }
            // Returns the list of identified local minima.
            return localMinima;
        }

        // Calculates the average wind speed change around a specific index in the dataset, considering up to two points before and after.
        private double CalculateAverageWindChange(List<WindData> windData, int index)
        {
            int dataPointsBefore = 0; // Counts the number of data points considered before the index.
            int dataPointsAfter = 0; // Counts the number of data points considered after the index.
            double totalChangeBefore = 0.0; // Accumulates total wind speed changes before the index.
            double totalChangeAfter = 0.0; // Accumulates total wind speed changes after the index.

            // Calculates total wind speed change before the index, up to two points.
            for (int i = 1; i <= 2 && index - i >= 0; i++)
            {
                totalChangeBefore += Math.Abs(windData[index - i].Speed - windData[index - i + 1].Speed);
                dataPointsBefore++;
            }

            // Calculates total wind speed change after the index, up to two points.
            for (int i = 1; i <= 2 && index + i < windData.Count; i++)
            {
                totalChangeAfter += Math.Abs(windData[index + i].Speed - windData[index + i - 1].Speed);
                dataPointsAfter++;
            }

            // Calculates and returns the average wind speed change around the index.
            double averageChange = (totalChangeBefore + totalChangeAfter) / (dataPointsBefore + dataPointsAfter);
            return averageChange;
        }

        // Determines whether rainfall probability data should be fetched based on any non-zero probability of rain in the dataset.
        public bool ShouldFetchRainfallProbabilityData(string rainfallData)
        {
            JObject parsedRainfallData = JObject.Parse(rainfallData);// Parses the JSON string into a JObject.
            var rainfallDays = parsedRainfallData["forecasts"]["rainfall"]["days"];// Accesses the rainfall forecast data.

            // Checks if the rainfall forecast data is available.
            if (rainfallDays != null)
            {
                // Iterates through each day's forecasts.
                foreach (var day in rainfallDays)
                {
                    // Iterates through each entry in a day's forecast.
                    foreach (var entry in day["entries"])
                    {
                        // Extracts the probability of rain.
                        int probability = entry["probability"].Value<int>();
                        if (probability > 0)
                        {
                            return true;  // We found a day with non-zero probability of rain, fetch the details
                        }
                    }
                }
            }
            return false; //no probability of rain, do not fetch the details
        }
    }
    
}
