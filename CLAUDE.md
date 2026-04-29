# CLAUDE.md — API Test Data Generator

## Project Context

This project automates the generation of X++ test data scripts for D365 F&O API testing.
Given an API payload and class name, Claude traces the source code, resolves all dependencies,
and generates ready-to-execute runnable classes.

**Domain:** Utility & Energy (Nordic)  
**Naming convention:** `TST_` prefix for all test-created data  

---

## Paths

| Purpose | Path |
|---|---|
| API source code (read-only) | `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\` |
| Generated runnable classes (output) | `J:\AosService\PackagesLocalDirectory\APITestDataGen\APITestDataGen\AxClass\` |
| Sample payloads | `<repo_root>\TestData\payloads\` |
| Dependency registry | `<repo_root>\TestData\registry.json` |

---

## Commands

### `/analyze <payload_file> <APIClassName>`

1. Read the payload from `TestData/payloads/<payload_file>`
2. Locate `<APIClassName>` in `Metadata/APITestDataGen/APITestDataGen/AxClass/`
3. Trace the call chain: API class → Entity class → table reads/writes
4. Extract all field values from the payload that map to D365 tables
5. Classify each dependency as:
   - `SCRIPT` — must be created by the setup script
   - `PRE_EXISTING` — assumed to exist (standard D365 data, companies, countries etc.)
6. Print the full dependency list
7. Update `TestData/registry.json` with any new field → table mappings discovered

### `/generate`

Uses the output of the last `/analyze`. Produces two runnable classes in `Metadata/APITestDataGen/APITestDataGen/AxClass/`:

- `TST_Setup_<APIClassName>.xml` — creates all `SCRIPT` dependencies
- `TST_Teardown_<APIClassName>.xml` — deletes exactly what setup created, in reverse order

**Runnable class rules:**
- Class must extend nothing, implement `main(Args _args)` as entry point
- Always check existence before insert (guard with `if (!record.RecId)`)
- Always use `buf2Buf` from an existing record of the same type as the template
- Log each action with `info(strFmt(...))`
- Teardown must only delete records with `TST_` prefix — never touch pre-existing data

### `/patch <error_log_file>`

1. Read the error from `TestData/<error_log_file>`
2. Identify the missing dependency from the error message
3. Re-trace source code to find the correct table
4. Update both setup and teardown scripts
5. Update `TestData/registry.json`
6. If the dependency cannot be resolved from source, flag it explicitly — do not guess

---

## Registry Format (`TestData/registry.json`)

```json
{
  "mappings": [
    {
      "apiField": "CustGroup",
      "d365Table": "CustGroup",
      "classification": "SCRIPT",
      "discoveredFrom": "EGS_API_newCustomer"
    }
  ]
}
```

---

## Conventions

- All test data values use `TST_` prefix
- Generated class names follow `TST_Setup_<APIClassName>` and `TST_Teardown_<APIClassName>`
- Never hardcode `DataAreaId` — read from context or payload
- Financial dimensions and standard reference data (countries, companies) are always `PRE_EXISTING`
- When in doubt about a classification, ask — do not assume
