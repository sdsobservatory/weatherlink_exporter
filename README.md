# David WeatherLink Prometheus Exporter
Query the public WeatherLink API and convert the data to a form prometheus can read.

# Configuration
Set the `WeatherLinkUrl` environment variable.
This url comes from the public facing WeatherLink page, such as https://www.weatherlink.com/embeddablePage/show/793e175310484a32a2d92b573fcca207/summary.
Use the network tab in developer tools to look for the url that returns a json.

# Usage
Metrics are available on port 9100 at `/metrics`.

Additionally, there are two HTTP endpoints on port 8080:

  - `/raw`
  - `/weather`
