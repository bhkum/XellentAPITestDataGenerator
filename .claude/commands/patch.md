# /patch

Patch setup and teardown scripts based on a D365 error log.

## Usage
```
/patch <error_log_file>
```

## Steps

1. Read the error from `<repo_root>\TestData\<error_log_file>`
2. Identify the missing or incorrect dependency from the error message
3. Re-trace `J:\AosService\PackagesLocalDirectory\XellentBilling\XellentBilling\AxClass\` to find the correct table
4. Update both `TST_Setup_<APIClassName>.xml` and `TST_Teardown_<APIClassName>.xml`
5. Update `<repo_root>\TestData\registry.json` with the new mapping
6. Print a summary of what changed

## If dependency cannot be resolved from source
- Do NOT guess
- Flag it explicitly:
```
⚠ Could not resolve: <fieldName> — manual investigation required
```
