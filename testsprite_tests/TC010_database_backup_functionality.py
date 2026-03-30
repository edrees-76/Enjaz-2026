import requests
from requests.auth import HTTPBasicAuth

def test_database_backup_functionality():
    base_url = "http://localhost:8080"
    backup_endpoint = f"{base_url}/api/settings/backup"
    auth = HTTPBasicAuth('admin', '12345')
    headers = {
        "Accept": "application/json"
    }
    try:
        response = requests.post(backup_endpoint, auth=auth, headers=headers, timeout=30)
        assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"
        if response.content and response.headers.get('Content-Type', '').startswith('application/json'):
            json_response = response.json()
            assert 'success' in json_response, "Response JSON missing 'success' key"
            assert json_response['success'] is True, "Backup success flag is False"
            assert 'message' in json_response, "Response JSON missing 'message' key"
            assert "backup" in json_response['message'].lower(), "Backup message does not indicate success"
        else:
            # No JSON content, assume success due to 200 status code
            pass
    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_database_backup_functionality()
