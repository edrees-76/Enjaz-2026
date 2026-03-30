import requests
from requests.auth import HTTPBasicAuth

def test_view_reporting_dashboard():
    base_url = "http://localhost:8080"
    endpoint = f"{base_url}/api/reporting/dashboard"
    auth = HTTPBasicAuth("admin", "12345")
    headers = {
        "Accept": "application/json"
    }
    try:
        response = requests.get(endpoint, auth=auth, headers=headers, timeout=30)
        response.raise_for_status()  # Raises HTTPError for bad responses
        data = response.json()

        # Validate presence of charts data key
        assert "charts" in data, "Response JSON should contain 'charts' key"
        charts = data["charts"]

        # Validate that there are exactly 6 interactive charts
        assert isinstance(charts, list), "'charts' should be a list"
        assert len(charts) == 6, "There should be 6 interactive charts in the dashboard"

        # Validate each chart has expected keys and data types (example)
        for chart in charts:
            assert "title" in chart, "Each chart should have a 'title'"
            assert "data" in chart, "Each chart should have 'data'"
            
            title = chart["title"]
            data_points = chart["data"]
            
            assert isinstance(title, str) and title, "Chart title should be a non-empty string"
            assert isinstance(data_points, list), "Chart data should be a list"
            assert len(data_points) > 0, f"Chart '{title}' should have at least one data point"
            
            # Each data point should be a dict with numeric values (example validation)
            for point in data_points:
                assert isinstance(point, dict), "Each data point should be a dictionary"
                # Check if at least one numeric value exists in the point dictionary
                numeric_values = [v for v in point.values() if isinstance(v, (int, float))]
                assert numeric_values, "Each data point should contain at least one numeric value"
    except requests.exceptions.HTTPError as http_err:
        assert False, f"HTTP error occurred: {http_err}"
    except requests.exceptions.RequestException as req_err:
        assert False, f"Request error occurred: {req_err}"
    except ValueError as json_err:
        assert False, f"JSON decoding failed: {json_err}"

test_view_reporting_dashboard()