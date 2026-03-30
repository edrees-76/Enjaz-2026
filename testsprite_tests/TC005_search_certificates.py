import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
AUTH = HTTPBasicAuth("admin", "12345")
TIMEOUT = 30

def test_search_certificates():
    search_endpoint = f"{BASE_URL}/certificates/search"
    headers = {
        "Accept": "application/json"
    }

    # Example search criteria payload (adjust fields as required by actual API)
    search_payload = {
        "name": "Test Certificate",
        "issuer": "Test Issuer",
        "validFrom": "2023-01-01",
        "validTo": "2025-12-31",
        "status": "active"
    }

    try:
        response = requests.post(
            search_endpoint,
            json=search_payload,
            headers=headers,
            auth=AUTH,
            timeout=TIMEOUT
        )
        response.raise_for_status()
    except requests.exceptions.RequestException as e:
        assert False, f"Request to search certificates failed: {e}"

    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"
    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert isinstance(data, list), "Response JSON should be a list of certificates"
    for cert in data:
        assert isinstance(cert, dict), "Each certificate entry should be a dictionary"
        # Validate that the certificate matches search criteria fields if present
        if "name" in cert and search_payload["name"]:
            assert search_payload["name"].lower() in cert.get("name", "").lower()
        if "issuer" in cert and search_payload["issuer"]:
            assert search_payload["issuer"].lower() in cert.get("issuer", "").lower()
        if "status" in cert and search_payload["status"]:
            assert search_payload["status"].lower() == cert.get("status", "").lower()
        # Assuming validFrom and validTo are date strings in ISO format
        if "validFrom" in cert and search_payload["validFrom"]:
            assert cert.get("validFrom", "") >= search_payload["validFrom"]
        if "validTo" in cert and search_payload["validTo"]:
            assert cert.get("validTo", "") <= search_payload["validTo"]

test_search_certificates()