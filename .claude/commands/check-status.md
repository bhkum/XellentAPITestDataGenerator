# /check-status

Identify where the post-process batch records its outcome and tell the user which form to open. Records reference data only — no runnable class is generated and Claude does not query D365.

After the user inspects the form they copy any failed rows into `TestData/Errors.md` (format in `CLAUDE_PostProcess.md`). `/fix-errors` then resolves them.

## Usage
```
/check-status <APIClassName>
```

## Precondition
`/find-batch <APIClassName>` has run and recorded `batchClass` in `<repo_root>\TestData\registry.json`. The user has already executed the batch in D365 (via the menu path that `/find-batch` printed).

## Steps

1. Open the batch class identified by `/find-batch`. Trace where it writes its outcome:
   - Successful processing → which **result table** gets the new business record (e.g. `EXU_AgreementTable` for `EGS_MASS_ImportCustInst`)
   - Per-row status → which table/field records pass/fail per staging row (often the staging table itself gains a `Status` / `Treated` flag, or a sibling log table)
   - Errors → which **field** on the per-row status table holds the verbatim error message (e.g. `EGS_MASS_Input.Err_Msg`)
2. Identify the **form** humans use to inspect this — typically the form bound to the per-row status table or its log header, often labelled "treated posts", "import log", or similar. Capture the menu item name and the menu path (`Module > Inquiries > …`) by searching `AxMenuItemDisplay` and `AxMenu` metadata.
3. Capture which field/value combination indicates success vs. failure (e.g. `EGS_MASS_Input.Status == PROCESSED` for success, `ERROR` for failure). Capture the error-message field name.
4. Update the matching `postProcess` entry in `<repo_root>\TestData\registry.json` with:
   - `statusTable`
   - `statusField`
   - `successValues`
   - `failureValues`
   - `errorMessageField`
   - `resultTable`
   - `statusForm`
   - `statusMenuItem`
   - `statusMenuPath`

## Output

Print a summary that tells the user **where to look and what to copy**:

```
Status check for EGS_API_newAgreement

  Where the values are populated:
    Result table:   EXU_AgreementTable (filter: AgreementId starts with XAPI_)
    Status table:   EGS_MASS_Input
    Status field:   Status   (success = PROCESSED|WARNING; failure = ERROR)
    Error field:    Err_Msg  (verbatim message per failed row)

  Open in D365:
    Menu path:   EXU Billing > Import > The agreementinformation > Treated posts
    Menu item:   EGS_MASS_Inlog
    Form:        EGS_MASS_Inlog (drill to per-row EGS_MASS_Input)

  How to record errors:
    For every row where Status = ERROR and the row was produced by setup
    (XAPI_-prefixed identifier), copy the row into TestData/Errors.md
    using the format documented in CLAUDE_PostProcess.md.
    Then run /fix-errors <APIClassName>.
```

## No runnable class is generated
The status check is reference data; humans verify via the form. Claude does not query D365.

## If a value cannot be resolved from source
- Do NOT guess.
- Flag explicitly:
```
⚠ Could not resolve <field> for <APIClassName> — manual investigation required
```
