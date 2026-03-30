
# TestSprite AI Testing Report(MCP)

---

## 1️⃣ Document Metadata
- **Project Name:** منظومة شهادات جديدة 2026 جديدة
- **Date:** 2025-12-31
- **Prepared by:** TestSprite AI Team

---

## 2️⃣ Requirement Validation Summary

#### Test TC001
- **Test Name:** user login with valid credentials
- **Test Code:** [TC001_user_login_with_valid_credentials.py](./TC001_user_login_with_valid_credentials.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 17, in test_user_login_with_valid_credentials
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 27, in <module>
  File "<string>", line 19, in test_user_login_with_valid_credentials
AssertionError: Response is not valid JSON

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/11da30f1-cd6a-4264-a1aa-bcc6270e13ac
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC002
- **Test Name:** create new certificate
- **Test Code:** [TC002_create_new_certificate.py](./TC002_create_new_certificate.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 34, in test_create_new_certificate
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 62, in <module>
  File "<string>", line 36, in test_create_new_certificate
AssertionError: Response is not valid JSON: Expecting value: line 1 column 1 (char 0). Response text: TestSprite Dummy Server Running

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/e3ef7fe5-6a2b-4419-99ae-c3ad7f7f0f46
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC003
- **Test Name:** edit existing certificate
- **Test Code:** [TC003_edit_existing_certificate.py](./TC003_edit_existing_certificate.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 99, in <module>
  File "<string>", line 49, in test_edit_existing_certificate
AssertionError: Created certificate id is None

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/54439f5a-df8c-4c6b-977e-db84893af431
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC004
- **Test Name:** delete certificate
- **Test Code:** [TC004_delete_certificate.py](./TC004_delete_certificate.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 31, in test_delete_certificate
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 69, in <module>
  File "<string>", line 33, in test_delete_certificate
AssertionError: Create certificate response is not valid JSON

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/c8f5b037-4aa3-482d-abc1-2f1c12a51125
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC005
- **Test Name:** search certificates
- **Test Code:** [TC005_search_certificates.py](./TC005_search_certificates.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 37, in test_search_certificates
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 57, in <module>
  File "<string>", line 39, in test_search_certificates
AssertionError: Response is not valid JSON

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/d03c9fb5-5866-4475-93e1-a8dd6b08306d
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC006
- **Test Name:** validate certificate
- **Test Code:** [TC006_validate_certificate.py](./TC006_validate_certificate.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 60, in <module>
  File "<string>", line 29, in test_validate_certificate
AssertionError: Certificate creation failed: TestSprite Dummy Server Running

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/44df7fad-9b29-4319-8650-32b80574b7fd
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC007
- **Test Name:** view reporting dashboard
- **Test Code:** [TC007_view_reporting_dashboard.py](./TC007_view_reporting_dashboard.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 14, in test_view_reporting_dashboard
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 49, in <module>
  File "<string>", line 45, in test_view_reporting_dashboard
AssertionError: Request error occurred: Expecting value: line 1 column 1 (char 0)

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/db094065-9d26-4ece-8fc9-7637af26add4
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC008
- **Test Name:** export reports to pdf and excel
- **Test Code:** [TC008_export_reports_to_pdf_and_excel.py](./TC008_export_reports_to_pdf_and_excel.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 35, in <module>
  File "<string>", line 16, in test_export_reports_to_pdf_and_excel
AssertionError: Expected 'application/pdf' in Content-Type but got text/plain

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/2c74fd74-f980-42b1-bc5c-0f8611cbb2bd
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC009
- **Test Name:** update application settings
- **Test Code:** [TC009_update_application_settings.py](./TC009_update_application_settings.py)
- **Test Error:** Traceback (most recent call last):
  File "/var/task/requests/models.py", line 974, in json
    return complexjson.loads(self.text, **kwargs)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/__init__.py", line 514, in loads
    return _default_decoder.decode(s)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 386, in decode
    obj, end = self.raw_decode(s)
               ^^^^^^^^^^^^^^^^^^
  File "/var/lang/lib/python3.12/site-packages/simplejson/decoder.py", line 416, in raw_decode
    return self.scan_once(s, idx=_w(s, idx).end())
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
simplejson.errors.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "<string>", line 43, in test_update_application_settings
  File "/var/task/requests/models.py", line 978, in json
    raise RequestsJSONDecodeError(e.msg, e.doc, e.pos)
requests.exceptions.JSONDecodeError: Expecting value: line 1 column 1 (char 0)

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/var/task/handler.py", line 258, in run_with_retry
    exec(code, exec_env)
  File "<string>", line 51, in <module>
  File "<string>", line 49, in test_update_application_settings
AssertionError: Request failed: Expecting value: line 1 column 1 (char 0)

- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/21b93385-f5b4-4173-832f-6688d5c02f8a
- **Status:** ❌ Failed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---

#### Test TC010
- **Test Name:** database backup functionality
- **Test Code:** [TC010_database_backup_functionality.py](./TC010_database_backup_functionality.py)
- **Test Visualization and Result:** https://www.testsprite.com/dashboard/mcp/tests/5db92f1c-6b90-4197-bfb7-2ceec7ee51a9/91454dce-9663-4b17-9127-7b40ac286b84
- **Status:** ✅ Passed
- **Analysis / Findings:** {{TODO:AI_ANALYSIS}}.
---


## 3️⃣ Coverage & Matching Metrics

- **10.00** of tests passed

| Requirement        | Total Tests | ✅ Passed | ❌ Failed  |
|--------------------|-------------|-----------|------------|
| ...                | ...         | ...       | ...        |
---


## 4️⃣ Key Gaps / Risks
{AI_GNERATED_KET_GAPS_AND_RISKS}
---