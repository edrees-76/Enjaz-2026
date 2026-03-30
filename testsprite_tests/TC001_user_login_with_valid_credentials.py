import requests
from requests.auth import HTTPBasicAuth

def test_user_login_with_valid_credentials():
    base_url = "http://localhost:8080"
    login_endpoint = f"{base_url}/api/login"
    auth = HTTPBasicAuth("admin", "12345")
    timeout = 30

    try:
        response = requests.post(login_endpoint, auth=auth, timeout=timeout)
    except requests.RequestException as e:
        assert False, f"Request to login endpoint failed: {e}"

    assert response.status_code == 200, f"Expected status code 200 but got {response.status_code}"
    try:
        json_data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert "token" in json_data and isinstance(json_data["token"], str) and json_data["token"], "Authentication token not found or empty in response"
    assert "role" in json_data and isinstance(json_data["role"], str) and json_data["role"], "User role not found or empty in response"

    # Optionally verify role-based access value (e.g., admin)
    assert json_data["role"].lower() == "admin", f"Expected role 'admin' but got '{json_data['role']}'"

test_user_login_with_valid_credentials()