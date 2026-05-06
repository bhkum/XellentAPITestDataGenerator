# /generate

Generate setup and teardown runnable classes from the last `/analyze` output, along with menu items registered under `XAPI_MainMenu > InDevelopmen`.

## Usage
```
/generate
```

## Config
Before generating, read `<repo_root>\TestData\xapi.config.json` and use the value of `testDataPrefix` for all naming. Never hardcode a prefix.

## Steps

1. Use the dependency list from the last `/analyze` run
2. Read prefix from `<repo_root>\TestData\xapi.config.json` → `testDataPrefix`
3. Generate two X++ runnable class XML files in `J:\AosService\PackagesLocalDirectory\APITestDataGen\APITestDataGen\AxClass\`:
   - `<testDataPrefix>Setup_<APIClassName>.xml`
   - `<testDataPrefix>Teardown_<APIClassName>.xml`
4. Generate two menu item XML files in `J:\AosService\PackagesLocalDirectory\APITestDataGen\APITestDataGen\AxMenuItemAction\`:
   - `<testDataPrefix>Setup_<APIClassName>.xml` — pointing to the setup runnable class
   - `<testDataPrefix>Teardown_<APIClassName>.xml` — pointing to the teardown runnable class
5. Register both menu items under `XAPI_MainMenu > InDevelopmen` by updating the menu XML at `J:\AosService\PackagesLocalDirectory\APITestDataGen\APITestDataGen\AxMenu\XAPI_MainMenu.xml`

## Rules for setup class
- Class must have a `main(Args _args)` entry point
- Process dependencies in dependency order (parent records before child records)
- Always guard inserts: `if (!record.RecId)` before every insert
- Always use `buf2Buf` from an existing record of the same type as template
- Log every action: `info(strFmt("Inserted %1: %2", tableName, value))`
- All test values must use `testDataPrefix`

## Rules for teardown class
- Delete in reverse dependency order
- Only delete records where the key starts with `testDataPrefix` — never touch pre-existing data
- Log every deletion: `info(strFmt("Deleted %1: %2", tableName, value))`

## Output
Confirm all four files created/updated and print the file paths.
