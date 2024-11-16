using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PollutionIndex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string apiKey;
        private readonly HttpClient httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            apiKey = Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("API key is missing.");
            }
        }

        private async void AddressInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (AddressInput.Text.Length < 3)
            {
                SuggestionsListBox.Visibility = Visibility.Collapsed;
                return;
            }
            
            string url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={AddressInput.Text}&key={apiKey}";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var predictions = JObject.Parse(jsonResponse)["predictions"];
                List<String> suggestions = new List<String>();

                foreach (var prediction in predictions)
                {
                    suggestions.Add(prediction["description"].ToString());
                }

                SuggestionsListBox.ItemsSource = suggestions;
                SuggestionsListBox.Visibility = suggestions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            } 
        }

        private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestionsListBox.SelectedItem != null)
            {
                AddressInput.Text = SuggestionsListBox.SelectedItem.ToString();
                SuggestionsListBox.Visibility = Visibility.Collapsed;
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddressInput.Text))
            {
                MessageBox.Show("Please enter valid address");
                return;
            }
            // get coords
            var (latitude, longitude) = await GetCoordinates(AddressInput.Text);

            // get roads within 1.5mi radius
            List<(double lat, double lon, string type)> roadways = await GetNearbyRoadways(latitude, longitude);

            if (roadways != null)
            {
                Debug.WriteLine($"Roadway coords: {string.Join(", ", roadways.Select(r => $"({r.lat}, {r.lon}, {r.type})"))}");
                Debug.WriteLine($"Roadways analyzed: {roadways.Count}");
            }

            List<(double, string)> distances = GetDistances(roadways, latitude, longitude);
            Debug.WriteLine($"Distances: {string.Join(", ", distances)}");

            var testDist = new List<(double, string)> { (100, "residential"), (200, "residential") };

            double noise = CalculateNoiseDropoff(testDist);
            Debug.WriteLine($"AVERAGE NOISE IN LOCATION: {noise}");

            NoiseLevelTextBlock.Text = $"The average noise level at the given address is ~{noise:F1} dB.";
        }

        private async Task<(double lat, double lon)> GetCoordinates(string address)
        {
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={apiKey}";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                //Debug.WriteLine(jsonResponse);
                var results = JObject.Parse(jsonResponse)["results"];

                if (results != null && results.Any())
                {
                    var location = results[0]["geometry"]["location"];
                    double latitude = (double)location["lat"];
                    double longitude = (double)location["lng"];
                    return (latitude, longitude);
                }
            }
            MessageBox.Show("Could not find coordinates for given address");
            return (0, 0);
        }

        private async Task<List<(double lat, double lon, string type)>> GetNearbyRoadways(double latitude, double longitude)
        {
            int radiusMotorway = 2414;
            int radiusArterials = 1609;
            int radiusResidential = 804;

            string overpassQuery = "[out:json];" +
                       "(way[\"highway\"~\"motorway|trunk|primary\"](around:{0},{1},{2});" +
                       "way[\"highway\"~\"secondary|tertiary\"](around:{3},{1},{2});" +
                       "way[\"highway\"=\"residential\"](around:{4},{1},{2});" +
                       ");" +
                       "out body;" +
                       ">;" +
                       "out skel qt;";

            string formattedQuery = string.Format(overpassQuery, radiusMotorway, latitude, longitude,radiusArterials, radiusResidential);
            string url = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(formattedQuery)}";


            HttpResponseMessage response = await httpClient.GetAsync(url);
            Debug.WriteLine($"Status Code: {response.StatusCode}");
            List<(double lat, double lon, string type)> roadways = new List<(double, double, string)>();
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var elements = JObject.Parse(jsonResponse)["elements"];

                foreach (var element in elements)
                {
                    if (element["type"].ToString() == "way" && element["tags"]?["highway"] != null)
                    {
                        string type = element["tags"]["highway"].ToString();

                        if (element["nodes"] != null)
                        {
                            var nodeIds = element["nodes"].Take(20).ToList();
                            foreach (var nodeId in nodeIds)
                            {
                                var node = elements.FirstOrDefault(e => e["type"]?.ToString() == "node" && e["id"]?.ToString() == nodeId.ToString());
                                if (node != null)
                                {
                                    double lat = (double)node["lat"];
                                    double lon = (double)node["lon"];
                                    roadways.Add((lat, lon, type));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Error Response: {errorResponse}");
                MessageBox.Show("No response from Overpass API");
            }
            return roadways;
        }

        private List<(double, string)> GetDistances(List<(double lat, double lon, string type)> points, double lat, double lon)
        {
            const double R = 6371 * 1000; // Radius of the Earth in meters
            const double p = Math.PI / 180; // Conversion factor for degrees to radians

            var distances = new List<(double, string)>();

            foreach (var point in points)
            {
                // Convert degrees to radians
                double lat1Rad = lat * p;
                double lon1Rad = lon * p;
                double lat2Rad = point.lat * p;
                double lon2Rad = point.lon * p;

                // Haversine formula
                double a = 0.5 - Math.Cos(lat2Rad - lat1Rad) / 2 +
                           Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                           (1 - Math.Cos(lon2Rad - lon1Rad)) / 2;

                double distanceInMeters = 2 * R * Math.Asin(Math.Sqrt(a));

                distances.Add((distanceInMeters, point.type));
            }

            return distances;
        }

        private double CalculateNoiseDropoff(List<(double dist, string type)> distances)
        {
            double totalNoiseLevel = 0;
            int count = 0;

            foreach (var road in distances)
            {
                double roadDist = road.dist;

                double referenceNoiseLevel = 0;
                switch (road.type)
                {
                    case "residential":
                        referenceNoiseLevel = 60;
                        break;
                    case "motorway_link":
                        referenceNoiseLevel = 80;
                        break;
                    default:
                        referenceNoiseLevel = 70;
                        break;
                }

                if (roadDist > 0)
                {
                    double noiseLevel = referenceNoiseLevel - 20 * Math.Log10(roadDist);

                    if (roadDist < 20)
                    {
                        noiseLevel = referenceNoiseLevel;
                    }

                    totalNoiseLevel += noiseLevel;
                    count++;
                }
            }
            return count > 0 ? totalNoiseLevel / count : 0;
        }
    }
}
