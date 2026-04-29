# /generate

Generate setup and teardown runnable classes from the last `/analyze` output.

## Usage
```
/generate
```

## Steps

1. Use the dependency list from the last `/analyze` run
2. Generate two X++ runnable class XML files in `J:\AosService\PackagesLocalDirectory\APITestDataGen\APITestDataGen\AxClass\`:
   - `XAPI_Setup_<APIClassName>.xml`
   - `XAPI_Teardown_<APIClassName>.xml`

## Rules for setup class
- Class must have a `main(Args _args)` entry point
- Process dependencies in dependency order (parent records before child records)
- Always guard inserts: `if (!record.RecId)` before every insert
- Always use `buf2Buf` from an existing record of the same type as template
- Log every action: `info(strFmt("Inserted %1: %2", tableName, value))`
- All test values must use `XAPI_` prefix

## Rules for teardown class
- Delete in reverse dependency order
- Only delete records where the key starts with `XAPI_` — never touch pre-existing data
- Log every deletion: `info(strFmt("Deleted %1: %2", tableName, value))`

## Output
Confirm both files created and print the file paths.
