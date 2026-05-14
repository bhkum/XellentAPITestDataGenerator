# /analyze

Analyze an API payload and resolve all D365 dependencies by tracing source code.

## Usage
```
/analyze <payload_file> <APIClassName>
```

## HARD RULES — must be followed before completing analysis
- Do NOT complete `/analyze` until every `SCRIPT` field has a confirmed `MaxLen` and `FinalValue`
- Do NOT allow `/generate` to proceed if any `SCRIPT` field is missing `MaxLen` or `FinalValue`
- If EDT cannot be found, stop and flag explicitly — do not skip or guess

## Steps

1. Read the payload from `<repo_root>\TestData\payloads\<payload_file>`
2. Locate `<APIClassName>.xml` in `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\`
3. Trace the full call chain: API class → Entity class → table reads/writes
4. Extract all field values from the payload that reference D365 tables
5. Classify each dependency:
   - `SCRIPT` — must be created by setup script
   - `PRE_EXISTING` — standard D365 data, assumed to exist

6. **EDT Length Check — mandatory for every `SCRIPT` field:**
   a. Read `<repo_root>\TestData\registry.json` — if `MaxLen` already exists for this field, use cached value and skip to step (f)
   b. Identify the field's EDT name from the table XML in `AxTable\` across all packages under `J:\AosService\PackagesLocalDirectory\`
   c. Search all packages under `J:\AosService\PackagesLocalDirectory\` for that EDT in `AxEdt\`
   d. Read `StringSize` from the EDT XML
   e. Write `EDTName` and `MaxLen` to `registry.json` for this field — cache it for future sessions
   f. Read `testDataPrefix` from `<repo_root>\TestData\xapi.config.json`
   g. Calculate `FinalValue = testDataPrefix + <shortened field value>` that fits within `MaxLen`
   h. If truncation was needed, log a warning:
      ```
      ⚠ Field <FieldName> EDT <EDTName> max length <MaxLen> — value truncated to <FinalValue>
      ```

7. Print the full dependency list — **do not proceed until this table is complete:**

```
Field         | Value           | Table               | Classification | MaxLen | FinalValue
CustGroup     | TST_CustGrp     | CustGroup           | SCRIPT         | 10     | TST_CstGp
PaymMode      | TST_PaymMd      | CustPaymModeTable   | SCRIPT         | 10     | TST_PyMd
CountryRegion | SWE             | LogisticsAddress... | PRE_EXISTING   | -      | -
```

8. Explicitly confirm: **"All SCRIPT fields have MaxLen and FinalValue — ready to /generate"**
9. Update `<repo_root>\TestData\registry.json` with all new mappings including `EDTName` and `MaxLen`
