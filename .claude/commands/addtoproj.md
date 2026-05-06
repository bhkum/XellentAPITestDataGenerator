# /addtoproj

Add all objects generated in the current session to a D365 F&O project file (.rnrproj).

## Usage
```
/addtoproj <project_file_path>
```

## Steps

1. Read the `.rnrproj` XML file at `<project_file_path>`
2. Collect all objects generated in the current session:
   - Runnable classes (`AxClass`)
   - Menu items (`AxMenuItemAction`)
3. For each object, check if it already exists in the `.rnrproj` — skip if it does
4. For each new object, add an entry in the correct `<ModelElementData>` section with the appropriate element type:
   - Classes → `RunBaseClass`
   - Menu items → `MenuItemAction`
5. Save the updated `.rnrproj` file
6. Print a summary:
   - Added: list of objects added
   - Skipped: list of objects already present

## Rules
- Never remove existing entries from the project file
- Never modify anything outside the `<ModelElementData>` section
- If `<project_file_path>` does not exist or is not a valid `.rnrproj` file, stop and flag the error — do not create a new file
