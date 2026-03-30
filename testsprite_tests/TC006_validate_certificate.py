import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
AUTH = HTTPBasicAuth('admin', '12345')
HEADERS = {"Content-Type": "application/json"}
TIMEOUT = 30

def test_validate_certificate():
    # First, create a new certificate resource to validate
    create_payload = {
        "name": "Test Certificate",
        "issuer": "Test Issuer",
        "validFrom": "2024-01-01T00:00:00Z",
        "validTo": "2025-01-01T00:00:00Z",
        "serialNumber": "1234567890ABCDEF",
        "publicKey": "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...",
        "signatureAlgorithm": "SHA256withRSA"
    }
    cert_id = None
    try:
        create_resp = requests.post(
            f"{BASE_URL}/api/certificates",
            auth=AUTH,
            headers=HEADERS,
            json=create_payload,
            timeout=TIMEOUT
        )
        assert create_resp.status_code == 201, f"Certificate creation failed: {create_resp.text}"
        cert_data = create_resp.json()
        cert_id = cert_data.get("id")
        assert cert_id is not None, "Created certificate ID is missing."

        # Validate the created certificate
        validate_resp = requests.post(
            f"{BASE_URL}/api/certificates/{cert_id}/validate",
            auth=AUTH,
            headers=HEADERS,
            timeout=TIMEOUT
        )
        assert validate_resp.status_code == 200, f"Validation request failed: {validate_resp.text}"

        validation_result = validate_resp.json()
        # Assuming validation response contains a boolean field 'isValid' and a message
        assert "isValid" in validation_result, "'isValid' field missing in validation response"
        assert isinstance(validation_result["isValid"], bool), "'isValid' should be a boolean"
        assert validation_result["isValid"] is True, "Certificate validation failed when it should succeed"

    finally:
        # Cleanup - delete the created certificate if it exists
        if cert_id:
            delete_resp = requests.delete(
                f"{BASE_URL}/api/certificates/{cert_id}",
                auth=AUTH,
                headers=HEADERS,
                timeout=TIMEOUT
            )
            assert delete_resp.status_code in (200, 204), f"Failed to delete certificate: {delete_resp.text}"

test_validate_certificate()