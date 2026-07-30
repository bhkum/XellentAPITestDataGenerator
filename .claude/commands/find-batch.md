# /find-batch

Identify the batch processor that consumes what an ASYNC API wrote, and tell the user which menu item to run.

**This command does NOT generate a runnable and does NOT execute the batch.**
The user runs the batch themselves in D365 via the printed menu path.

## Usage
```
/find-batch <APIClassName>
```

## Precondition
`/check-response <APIClassName>` returned `ASYNC`. If it returned `SYNC`, refuse and exit.

## Steps

1. Re-trace the API call chain in `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\` to identify the batch processor that consumes what the API wrote. Look for:
   - The class that reads from the staging table the API populated (e.g. `EGS_MASS_ImportCustInst` reads `EGS_MASS_Input`)
   - Any class extending `RunBaseBatch` or implementing `SysOperation` that references the same data
   - Menu items pointing at such a class (search `AxMenuItemAction` for `Object` matching the batch class)
2. Capture: batch class name, full file path, the staging table it reads, the trigger mechanism, the menu item name, and the full menu path (`Module > … > <Menu item label>`) by tracing the parent menu in `AxMenu`.

## Registry update
Update `<repo_root>\TestData\registry.json` for the matching `postProcess` entry. At this stage fill in:
- `batchClass`
- `batchClassPath` (full Windows path)
- `stagingTable`
- `triggerMechanism`
- `batchMenuItem`
- `batchMenuPath`

Do **not** add `postProcessRunnable` — it is no longer part of the workflow.
Leave status fields empty for `/check-status` to populate.

## Output

Print a summary that tells the user **what to run and where**:

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

## No runnable class is generated
The user runs the batch directly via its existing menu item. Claude only identifies which menu item that is.

## If the batch cannot be resolved from source
- Do NOT guess.
- Flag explicitly:
```
⚠ Could not identify batch processor for <APIClassName> — manual investigation required
```
