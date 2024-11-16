# Pollution Index Application

This WPF (Windows Presentation Foundation) application calculates the average noise level at a given location based on nearby roadways. It leverages the Google Maps API for geocoding and place autocomplete and the Overpass API for retrieving road data.

---

## Features

- **Address Autocomplete**: Suggests addresses based on user input using the Google Maps Places API.
- **Noise Level Calculation**: Estimates average noise levels using road type data (e.g., residential, motorways) and distances from the selected address.
- **Interactive UI**: User-friendly interface for entering an address and viewing results.

---

## Prerequisites

1. **API Keys**:
   - Google Maps API Key (for Places and Geocoding).
2. **.NET Framework**: Ensure you have .NET Framework 4.7.2 or later installed.
3. **Libraries**:
   - Newtonsoft.Json (`Json.NET`) for JSON parsing.

---

## Setup Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/PollutionIndex.git
cd PollutionIndex
```

### 2. Set Up API Key
Ensure your Google Maps API key is set as an environment variable:
```bash
set GOOGLE_PLACES_API_KEY=your-google-api-key
```

### 3. Restore NuGet Packages
Open the solution in Visual Studio, and restore NuGet packages:
1. Navigate to `Tools` > `NuGet Package Manager` > `Manage NuGet Packages for Solution`.
2. Install missing dependencies if needed (e.g., Newtonsoft.Json).

### 4. Build and Run the Application
1. Build the solution in Visual Studio.
2. Run the application.

---

## How to Use

1. **Enter Address**: Start typing an address in the input box. Suggestions will appear based on the input.
2. **Select Suggestion**: Choose an address from the suggestions to autofill the input.
3. **Calculate Noise Level**: Click the **Fetch Data** button to calculate the average noise level at the location.
4. **View Results**: The estimated noise level will be displayed below the input.

---

## Project Structure

```
PollutionIndex/
│
├── MainWindow.xaml                 # Defines the application's UI layout
├── MainWindow.xaml.cs              # Implements logic for UI interactions and API calls
├── app.config                      # Configuration file for application settings
├── Properties/                     # Contains application settings and resources
├── PollutionIndex.csproj           # Project configuration file
└── ReadMe.md                       # This documentation
```

---

## APIs Used

1. **Google Maps API**:
   - Places API for address autocomplete.
   - Geocoding API to get latitude and longitude for addresses.

2. **Overpass API**:
   - Fetches road data within specified radii for noise calculation.

---

## Noise Level Calculation

Noise levels are estimated using:
- Road types (`residential`, `motorway`, `secondary`.).
- A formula for noise drop-off based on distance:
  \[
  \text{Noise Level (dB)} = \text{Reference Noise} - 20 \cdot \log_{10}(\text{Distance})
  \]

---

## Known Issues and Limitations

- **API Key Restrictions**: Ensure your Google Maps API key has permissions for Places and Geocoding APIs.
- **API Rate Limits**: Overpass API has usage limits; excessive requests may result in temporary blocks.
- **Noise Calculation Accuracy**: Assumes a simplified noise propagation model.

---

## Future Improvements

1. Add support for more road types and custom noise data.
2. Integrate a map view for visualizing nearby roadways.
3. Provide advanced noise analysis using environmental datasets.

---

## Contact

For questions or feedback, please reach out to:
- **Author**: [Hayden Balsys](mailto:haydenbalsys@gmail.com) 
- **GitHub**: [haydenbalsys](https://github.com/haydenbalsys)

Enjoy using the application! 🌍
