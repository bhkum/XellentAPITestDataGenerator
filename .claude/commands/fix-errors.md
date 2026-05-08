# /fix-errors

Read the user-populated `TestData/Errors.md`, classify each failed-row entry for the given API, and resolve the underlying cause — typically by updating the setup script, teardown script, or registry mapping. Append a `Resolution` line to each entry once fixed; never delete prior content.

## Usage
```
/fix-errors <APIClassName>
```

## Precondition
- The user has populated `TestData/Errors.md` with one or more entries for `<APIClassName>` (format in `CLAUDE_PostProcess.md`).
- `<APIClassName>` already has a `postProcess` registry entry (from `/check-response` + `/find-batch` + `/check-status`).

## Steps

1. Read `TestData/Errors.md`. Filter entries where the heading API class name matches `<APIClassName>`. Process the most recent entries first (timestamps are newest-first).
2. For each entry, classify the failure:
   - **Missing dependency** (e.g. "X not found in CustTable", "Customer XAPI_CUST_001 not found"). Re-trace source code in `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\` to find the table the dependency lives in. Same logic as `/patch`.
   - **Validation rejection** (e.g. "DeliveryCategory must be Agreement"). Trace the validation site in the batch class; identify the field on the staging table that drives it; map back to the payload field; map back to setup-script values.
   - **Reference-data mismatch** (e.g. wrong `companyId`, missing `EGS_MASS_ConvInString` row). Re-trace conversion lookup tables.
   - **Unrecognised** — do not guess. Flag and ask the user.
3. For each classified failure, propose the fix — typically one of:
   - Update `Metadata\APITestDataGen\APITestDataGen\AxClass\XAPI_Setup_<APIClassName>.xml` to create the missing dependency before the API call.
   - Update `Metadata\APITestDataGen\APITestDataGen\AxClass\XAPI_Teardown_<APIClassName>.xml` to remove the new dependency afterwards.
   - Update `TestData\registry.json` `mappings` with a new field → table entry so future `/analyze` runs know about it.
   - Update the payload (only if the payload itself is malformed; rare).
4. Print the proposed fixes and ask the user to confirm before applying. Apply each accepted fix.
5. For every resolved entry, append a `Resolution` line in `Errors.md`:
```markdown
- **Resolution (<YYYY-MM-DD>):** <what was changed> (e.g. setup script now creates X record, registry mapping added for Y → Z table)
```
   **Do not delete the entry.** `Errors.md` is an append-only log.
6. After applying fixes, print the user's next-action list:
```
Next steps:
  1. Re-run XAPI_Teardown_<APIClassName> in D365 to clean stale staging rows
     (any rows still in EGS_MASS_Input with Status = ERROR).
  2. Re-run XAPI_Setup_<APIClassName>.
  3. Re-call the API.
  4. Re-run the batch from its menu path (see registry.batchMenuPath).
  5. Re-run /check-status <APIClassName> to confirm no further errors.
```

## Output

For each entry processed:

```
Errors.md entry: <timestamp> — <APIClassName>
  Classification: missing dependency
  Root cause:     CustTable record for AccountNum=XAPI_CUST_001 was not created
                  by setup. EGS_API_newAgreement reads CustTable in
                  EGS_MASS_ImportCustInst.xml:<line>.
  Proposed fix:
    - Edit XAPI_Setup_EGS_API_newAgreement.xml: insert a CustTable record
      with AccountNum=XAPI_CUST_001 before the agreement payload is sent.
    - Edit XAPI_Teardown_EGS_API_newAgreement.xml: delete the same record.
    - Add registry mapping: AlternativeCustomerNumber → CustTable.

Apply this fix? (y/n)
```

After all entries:

```
Resolutions appended to TestData/Errors.md.
Next steps printed above. Re-run /check-status when the batch has been re-run.
```

## If a failure cannot be resolved from source
- Do NOT guess.
- Flag explicitly:
```
⚠ Could not resolve error "<verbatim>" for <APIClassName> — manual investigation required
```
- Append a `Resolution (<date>): UNRESOLVED — <reason>` line so the entry is not retried blindly.
