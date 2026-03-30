import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
TIMEOUT = 30
AUTH = HTTPBasicAuth("admin", "12345")

def test_update_application_settings():
    headers = {
        "Content-Type": "application/json"
    }

    # Prepare settings payload to update
    update_payload = {
        "theme": "dark",
        "fontScaling": 1.25,
        "databaseBackupEnabled": True
    }

    try:
        # Update application settings
        response = requests.put(
            f"{BASE_URL}/api/settings",
            auth=AUTH,
            json=update_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert response.status_code == 200, f"Expected 200 OK, got {response.status_code}"

        # Do not assume response has JSON content; skip JSON decode on update response
        assert response.content is not None, "No response content from settings update"

        # Retrieve settings to verify persistence
        get_response = requests.get(
            f"{BASE_URL}/api/settings",
            auth=AUTH,
            headers=headers,
            timeout=TIMEOUT
        )
        assert get_response.status_code == 200, f"Expected 200 OK on get settings, got {get_response.status_code}"
        assert get_response.content, "Empty response content from settings get"
        persisted_settings = get_response.json()
        assert persisted_settings.get("theme") == update_payload["theme"], "Theme setting did not persist after update"
        assert abs(persisted_settings.get("fontScaling", 0) - update_payload["fontScaling"]) < 0.0001, "Font scaling did not persist after update"
        assert persisted_settings.get("databaseBackupEnabled") == update_payload["databaseBackupEnabled"], "Database backup setting did not persist after update"

    except requests.exceptions.RequestException as e:
        assert False, f"Request failed: {e}"

test_update_application_settings()
