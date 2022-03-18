

# Installation
## Kentico Application ("Mother"/"Admin"):

1. Install `XperienceCommunity.CSVImport.Admin` Nuget Package on your Kentico Application
1. Rebuild your web application
1. Log into Kentico as a Global Administrator
1. Go to Modules
1. Search and edit `CSV Import`
1. Go to `Sites` and add to your site.

# Usage

In the Kentico Xperience Admin, go to CSV Importer -> CSV Import.  

1. Select the class you wish to import to
2. Select your delimiter (comma default, but European CSV is often by semi colon)
3. (Optionally) click "click here to generate a default csv file" link to get all the current data in a csv with all fields formatted properly (**NOTE**: we can only generate using comma separation, you may have to manually adjust to semi-colon for European Excels to edit it)
4. Select your csv file and hit Upload CSV
5. Adjust mappings
6. Run the Import

## Import Configuration and Settings

 * **Text NULL to set Null Value**: Allows you to set null (vs empty) values in the database by putting "NULL"
 * **Insert Mode**
   * None: No inserts will take place
   * Insert All: All records are treated as new records
   * Insert by No Value in Identifier: Any record that does not contain the ID field value will be inserted.
* **Identifier Mapping**: The CSV Field that will be treated as the ID Field
* **Update Mode**
    * None: No updates will take place
    * Update All: All records that have an ID field will be updated
    * Update By Indicator: If any of the following values are found in the indicator field, that record will update:  1, y, yes, true, delete
 * **Update Indicator Mapping**: The field that contains the Update Indicator for the Update mode
* **Delete Mode**
    * None: No deletes will take place
    * Delete All: All records in the **database** will be delete (not just ones present)
    * DeleteBy Indicator: If any of the following values are found in the indicator field, that record will be deleted:  1, y, yes, true, delete
 * **Update Indicator Mapping**: The field that contains the Update Indicator for the Update mode

### Table Mappings
This area allows you to configure the Mapping in the CSV to the database columns.  

**-Auto Handle-** will automatically handle the given fields, and even if mapped, if the value is empty and the field is a system field (CodeName, Guid, Last Modified, Row ID) it will handle them automatically.

### Class Limiter Settings
In the Kentico Xperience Settings module, you can specify what classes are allowed to be CSV Imported into.  This is useful in ensuring that someone doesn't mess up a vital table.

# Staging / Event Handling
This tool is set to trigger normal object insert/delete workflows (except Form data will not shoot off emails) as long as the object exists in code (ObjectInfo / ObjectInfoProvider are created).  If a custom module class does not have it's Info/InfoProvider class generated, it will use SQL to insert/update/delete, and no staging tasks or triggers will be hit.  Please be aware of this as you use this tool.

# Acknowledgement, Contributions, bug fixes and License

This tool is free for all to use.

# Compatability
Can be used on any Kentico Xperience 13 (both .Net Core and .Net)

Previous versions of the tool existed under the NuGet package [HBS_CSVImport](https://www.nuget.org/packages/HBS_CSVImport/) for versions 8.2-12
