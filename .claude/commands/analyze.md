# /analyze

Analyze an API payload and resolve all D365 dependencies by tracing source code.

## Usage
```
/analyze <payload_file> <APIClassName>
```

## Steps

1. Read the payload from `<repo_root>\TestData\payloads\<payload_file>`
2. Locate `<APIClassName>.xml` in `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\`
3. Trace the full call chain: API class → Entity class → table reads/writes
4. Extract all field values from the payload that reference D365 tables
5. Classify each dependency:
   - `SCRIPT` — must be created by setup script (e.g. CustGroup, PaymMode)
   - `PRE_EXISTING` — standard D365 data, assumed to exist (e.g. countries, companies, financial dimensions)
6. Print the full dependency list in this format:

```
Field         | Value          | Table               | Classification
CustGroup     | XAPI_CustGrp    | CustGroup           | SCRIPT
PaymMode      | XAPI_PaymMd     | CustPaymModeTable   | SCRIPT
CountryRegion | SWE            | LogisticsAddress... | PRE_EXISTING
```

7. Update `<repo_root>\TestData\registry.json` with any new field → table mappings
8. Confirm ready to run `/generate`
