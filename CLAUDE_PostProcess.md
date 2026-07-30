# CLAUDE_PostProcess.md — API Post-Process Handling

## Purpose

Some APIs in this project complete synchronously: a 200 response carries the
created entity's data, and there is nothing left to do. Others are
fire-and-stage: the 200 response only acknowledges receipt, and a downstream
batch must run before the data is actually visible (e.g. `EGS_API_newAgreement`
writes into `EGS_MASS_Input`, then `EGS_MASS_ImportCustInst` materializes the
agreement; the result lands in a "treated posts"–style log).

This document defines four chained commands that together:

1. Decide whether an API has a post-process,
2. **Identify** the batch that must run (Claude does **not** generate a
   runnable and does **not** execute the batch — the user runs it
   themselves in D365),
3. Tell the user where to inspect the result and which form to open,
4. Read the user-populated error file and resolve the failures.

The commands extend — they do not replace — `/analyze`, `/generate`, `/patch`
defined in `CLAUDE.md`. Run them **after** `/generate` has produced the
setup/teardown classes.

> **Important — what Claude can and cannot do:**
> Claude Code runs outside D365. It cannot execute X++ code in the AOS,
> and it does not have a SQL connection to AxDB from this session. The
> commands below therefore only **identify** and **document**. The user
> performs all D365 actions (running the batch, opening the form, copying
> errors). Claude reads the error file the user produces and fixes the
> underlying source-code or registry issue.

---

## API Classification (worked examples)

| API | Response carries data? | Post-process? | Batch | Status location |
|---|---|---|---|---|
| `EGS_API_newCustomer` | Yes (CustAccount returned) | No | — | — |
| `EGS_API_newAgreement` | No (only ack) | Yes | `EGS_MASS_ImportCustInst` | "treated posts" log (to be traced) |
| `EGS_API_newContractPart` | No (only ack) | Yes | (to be traced) | (to be traced) |

The commands below automate filling in the unknowns.

---

## Paths

| Purpose | Path |
|---|---|
| API source code (read-only) | `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\` |
| Mirrored metadata (in repo) | `<repo_root>\Metadata\APITestDataGen\APITestDataGen\AxClass\` |
| Sample payloads | `<repo_root>\TestData\payloads\` |
| Registry (extended — see below) | `<repo_root>\TestData\registry.json` |
| Post-process error log (user-populated) | `<repo_root>\TestData\Errors.md` |

Note: there is intentionally no "Generated runnable classes (output)" path
under `J:\` for post-process — the post-process runnable is no longer
generated.

---

## Commands

### `/check-response <APIClassName>`

Decides whether `<APIClassName>` is synchronous (response carries the created
entity's data) or asynchronous (response is only an ack and a post-process
batch is required).

**Detection — both signals must be evaluated:**

1. **Response-shape signal.** Locate the API class in the source path. Find
   the response object it constructs (typically via a `*Response` contract
   class or a return container). Inspect which fields it populates.
   - If it returns business identifiers/values created by the call
     (e.g. `CustAccount`, `RecId`, full echo of input + generated keys) →
     `SYNC` candidate.
   - If it only returns an ack (`status`, `messageId`, empty container,
     or just the input fields echoed back without any newly-created keys) →
     `ASYNC` candidate.
2. **Code-trace confirmation.** In the same call chain, look for any of:
   - Writes to `EGS_MASS_Input` or other staging tables,
   - Calls into `EGS_MASS_*` import processors,
   - `BatchHeader`, `SysOperation`, `RunBaseBatch` invocations,
   - Enqueues onto a queue/poster table.

   If any of the above exist → confirms `ASYNC`. If none → confirms `SYNC`.

3. **Reconcile.** If signals agree → final classification. If they disagree →
   print both findings and stop; do **not** guess. Ask the user.

**Output:**

```
API: EGS_API_newAgreement
Response signal:    ASYNC  (response object only sets `status` and `correlationId`)
Code-trace signal:  ASYNC  (writes to EGS_MASS_Input at <file>:<line>)
Final classification: ASYNC — post-process required.
Next: run /find-batch EGS_API_newAgreement
```

```
API: EGS_API_newCustomer
Response signal:    SYNC   (response object populates CustAccount, RecId)
Code-trace signal:  SYNC   (no batch trigger or staging-table write found)
Final classification: SYNC — no post-process. Done.
```

**Registry effect:** none yet (the next two commands populate the entry).

---

### `/find-batch <APIClassName>`

Precondition: `/check-response` returned `ASYNC`. If it returned `SYNC`, refuse
and exit.

**This command identifies the batch only. It does not generate a runnable
and does not execute the batch.** The user runs the batch themselves in
D365 using its existing menu item.

**Steps:**

1. Re-trace the API call chain to identify the batch processor that consumes
   what the API wrote. Look for:
   - The class that reads from the staging table the API populated
     (e.g. `EGS_MASS_ImportCustInst` reads `EGS_MASS_Input`),
   - Any class extending `RunBaseBatch` or implementing `SysOperation` that
     references the same data,
   - Menu items pointing at such a class (`AxMenuItemAction` with
     `Object` = the batch class).
2. Capture: batch class name, full file path, the staging table it reads,
   the trigger mechanism (manual menu item / scheduled batch / on-demand),
   and the menu path a human would click to run it manually
   (`Module > … > <Menu item label>`). Locate the menu item by searching
   `AxMenuItemAction` for `Object` matching the batch class, then locate
   the parent menu in `AxMenu`.
3. Update `registry.json` with `batchClass`, `batchClassPath`,
   `stagingTable`, `triggerMechanism`, `batchMenuItem`, and
   `batchMenuPath`. Leave all status fields empty for `/check-status` to
   populate.
4. Print a summary that tells the user **what to run and where**:

```
Batch identified for EGS_API_newAgreement
  Batch class:        EGS_MASS_ImportCustInst
  Path:               J:\AosService\...\AxClass\EGS_MASS_ImportCustInst.xml
  Staging table:      EGS_MASS_Input
  Trigger mechanism:  RunBaseBatch — manual menu item

Run this batch in D365 now:
  Menu path:    EXU Billing > Periodic > Import customer installation
  Menu item:    EGS_MASS_ImportCustInst

After the batch finishes, run /check-status EGS_API_newAgreement to
locate the status form.
```

**No runnable class is generated. No `XAPI_PostProcess_*` artefact exists.**

If the batch cannot be resolved from source, halt and flag — do not guess.

---

### `/check-status <APIClassName>`

Precondition: `/find-batch` has run and recorded the batch in the registry.
The user has now run the batch in D365.

**This command tells the user where to inspect status. It does not query
D365 and does not write errors itself.** After inspecting the form, the
user copies failed-row details into `TestData/Errors.md` manually (format
below). `/fix-errors` then resolves them.

**Steps:**

1. Open the batch class identified in the previous step. Trace where it
   writes its outcome:
   - Successful processing → which **result table** gets the new business
     record (e.g. `EXU_AgreementTable` for `EGS_MASS_ImportCustInst`).
   - Per-row status → which table/field records pass/fail per staging row
     (often the staging table itself gains a `Status` / `Treated` flag,
     or a sibling log table).
   - Errors → which field on the per-row status table holds the
     verbatim error message (e.g. `EGS_MASS_Input.Err_Msg`).
2. Identify the **form** humans use to inspect this — typically the form
   bound to the per-row status table or its log header (often labelled
   "treated posts", "import log", or similar). Capture the menu item name
   and the menu path (`Module > Inquiries > …`) by searching
   `AxMenuItemDisplay` and `AxMenu` metadata.
3. Capture which field/value combination indicates success vs. failure
   (e.g. `EGS_MASS_Input.Status == PROCESSED` for success, `ERROR` for
   failure). Capture the error-message field name.
4. Update `registry.json`: fill in `statusTable`, `statusField`,
   `successValues`, `failureValues`, `errorMessageField`, `statusForm`,
   `statusMenuItem`, `statusMenuPath`, and `resultTable`.
5. Print a summary that tells the user **where to look and what to copy**:

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
    For every row where Status = ERROR and the row was produced by
    setup (XAPI_-prefixed identifier), copy the row into
    TestData/Errors.md using the format documented in CLAUDE_PostProcess.md.
    Then run /fix-errors EGS_API_newAgreement.
```

**No runnable class is generated. Claude does not query D365.**

If a value cannot be resolved from source, halt and flag — do not guess.

---

### `/fix-errors <APIClassName>`

Precondition: the user has populated `TestData/Errors.md` with one or more
entries for `<APIClassName>` (format below). The registry already has the
post-process entry for the API.

**Steps:**

1. Read `TestData/Errors.md`. Filter entries where the heading API class
   name matches `<APIClassName>`. Process the most recent entries first
   (entries are timestamped; newest at top of selection).
2. For each entry, classify the failure:
   - **Missing dependency** (e.g. "X not found in CustTable",
     "Customer XAPI_CUST_001 not found"). Re-trace source code to find
     the table the dependency lives in. Same logic as `/patch`.
   - **Validation rejection** (e.g. "DeliveryCategory must be Agreement").
     Trace the validation site in the batch class; identify the field on
     `EGS_MASS_Input` that drives it; map back to the payload field; map
     back to setup-script values.
   - **Reference-data mismatch** (e.g. wrong `companyId`, missing
     `EGS_MASS_ConvInString` row). Re-trace conversion lookup tables.
   - **Unrecognised** — do not guess. Flag and ask the user.
3. For each classified failure, propose the fix — typically one of:
   - Update the setup script `XAPI_Setup_<APIClassName>.xml` to create
     the missing dependency before the API call.
   - Update the teardown script `XAPI_Teardown_<APIClassName>.xml` to
     remove the new dependency afterwards.
   - Update `registry.json` `mappings` with a new field → table entry so
     future analyses know about it.
   - Update the payload (only if the payload itself is malformed; rare).
4. Ask the user to confirm before applying. Apply each accepted fix.
5. Append a `Resolution` line to the corresponding entry in `Errors.md`
   with the date, what was changed, and a reference to any updated
   registry mapping. **Do not delete the entry** — `Errors.md` is an
   append-only log.
6. After applying fixes, the user should:
   - Re-run `XAPI_Teardown_<APIClassName>` (to clean stale staging rows
     that still have `Status = ERROR`),
   - Re-run `XAPI_Setup_<APIClassName>` and the API call,
   - Re-run the batch from its menu (`/find-batch` summary),
   - Re-run `/check-status` to confirm no further errors.

   Print these steps explicitly at the end of `/fix-errors` output so the
   user has a clear next-action list.

If a failure cannot be resolved from source, halt and flag — do not guess.

---

## Errors File (`TestData/Errors.md`)

User-populated log of every failed row observed during `/check-status`
inspection. New entries are **appended** by the user; `/fix-errors` reads
them and appends a `Resolution` line per entry once fixed. The file is
created on first error and never deleted.

**Format — one section per failed staging row:**

```markdown
## <ISO-8601 timestamp> — <APIClassName>

- **Batch:** <batchClass>
- **Staging row:** <stagingTable>.<keyField> = <value>
- **Status:** <statusField> = <failureValue>
- **Error message:** <verbatim message from D365>
- **Source payload:** <path under TestData/payloads/...>
```

After `/fix-errors` resolves the entry it appends:

```markdown
- **Resolution (<date>):** <what was changed> (e.g. setup script now creates
  X record, registry mapping added for Y → Z table)
```

**Example — entry as the user wrote it:**

```markdown
## 2026-05-08T14:32:11Z — EGS_API_newAgreement

- **Batch:** EGS_MASS_ImportCustInst
- **Staging row:** EGS_MASS_Input.MessageId = XAPI_AGR_001
- **Status:** Status = ERROR
- **Error message:** Customer XAPI_CUST_001 not found in CustTable for legal entity DEMF
- **Source payload:** TestData/payloads/newAgreement.json
```

**After `/fix-errors`:**

```markdown
## 2026-05-08T14:32:11Z — EGS_API_newAgreement

- **Batch:** EGS_MASS_ImportCustInst
- **Staging row:** EGS_MASS_Input.MessageId = XAPI_AGR_001
- **Status:** Status = ERROR
- **Error message:** Customer XAPI_CUST_001 not found in CustTable for legal entity DEMF
- **Source payload:** TestData/payloads/newAgreement.json
- **Resolution (2026-05-08):** XAPI_Setup_EGS_API_newAgreement now creates a CustTable record with AccountNum=XAPI_CUST_001 before the agreement payload is sent. Registry mapping added: AlternativeCustomerNumber → CustTable.
```

**Rules:**

- The user writes new entries by hand (or pastes from a SQL query result).
  `/check-status` tells them where to look but does not write.
- `/fix-errors` only appends `Resolution` lines — it never deletes or
  rewrites prior content.
- Entries are timestamped UTC ISO-8601 to keep the log diff-friendly and
  ordered.
- The file is markdown so it renders nicely in editors and Git hosts; do
  not switch it to JSON without updating this spec.
- The error message must be verbatim from D365 — do not paraphrase or
  summarize, otherwise debugging context is lost.

---

## Registry Extension

`TestData/registry.json` gains a new top-level key, `postProcess`, alongside
the existing `mappings`. Existing entries under `mappings` are untouched.

```json
{
  "mappings": [ /* unchanged — see CLAUDE.md */ ],
  "postProcess": [
    {
      "apiClassName": "EGS_API_newAgreement",
      "classification": "ASYNC",
      "responseSignal": "ack-only (status, correlationId)",
      "codeTraceSignal": "writes EGS_MASS_Input",
      "batchClass": "EGS_MASS_ImportCustInst",
      "batchClassPath": "J:\\AosService\\PackagesLocalDirectory\\XellentBilling\\XellentBilling\\AxClass\\EGS_MASS_ImportCustInst.xml",
      "stagingTable": "EGS_MASS_Input",
      "triggerMechanism": "RunBaseBatch — manual menu item",
      "batchMenuItem": "EGS_MASS_ImportCustInst",
      "batchMenuPath": "EXU Billing > Periodic > Import customer installation",
      "statusTable": "EGS_MASS_Input",
      "statusField": "Status",
      "successValues": ["PROCESSED", "WARNING"],
      "failureValues": ["ERROR"],
      "errorMessageField": "Err_Msg",
      "resultTable": "EXU_AgreementTable",
      "statusForm": "EGS_MASS_Inlog",
      "statusMenuItem": "EGS_MASS_Inlog",
      "statusMenuPath": "EXU Billing > Import > The agreementinformation > Treated posts",
      "discoveredFrom": "EGS_API_newAgreement"
    },
    {
      "apiClassName": "EGS_API_newCustomer",
      "classification": "SYNC",
      "responseSignal": "returns CustAccount + RecId",
      "codeTraceSignal": "no batch trigger / no staging-table write",
      "discoveredFrom": "EGS_API_newCustomer"
    }
  ]
}
```

**Rules:**

- Every API analyzed by `/check-response` gets an entry, even SYNC ones. A
  SYNC entry serves as proof-of-classification so future runs don't redo
  the work.
- `postProcessRunnable` is **no longer used.** Older registry files may
  still contain it — leave the field for archaeology, but new entries
  must not include it.
- Paths are stored as full Windows paths to make tracing reproducible
  across machines. Repo-relative paths are not sufficient because the
  source code lives outside the repo.
- Never delete an entry. If a classification changes (rare — usually
  means the API was rewritten), update in place and add a `revisedFrom`
  field noting the prior value.

---

## Conventions

- The post-process runnable concept is **removed** from this workflow.
  The user runs the batch directly via its existing menu item; Claude
  only identifies which menu item that is. This avoids generating
  duplicate code that has to be maintained alongside the real batch
  class.
- Status tables and forms are **read-only from Claude's perspective** —
  these commands record where to look; the user inspects and copies
  errors into `Errors.md`.
- If `/check-response` returns SYNC, **stop**. Do not run `/find-batch`,
  `/check-status`, or `/fix-errors`. The registry entry for that API is
  final.
- If any step cannot be resolved from source code, halt and ask. Same
  rule as `/patch`: do not guess.

---

## Suggested Test Flow

For an API with post-process (e.g. `EGS_API_newAgreement`):

1. `/analyze newAgreement.json EGS_API_newAgreement`        → dependency list
2. `/generate`                                              → setup + teardown
3. `/check-response EGS_API_newAgreement`                   → ASYNC confirmed
4. `/find-batch EGS_API_newAgreement`                       → batch identified
   in registry; printed menu path.
5. **User runs:** `XAPI_Setup_*` → POST to API → batch (via the printed
   menu path) in D365.
6. `/check-status EGS_API_newAgreement`                     → status fields
   filled in registry; printed form / menu path / fields to inspect.
7. **User opens** the form in D365, copies any failed rows into
   `TestData/Errors.md` using the documented format.
8. `/fix-errors EGS_API_newAgreement`                       → reads
   `Errors.md`, fixes setup/teardown/registry, appends `Resolution`
   lines. Then user re-runs setup → API → batch → status check.
9. `XAPI_Teardown_*`                                        → final cleanup.

For a SYNC API (e.g. `EGS_API_newCustomer`):

1. `/analyze`, `/generate` as today.
2. `/check-response EGS_API_newCustomer`                    → SYNC, registry
   entry written, done.
