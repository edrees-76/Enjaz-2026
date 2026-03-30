import requests
from requests.auth import HTTPBasicAuth
import re

BASE_URL = "http://localhost:8080"
AUTH = HTTPBasicAuth('admin', '12345')
HEADERS = {"Content-Type": "application/json"}
TIMEOUT = 30

def test_edit_existing_certificate():
    # First create a certificate to edit
    create_payload = {
        "name": "Original Certificate",
        "issuer": "Original Issuer",
        "validFrom": "2024-01-01",
        "validTo": "2025-01-01",
        "serialNumber": "1234567890",
        "description": "Original description"
    }

    cert_id = None
    try:
        create_resp = requests.post(
            f"{BASE_URL}/certificates",
            auth=AUTH,
            headers=HEADERS,
            json=create_payload,
            timeout=TIMEOUT
        )
        assert create_resp.status_code in (200,201), f"Expected 201 or 200, got {create_resp.status_code}"

        # Try to parse json body for id
        try:
            created_cert = create_resp.json()
            cert_id = created_cert.get("id")
        except Exception:
            created_cert = None
            cert_id = None

        # If no id from body, try extract from Location header
        if cert_id is None:
            location = create_resp.headers.get('Location')
            if location:
                # Assuming location ends with /{id}
                match = re.search(r'/([^/]+)$', location)
                if match:
                    cert_id = match.group(1)

        assert cert_id is not None, "Created certificate id is None"

        # Now edit the existing certificate
        edit_payload = {
            "name": "Edited Certificate",
            "issuer": "Edited Issuer",
            "validFrom": "2024-06-01",
            "validTo": "2026-06-01",
            "serialNumber": "9876543210",
            "description": "Edited description"
        }

        edit_resp = requests.put(
            f"{BASE_URL}/certificates/{cert_id}",
            auth=AUTH,
            headers=HEADERS,
            json=edit_payload,
            timeout=TIMEOUT
        )
        assert edit_resp.status_code == 200, f"Expected 200, got {edit_resp.status_code}"
        updated_cert = edit_resp.json()
        assert updated_cert["id"] == cert_id
        assert updated_cert["name"] == edit_payload["name"]
        assert updated_cert["issuer"] == edit_payload["issuer"]
        assert updated_cert["validFrom"] == edit_payload["validFrom"]
        assert updated_cert["validTo"] == edit_payload["validTo"]
        assert updated_cert["serialNumber"] == edit_payload["serialNumber"]
        assert updated_cert["description"] == edit_payload["description"]

        # Confirm that the changes persisted by retrieving certificate again
        get_resp = requests.get(
            f"{BASE_URL}/certificates/{cert_id}",
            auth=AUTH,
            headers=HEADERS,
            timeout=TIMEOUT
        )
        assert get_resp.status_code == 200, f"Expected 200, got {get_resp.status_code}"
        cert_from_db = get_resp.json()
        assert cert_from_db == updated_cert

    finally:
        if cert_id:
            del_resp = requests.delete(
                f"{BASE_URL}/certificates/{cert_id}",
                auth=AUTH,
                headers=HEADERS,
                timeout=TIMEOUT
            )
            assert del_resp.status_code in (200,204)

test_edit_existing_certificate()
