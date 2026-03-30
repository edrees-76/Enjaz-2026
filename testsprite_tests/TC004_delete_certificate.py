import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
AUTH = HTTPBasicAuth("admin", "12345")
HEADERS = {"Content-Type": "application/json"}
TIMEOUT = 30

def test_delete_certificate():
    # Step 1: Create a new certificate to delete
    create_payload = {
        # Assuming typical certificate fields, adapt if known
        "name": "Test Certificate",
        "issuer": "Test Issuer",
        "validFrom": "2024-01-01T00:00:00Z",
        "validTo": "2025-01-01T00:00:00Z",
        "serialNumber": "1234567890"
    }

    certificate_id = None
    try:
        create_response = requests.post(
            f"{BASE_URL}/certificates",
            json=create_payload,
            auth=AUTH,
            headers=HEADERS,
            timeout=TIMEOUT
        )
        assert create_response.status_code in [200, 201], f"Expected 201 Created or 200 OK, got {create_response.status_code}"
        try:
            json_create = create_response.json()
        except ValueError:
            assert False, "Create certificate response is not valid JSON"

        certificate_id = json_create.get("id")
        assert certificate_id is not None, "Created certificate response missing 'id'"

        # Step 2: Delete the certificate
        delete_response = requests.delete(
            f"{BASE_URL}/certificates/{certificate_id}",
            auth=AUTH,
            headers=HEADERS,
            timeout=TIMEOUT
        )
        assert delete_response.status_code == 204, f"Expected 204 No Content, got {delete_response.status_code}"

        # Step 3: Verify the certificate is deleted by attempting to get it
        get_response = requests.get(
            f"{BASE_URL}/certificates/{certificate_id}",
            auth=AUTH,
            headers=HEADERS,
            timeout=TIMEOUT
        )
        # Expecting 404 Not Found since it should be deleted
        assert get_response.status_code == 404, (
            f"Expected 404 Not Found after deletion, got {get_response.status_code}"
        )

    finally:
        # Cleanup if certificate still exists (in case deletion failed)
        if certificate_id is not None:
            requests.delete(
                f"{BASE_URL}/certificates/{certificate_id}",
                auth=AUTH,
                headers=HEADERS,
                timeout=TIMEOUT
            )

test_delete_certificate()
