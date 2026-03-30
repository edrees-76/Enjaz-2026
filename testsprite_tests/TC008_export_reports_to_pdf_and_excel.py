import requests
from requests.auth import HTTPBasicAuth

BASE_URL = "http://localhost:8080"
TIMEOUT = 30
AUTH = HTTPBasicAuth('admin', '12345')

def test_export_reports_to_pdf_and_excel():
    # Export report to PDF
    pdf_url = f"{BASE_URL}/api/reports/export/pdf"
    try:
        pdf_response = requests.get(pdf_url, auth=AUTH, headers={"Accept": "application/pdf"}, timeout=TIMEOUT)
        pdf_response.raise_for_status()
        assert pdf_response.status_code == 200
        content_type = pdf_response.headers.get("Content-Type", "")
        assert "application/pdf" in content_type.lower(), f"Expected 'application/pdf' in Content-Type but got {content_type}"
        assert len(pdf_response.content) > 0
    except requests.RequestException as e:
        assert False, f"PDF export request failed: {e}"

    # Export report to Excel
    excel_url = f"{BASE_URL}/api/reports/export/excel"
    try:
        excel_response = requests.get(excel_url, auth=AUTH, headers={"Accept": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"}, timeout=TIMEOUT)
        excel_response.raise_for_status()
        assert excel_response.status_code == 200
        content_type = excel_response.headers.get("Content-Type", "")
        content_type_lower = content_type.lower()
        assert content_type_lower.startswith("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") or content_type_lower.startswith("application/vnd.ms-excel"), \
            f"Expected Excel MIME type in Content-Type but got {content_type}"
        assert len(excel_response.content) > 0
    except requests.RequestException as e:
        assert False, f"Excel export request failed: {e}"

test_export_reports_to_pdf_and_excel()
