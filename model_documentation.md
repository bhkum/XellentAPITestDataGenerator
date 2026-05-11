# APITestDataGen — Model Documentation

> Auto-generated from the X++ metadata under `Metadata/APITestDataGen/`.
> Scope: Microsoft Dynamics 365 Finance & Operations model that exercises the
> Xellent Billing API surface with synthetic test data.

---

## 1. Model Overview

| Property | Value |
|---|---|
| Display name | API Test Data Gen |
| Internal name | APITestDataGen |
| Module | APITestDataGen |
| Publisher | EG |
| Model ID | 896001024 |
| Layer | 10 (USR) |
| Version | 1.0.0.0 |
| Customization | Allow |
| Locked | false |

**Module references (dependencies):**
- ApplicationFoundation
- ApplicationPlatform
- ApplicationSuite
- ContactPerson
- Dimensions
- Directory
- Ledger
- XellentBilling

**Packages / Modules found in this repo:**
- `APITestDataGen` — the single model under `Metadata/APITestDataGen/`.

**Element inventory:**

| Element type | Count |
|---|---|
| AxClass | 28 |
| AxTable | 2 |
| AxEnum | 1 |
| AxForm | 1 |
| AxMenu | 1 |
| AxMenuExtension | 1 |
| AxMenuItemAction | 5 |
| AxMenuItemDisplay | 1 |
| AxInterface | 0 |

---

## 2. Tables

### 2.1 `XAPI_APILogTable`

Append-only audit log for API call results.

**Properties:** `AllowRowVersionChangeTracking=Yes`, `SubscriberAccessLevel.Read=Allow`.

**Fields:**

| Name | Type | EDT / Enum | Notes |
|---|---|---|---|
| LogNum | String | `Num` | Log Number |
| ApiName | Enum | `XAPI_APIS` | API Name |
| Key | String | `Description` | Key |
| Value | String | `Description` | Value |
| Message | String | `Description` | Message |
| InfologData | Container | `InfologData` | |
| Response | String | `DirMemo` (IgnoreEDTRelation=Yes) | Raw HTTP response payload |
| Archived | Enum | `NoYes` / `NoYesId` | Soft-archive flag |

**Field groups:**

| Group | Fields |
|---|---|
| AutoReport | LogNum, ApiName, Key, Value, Message |
| AutoIdentification (AutoPopulate) | LogNum |
| AutoLookup, AutoSummary, AutoBrowse | *(empty)* |

**Indexes:** none defined.
**Relations:** none defined.

---

### 2.2 `XAPI_APIConfigurations`

Stores per-API endpoint configuration and the JSON payload template.

**Properties:** `AllowRowVersionChangeTracking=Yes`, `SubscriberAccessLevel.Read=Allow`.

**Fields:**

| Name | Type | EDT / Enum | Notes |
|---|---|---|---|
| API | Enum | `XAPI_APIS` | API identifier |
| JSON | String (size -1) | — | JSON template (unbounded) |
| URL | String (size -1) | — | Endpoint URL |
| Execute | Enum | `NoYes` / `NoYesId` | Run flag |

**Field groups:**

| Group | Fields |
|---|---|
| Main (Main) | API |
| AutoIdentification (AutoPopulate) | *(empty)* |
| AutoReport, AutoLookup, AutoSummary, AutoBrowse | *(empty)* |

**Indexes:** none defined.
**Relations:** none defined.

---

## 3. Classes

> The model uses two parallel patterns:
> 1. **Caller stack** — `XAPI_APICallerBase` (abstract) + per-API subclasses.
> 2. **Runner stack** — `XAPI_APIRunnerBase` (abstract) + `XAPI_APIRunner_Customer`,
>    fronted by `XAPI_APIRunnerFacade`.
>
> Data-prep classes (`XAPI_New*DataGen` / `XAPI_New*DelDataGen`) and the
> `XAPI_Setup_*` / `XAPI_Teardown_*` runnables are flat `static main` helpers.

### 3.1 Caller hierarchy

#### `XAPI_APICallerBase` *(abstract)*

Base class for HTTP/OAuth2 communication with EGS APIs. Holds tenant/client
credentials sourced from `XAPI_AutomationHelper` and exposes a Template Method
pattern (`cleanPrevious → runDependency → callAPI`).

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| protected | | str | `GetAccessToken()` | Obtains OAuth2 access token from Azure login via client-credentials flow. |
| protected | | void | `sendPostRequest()` | POSTs `body` to endpoint with Bearer auth; stores response in `responseText`. |
| protected | | void | `prepareBody(Map _fields)` | Replaces JSON template placeholders with values from `_fields`. |
| public | static | XAPI_APICallerBase | `construct(XAPI_APIS _api)` | Factory dispatching to the concrete caller subclass for `_api`. |
| public | | XAPI_APIS | `parmApi(XAPI_APIS _api = api)` | Parm accessor for the current API enum. |
| protected | | void | `log(str _key, str _value)` | Inserts a row into `XAPI_APILogTable` for the current API. |
| public abstract | | str | `getUrl()` | Endpoint URL — implemented by subclasses. |
| public abstract | | void | `callAPI()` | Subclass-specific request shaping + POST. |
| public abstract | | void | `cleanPrevious()` | Hook for pre-call cleanup. |
| public abstract | | void | `runDependency()` | Hook for prerequisite-data generation. |

#### `XAPI_APICaller_ContactInfo` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the ContactInfo endpoint URL. |
| public | | void | `callAPI()` | Reads customer account from log, POSTs contact-info payload, logs result. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

#### `XAPI_APICaller_Contract` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the Contract endpoint URL. |
| public | | void | `callAPI()` | Builds contract body from prior customer/installation logs, POSTs, logs contract number. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

#### `XAPI_APICaller_ContractPart` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the ContractPart endpoint URL. |
| public | | void | `callAPI()` | Builds contract-part request from customer/contract/installation logs, POSTs, then runs `EGS_MASS_ImportContractExt`. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

#### `XAPI_APICaller_Customer` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the Customer endpoint URL. |
| public | | void | `callAPI()` | Selects dimension attribute/value, generates a CPR, POSTs customer payload, logs `AccountNum`. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

#### `XAPI_APICaller_Installation` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the Installation endpoint URL. |
| public | | void | `callAPI()` | Builds installation payload (data area, net area), POSTs, deserializes response, logs `InstNum`. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

#### `XAPI_APICaller_MeterReading` *(extends `XAPI_APICallerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | | str | `getUrl()` | Returns the MeterReading endpoint URL. |
| public | | void | `callAPI()` | Reads customer/installation/meter context from logs, POSTs reading, logs meter number. |
| public | | void | `cleanPrevious()` | No-op. |
| public | | void | `runDependency()` | No-op. |

---

### 3.2 Runner hierarchy

#### `XAPI_APIRunnerBase` *(abstract)*

Newer execution path driven from the `XAPI_APIConfigurations` form. Splits the
flow into `runPre / runAPI / postRun` and reads URL + JSON from the
configuration table at run time.

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| protected abstract | | void | `runPre()` | Pre-execution hook implemented by subclasses. |
| protected abstract | | str | `body()` | Returns the fully-prepared request body. |
| public | | void | `run()` | Template orchestrator: `runPre()` → `runAPI()` → `postRun()`. |
| protected | | void | `init()` | Loads `XAPI_APIConfigurations` for the current API enum. |
| protected | | void | `runAPI()` | Delegates to `sendPostRequest()`. |
| protected | | void | `postRun()` | Empty hook for post-execution logic. |
| public | static | XAPI_APIRunnerBase | `construct(XAPI_APIS _api)` | Factory returning the concrete runner subclass for `_api`. |
| protected | | str | `prepareBody(Map _fields)` | Replaces placeholders in `apiConfig.JSON` with values from `_fields`. |
| protected | | void | `sendPostRequest()` | POSTs to `apiConfig.URL` with Bearer token + the body returned by `body()`. |
| protected | | str | `GetAccessToken()` | OAuth2 client-credentials access token retrieval. |

#### `XAPI_APIRunner_Customer` *(extends `XAPI_APIRunnerBase`)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| protected | | void | `runPre()` | Invokes `XAPI_Setup_EGS_API_newCustomer::main(null)` to stage prerequisites. |
| protected | | void | `init()` | Sets `api = CreateCustomer`, then delegates to base `init()`. |
| protected | | str | `body()` | Picks dimension/value, generates CPR, fills a Map, returns prepared body. |

#### `XAPI_APIRunnerFacade` *(final)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `runAPI(XAPI_APIS _api)` | Single entry point used by the form `RunButton`: constructs and runs the matching runner. |

---

### 3.3 Automation / orchestration

#### `XAPI_API_AutomationFacade`

High-level static facade combining creation and cleanup workflows.

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `cleanUp_Contract(EXU_ContractNum _contract)` | Deletes a single contract; logs presence/absence. |
| public | static | void | `cleanup_installation(EXU_InstNum _instNum)` | Deletes installation + dependent contract, utility installation, delivery point. |
| public | static | void | `cleanup_Customer(CustAccount _accNum)` | Deletes customer and related contract/installation records. |
| public | static | void | `cleanup_dependentData()` | Fires the `*DelDataGen` classes for contracts, installations, customers, contract parts. |
| public | static | void | `create_Customer()` | Generates customer prerequisites and calls the Customer API. |
| public | static | void | `create_Installation()` | Generates installation prerequisites and calls the Installation API. |
| public | static | void | `create_Contract()` | Generates contract prerequisites and calls the Contract API. |
| public | static | void | `create_ContractPart()` | Generates contract-part prerequisites and calls the ContractPart API. |
| public | static | void | `create_MeterReading()` | Generates meter-reading prerequisites and calls the MeterReading API. |

#### `XAPI_API_CallAutomator`

Standalone HTTP/OAuth2 client (predecessor / parallel to the Caller stack).

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| private | | str | `GetAccessToken()` | Retrieves OAuth2 token; extracts via regex; returns "" on error. |
| public | | str | `sendPostRequest(str url, str json)` | POSTs `json` with Bearer auth; lazy-loads the token; returns response or "". |
| public | | void | `callCreateCustAPI()` | Builds customer config map, POSTs, deserializes response, logs result. |
| public | | void | `callInstallationAPI()` | Builds installation map, POSTs, logs `InstNum`. |
| public | | void | `callContractAPI(CustAccount _custAccount, str _attachmentNum, str _deliveryCategory)` | Builds contract config from prior logs + utility installation, POSTs, logs response. |
| public | | void | `callContractPartAPI(CustAccount _custAccount, str _attachmentNum, str _deliveryCategory, EXU_ContractNum _contractNum, str _productNum)` | Builds contract-part config from logs, POSTs, logs response. |
| public | | void | `callMeterReadingAPI(CustAccount _custAccount, str _attachmentNum, str _deliveryCategory, str _meterNum)` | Builds meter-reading config from logs + utility installation, POSTs, logs `MeterNum`. |
| private | | void | `log(XAPI_APIS _api, str _key, str _value, str _response)` | Inserts a row into `XAPI_APILogTable`. |
| private | | str | `prepareBody(XAPI_APIS _api, Map _fields)` | Loads JSON template and replaces placeholders. |
| public | static | XAPI_API_CallAutomator | `construct()` | Instance factory. |

#### `XAPI_API_Create` *(final)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `main(Args _args)` | Runs the full creation chain: Customer → Installation → Contract → ContractPart → MeterReading via the facade. |

#### `XAPI_API_Delete` *(final)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `main(Args _args)` | Looks up customer from log, deletes via facade, archives log; errors if customer missing. |

#### `XAPI_AutomateScripts`

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `main(Args _args)` | Loads customer/installation/contract from log, archives log, runs full automation. |
| private | | void | `deleteAll()` | Transactionally deletes contract → delivery points → utility installation → installation → customer, then dependent data. |
| public | | void | `execute()` | Calls `deleteAll`, then create-and-call sequence for all five APIs. |
| public | | CustAccount | `parmCustomerAccount(CustAccount _accNum = accNum)` | Parm accessor (builder pattern). |
| public | | EXU_InstNum | `parmInstNum(EXU_InstNum _instNum = instNum)` | Parm accessor. |
| public | | EXU_ContractNum | `parmContractNum(EXU_ContractNum _contractNum = contractNum)` | Parm accessor. |
| public | static | XAPI_AutomateScripts | `construct()` | Instance factory. |

#### `XAPI_AutomationHelper` *(final)*

Static configuration constants only — no methods.

| Field | Value (placeholder) |
|---|---|
| `tenantId` | `"tenantid"` |
| `clientId` | `"clientid"` |
| `clientSecret` | `"clientsecret"` |
| `resourceUrl` | `"url"` |

#### `XAPI_Runner` *(final)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `main(Args _args)` | Placeholder entry point (body currently empty/commented). |
| private | static | void | `createCustomer()` | Generates customer data and calls the CreateCustomer API. |
| private | static | void | `createInstallation()` | Generates installation data and calls the CreateInstallation API. |
| private | static | void | `createContract()` | Generates contract data and calls the CreateContract API. |

#### `XAPI_Temp_RunForCust` *(final)*

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| public | static | void | `main(Args _args)` | Empty placeholder entry point. |

---

### 3.4 ID generators

#### `XAPI_AlternativeCustomerNumberGenerator`

Generates valid Nordic personal/company identification numbers.

| Vis | Static | Returns | Signature | Summary |
|---|---|---|---|---|
| | static | void | `initialize()` | Lazily allocates the static `System.Random` instance. |
| private | static | int | `randRange(int min, int max)` | Random int in `[min, max]`. |
| private | static | str | `padLeft(str value, int len, str ch)` | Left-pads a string to a target length. |
| private | static | int | `luhnCheckDigit(str nine)` | Computes the Luhn check digit for a 9-digit string. |
| private | static | boolean | `luhnValid(str number)` | Validates a number against the Luhn algorithm. |
| private | static | str | `generateDanishCPR()` | Random valid Danish CPR (day/month/year/sequence + Luhn). |
| private | static | str | `generateDanishCVR()` | Random valid Danish CVR (8 digits with weighted checksum). |
| private | static | str | `generateSwedishCPR()` | Random valid Swedish CPR (1950–2020 birth date + Luhn). |
| private | static | str | `generateSwedishCVR()` | Random valid Swedish CVR (9 digits with Luhn). |
| public | static | str | `generateCPR()` | Picks DK/SE generator based on company validation rule. |
| public | static | str | `generateCVR()` | Picks DK/SE generator based on company validation rule. |

---

### 3.5 Test-data generators (per-domain)

All of the classes below expose only `public static void main(Args _args)`.
They clone existing rows with `buf2Buf` and prefix new records with `TST_` /
`XAPI_`. Their counterparts (`*DelDataGen`) delete the same rows in reverse
dependency order.

| Class | Modifiers | Purpose |
|---|---|---|
| `XAPI_NewCustomerDataGen` | — | Creates test customer group, payment mode, and 4 technique groups (`TST_Tech1..4`). |
| `XAPI_NewCustomerDelDataGen` | — | Deletes customer group, payment mode, and the 4 technique groups. |
| `XAPI_NewContractDataGen` | — | Creates billing plan (`TST_Billing`) and Installation-type contract template (`TST_Template`). |
| `XAPI_NewContractDelDataGen` | internal final | Deletes contract template and billing plan. |
| `XAPI_NewContractPartDataGen` | — | Creates delivery group, product, inventory item, and product-extend row. |
| `XAPI_newContractPartDelDataGen` | internal final | Deletes product-extend row, item, product, delivery group. |
| `XAPI_NewInstallationDataGen` | — | Creates the full installation prereq tree (zip / county / street / delivery category / reading plan / supply model / ElsmartParam). |
| `XAPI_NewInstallationDelDataGen` | internal final | Tears down the installation prereq tree in reverse. |
| `XAPI_NewMeterReadingDataGen` | internal final | Creates meters, billing counters, group tables, reading import params, and posts a mounting journal with readings. |
| `XAPI_Setup_EGS_API_newCustomer` | — | Stages prerequisites for the `EGS_API_newCustomer` test (customer group `TST_CstGrp`, payment mode `TST_PaymMd`, receiving system `NewSystem_TC3`). |
| `XAPI_Teardown_EGS_API_newCustomer` | — | Tears down the same prerequisites, guarded by the `TST_` prefix. |

---

## 4. Enums

### `XAPI_APIS`

API identifier shared by `XAPI_APIConfigurations`, `XAPI_APILogTable`, and the
runner/caller factories.

| Name | Label | Value |
|---|---|---|
| CreateCustomer | Customer | 0 (default) |
| CreateInstallation | Installation | 1 |
| CreateContract | Contract | 2 |
| CreateCustContactInfo | Contact Info | 3 |
| CreateMeterReading | Meter Reading | 4 |
| CreateContractPart | Contract Part | 5 |

- `Label`: API
- `IsExtensible`: true
- `UseEnumValue`: No

---

## 5. Forms

### `XAPI_APIConfigurations`

Maintenance form for the per-API endpoint + JSON template configuration.

- **Pattern:** SimpleListDetails-Grid v1.4
- **Form class:** extends `FormRun`

**Data sources:**

| Name | Table |
|---|---|
| XAPI_APIConfigurations | XAPI_APIConfigurations |

**Key controls:**

| Control | Type | DataSource / DataField | Notes |
|---|---|---|---|
| ActionPaneMain | ActionPane | — | |
| └ ActionPaneTabMain | ActionPaneTab | — | |
| &nbsp;&nbsp; ├ ButtonGroupNew | ButtonGroup | — | NewButton, DeleteButton (CommandButtons) |
| &nbsp;&nbsp; └ ButtonGroupRun | ButtonGroup | — | RunButton (Button) — invokes `XAPI_APIRunnerFacade::runAPI(XAPI_APIS::CreateCustomer)` on `clicked()`. |
| GridContainer | Group | XAPI_APIConfigurations | Hosts QuickFilter + Grid |
| └ Grid | Grid (AllowEdit=No) | XAPI_APIConfigurations | `Grid_API` (ComboBox → `API`) |
| DetailsHeader | Group | — | Pattern: FieldsFieldGroups |
| Tab → General → GroupConfiguration | TabPage / Group | XAPI_APIConfigurations | Fields: `API`, `URL`, `Execute`, `JSON` (MultiLine=Yes) |

---

## 6. Interfaces

No `AxInterface` elements are defined in this model. The
caller/runner hierarchies use **abstract base classes**
(`XAPI_APICallerBase`, `XAPI_APIRunnerBase`) rather than X++ interfaces, with
`construct(XAPI_APIS)` factory methods to dispatch to concrete subclasses.

---

## 7. Menus and Menu Items

### Menu `XAPI_MainMenu`

Label: **XAPI**. Surfaced into the standard `MainMenu` via the menu extension
`MainMenu.APITestDataGen`.

- Submenu **APITesting**
  - `XAPI_AutomateScripts` (Action → `XAPI_API_CallAutomator`)
  - `XAPI_API_Delete` (Action → `XAPI_API_Delete`)
  - `XAPI_API_Create` (Action → `XAPI_API_Create`)
- Submenu **InDevelopmen** *(sic)*
  - `XAPI_APIConfigurations` (Display → form `XAPI_APIConfigurations`)
  - `XAPI_Setup_EGS_API_newCustomer` (Action → class `XAPI_Setup_EGS_API_newCustomer`)
  - `XAPI_Teardown_EGS_API_newCustomer` (Action → class `XAPI_Teardown_EGS_API_newCustomer`)

### Menu item actions

| Name | Label | Object | Object type |
|---|---|---|---|
| XAPI_API_Create | API Creation | XAPI_API_Create | Class |
| XAPI_API_Delete | API data deletion | XAPI_API_Delete | Class |
| XAPI_AutomateScripts | Run all api | XAPI_API_CallAutomator | Class |
| XAPI_Setup_EGS_API_newCustomer | Setup: EGS API New Customer | XAPI_Setup_EGS_API_newCustomer | Class |
| XAPI_Teardown_EGS_API_newCustomer | Teardown: EGS API New Customer | XAPI_Teardown_EGS_API_newCustomer | Class |

### Menu item displays

| Name | Label | Object | Object type |
|---|---|---|---|
| XAPI_APIConfigurations | API Configurations | XAPI_APIConfigurations | Display (Form) |

---

## 8. Conventions and Naming Patterns

Observed across the codebase:

- **`XAPI_` prefix** on every model artifact (classes, tables, enum, form,
  menus, menu items). This matches the project rule in `CLAUDE.md`.
- **Test-data prefix `TST_`** is used inside `*DataGen` classes for new
  records (e.g. `TST_CustGrp`, `TST_PaymMd`, `TST_Template`, `TST_Tech1..4`).
  A few classes use `XAPI_`-prefixed values directly (`Setup_EGS_*`).
- **Pairing**: every `*DataGen` class has a matching `*DelDataGen` companion
  that deletes exactly what its sibling created — usually in reverse order,
  with existence checks and `info()` / `warning()` logging.
- **Runnable shape**: helper classes expose a single
  `public static void main(Args _args)` entry point and are registered as
  `MenuItemAction` items.
- **Two parallel API call patterns**:
  - Older: `XAPI_API_CallAutomator` + `XAPI_API_AutomationFacade` (one big
    class with `callXxxAPI` methods, configuration in `XAPI_AutomationHelper`).
  - Newer: `XAPI_APIRunnerBase` + per-API `XAPI_APIRunner_*` subclasses,
    fronted by `XAPI_APIRunnerFacade::runAPI(XAPI_APIS)`, configured at
    runtime from the `XAPI_APIConfigurations` table.
- **Factory pattern**: `construct(XAPI_APIS _api)` on both `XAPI_APICallerBase`
  and `XAPI_APIRunnerBase` dispatches by enum value to the right subclass.
- **OAuth2 client-credentials flow** is duplicated across
  `XAPI_APICallerBase`, `XAPI_APIRunnerBase`, and `XAPI_API_CallAutomator`
  (each has its own `GetAccessToken`); credentials come from
  `XAPI_AutomationHelper` static fields.
- **Logging**: every API call writes to `XAPI_APILogTable` (with the
  `XAPI_APIS` enum as the discriminator), and later steps read back the log
  to chain requests (e.g. Contract uses the customer + installation rows
  logged by earlier calls).
- **Casing inconsistency** — class naming is mostly PascalCase, but a few
  outliers exist (`XAPI_newContractPartDelDataGen` is camelCase, and the
  submenu is labelled `InDevelopmen` rather than `InDevelopment`). These
  look like typos rather than deliberate convention.
- **`internal final`** is used on the newer delete-data and meter-reading
  generators (`XAPI_NewContractDelDataGen`, `XAPI_newContractPartDelDataGen`,
  `XAPI_NewInstallationDelDataGen`, `XAPI_NewMeterReadingDataGen`), while
  older classes are plain public.
- **Credentials are placeholder values** in `XAPI_AutomationHelper`
  (`"tenantid"`, `"clientid"`, `"clientsecret"`, `"url"`); real values
  presumably come from environment-specific overlays or future refactoring.
