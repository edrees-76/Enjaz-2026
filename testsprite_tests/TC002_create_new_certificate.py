import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
TIMEOUT = 30
AUTH = HTTPBasicAuth('admin', '12345')

def test_create_new_certificate():
    certificate_payload = {
        "name": "Test Certificate",
        "issuer": "Test Issuer",
        "validFrom": "2024-01-01T00:00:00Z",
        "validTo": "2025-01-01T00:00:00Z",
        "serialNumber": "1234567890",
        "publicKey": "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnExampleKey==",
        "signatureAlgorithm": "SHA256withRSA"
    }
    headers = {
        "Content-Type": "application/json"
    }

    created_cert_id = None
    try:
        # Create certificate
        response = requests.post(
            f"{BASE_URL}/api/certificates",
            json=certificate_payload,
            headers=headers,
            auth=AUTH,
            timeout=TIMEOUT
        )
        assert response.status_code == 200, f"Expected 200 OK, got {response.status_code}. Response text: {response.text}"
        try:
            resp_json = response.json()
        except Exception as e:
            assert False, f"Response is not valid JSON: {e}. Response text: {response.text}"

        assert "id" in resp_json, "Response JSON missing 'id'"
        created_cert_id = resp_json["id"]

        # Retrieve certificate to verify persistence
        get_resp = requests.get(
            f"{BASE_URL}/api/certificates/{created_cert_id}",
            auth=AUTH,
            timeout=TIMEOUT
        )
        assert get_resp.status_code == 200, f"Expected 200 OK on get, got {get_resp.status_code}"
        cert_data = get_resp.json()
        assert cert_data["name"] == certificate_payload["name"]
        assert cert_data["issuer"] == certificate_payload["issuer"]
        assert cert_data["serialNumber"] == certificate_payload["serialNumber"]
        assert cert_data["signatureAlgorithm"] == certificate_payload["signatureAlgorithm"]

    finally:
        if created_cert_id:
            requests.delete(
                f"{BASE_URL}/api/certificates/{created_cert_id}",
                auth=AUTH,
                timeout=TIMEOUT
            )

test_create_new_certificate()
