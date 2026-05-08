# /check-response

Decide whether an API is synchronous (response carries the created entity's data) or asynchronous (response is only an ack and a post-process batch is required).

## Usage
```
/check-response <APIClassName>
```

## Steps

1. Locate `<APIClassName>.xml` in `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\`.
2. **Response-shape signal.** Find the response object the API constructs (typically via a `*Response` contract class or a return container). Inspect which fields it populates:
   - Returns business identifiers/values created by the call (e.g. `CustAccount`, `RecId`, full echo of input + generated keys) → `SYNC` candidate.
   - Returns only an ack (`status`, `messageId`, empty container, or echoed input without newly-created keys) → `ASYNC` candidate.
3. **Code-trace confirmation.** In the same call chain, look for any of:
   - Writes to `EGS_MASS_Input` or other staging tables
   - Calls into `EGS_MASS_*` import processors
   - `BatchHeader`, `SysOperation`, `RunBaseBatch` invocations
   - Enqueues onto a queue/poster table

   Any of the above → confirms `ASYNC`. None → confirms `SYNC`.
4. **Reconcile.** If signals agree → final classification. If they disagree → print both findings and stop. Do **not** guess. Ask the user.
5. Write a `postProcess` entry in `<repo_root>\TestData\registry.json` with `apiClassName`, `classification`, `responseSignal`, `codeTraceSignal`, and `discoveredFrom`. SYNC entries are final; ASYNC entries are filled in by `/find-batch` and `/check-status`.

## Output

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

## If a signal cannot be resolved from source
- Do NOT guess.
- Flag explicitly and stop:
```
⚠ Could not resolve <signal> for <APIClassName> — manual investigation required
```
