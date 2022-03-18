using System;
using System.Collections.Generic;
using System.Linq;
using CMS.UIControls;
using CMS.Helpers;
using CMS.Base.Web.UI;
using CMS.DataEngine;
using System.Web.UI.WebControls;
using System.IO;
using System.Xml;
using System.Web.UI;
using CMS.CustomTables;
using CsvHelper;
using CsvHelper.Configuration;
using CMS.OnlineForms;
using CMS.Membership;
using CMS.SiteProvider;
using System.Text;

namespace XperienceCommunity.CSVImport
{
    // Look for the 2 "MODIFY HERE" comment in order to quickly find the place to modify in order to add your own Custom Module Classes so Event hooks, Event Logs, and Staging tasks will properly generate.

    [UIElement("CSVImport", "CSVImport", CheckPermissions = true)]
    public partial class CSVImport : CMSPage
    {
        #region "Properties"

        public int CurrentClassID
        {
            get
            {
                return ValidationHelper.GetInteger(ddlClasses.SelectedValue, -1);
            }
        }

        public string RowIDMapping
        {
            get
            {
                return ddlIdentifierField.SelectedValue;
            }
        }


        #endregion
        protected void Page_Load(object sender, EventArgs e)
        {
            ScriptHelper.RegisterBootstrapScripts(this.Page);
            CssRegistration.RegisterBootstrap(this.Page);
        }

        /// <summary>
        /// Sets up the Mapping fields and configuration for the selected Class.
        /// </summary>
        private void SetupMapping()
        {
            pnlCSVMapAndSettings.Visible = true;
            pnlGetOrUploadCSV.Visible = false;

            // Get CSV fields
            int ClassID = CurrentClassID;
            ClassID = (ClassID > 0 ? ClassID : ValidationHelper.GetInteger(SessionHelper.GetValue("CSV_ClassID_" + MembershipContext.AuthenticatedUser.UserID), -1));
            string delimieter = (ValidationHelper.GetString(SessionHelper.GetValue("CSV_Delimeter_" + MembershipContext.AuthenticatedUser.UserID), ","));
            string CSVContent = (string)SessionHelper.GetValue("CSV_Import_" + ClassID);
            StringReader CSVStringRead = new StringReader(CSVContent);
            var reader = new CsvReader(CSVStringRead, new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true,
                Delimiter = delimieter
            });
            reader.Read();
            reader.ReadHeader();
            string[] FieldHeaders = reader.HeaderRecord;
            List<ListItem> DropDownOptions = new List<ListItem>();
            DropDownOptions.Add(new ListItem("---", ""));
            DropDownOptions.Add(new ListItem("-Auto Handle-", "CSVImport_Auto"));

            foreach (string fieldHeader in FieldHeaders)
            {
                DropDownOptions.Add(new ListItem(fieldHeader, fieldHeader));
            }

            // Get fields
            DataClassInfo ClassObject = DataClassInfoProvider.GetDataClassInfo(ClassID);
            XmlDocument ClassFormXml = new XmlDocument();
            ClassFormXml.LoadXml(ClassObject.ClassFormDefinition);
            var PrimaryKeyFieldNode = ClassFormXml.SelectSingleNode("/form/field[@isPK='true']");
            string PrimaryKeyFieldName = "";
            if (PrimaryKeyFieldNode != null)
            {
                PrimaryKeyFieldName = PrimaryKeyFieldNode.Attributes["column"].Value;
            }

            if (plcFields.Controls.Count == 0)
            {
                // loop through fields, creating a Label and drop down for each
                foreach (XmlNode fieldNode in ClassFormXml.SelectNodes("/form/field"))
                {
                    string FieldName = fieldNode.Attributes["column"].Value;
                    string FieldCaption = (fieldNode.SelectSingleNode("./properties/fieldcaption") != null ? fieldNode.SelectSingleNode("./properties/fieldcaption").InnerText : FieldName);

                    bool IsAutoField = false;
                    if (FieldName == PrimaryKeyFieldName || FieldName.EndsWith("CreatedBy") || FieldName.EndsWith("ModifiedBy") || FieldName.EndsWith("CreatedWhen") || FieldName.EndsWith("ModifiedWhen") || FieldName.EndsWith("GUID"))
                    {
                        IsAutoField = true;
                    }

                    bool required = false;
                    if (fieldNode.Attributes["alloweempty"] != null)
                    {
                        required = ValidationHelper.GetBoolean(fieldNode.Attributes["alloweempty"].Value, false);
                    }

                    DropDownList ddlOptions = new DropDownList();
                    ddlOptions.ID = "ddl_" + FieldName;
                    foreach (ListItem li in DropDownOptions)
                    {
                        ddlOptions.Items.Add(new ListItem(li.Text, li.Value)
                        {
                            Selected = ((IsAutoField && li.Value == "CSVImport_Auto") || (!IsAutoField && li.Text == FieldName))
                        });
                    }
                    ddlOptions.CssClass = "dropdown form-control mapping-control";

                    plcFields.Controls.Add(ddlOptions);


                    Label fieldLabel = new Label();
                    fieldLabel.ID = "lbl_" + FieldName;
                    fieldLabel.Text = FieldCaption + " [" + FieldName + "]";
                    fieldLabel.CssClass = "form-label  mapping-label " + (required ? "required" : "");
                    fieldLabel.AssociatedControlID = "ddl_" + FieldName;
                    plcFields.Controls.Add(fieldLabel);

                    plcFields.Controls.Add(new Literal() { Text = "<div class=\"clear\"/>" });

                }
            }
            ddlIdentifierField.Items.Clear();
            ddlUpdateIndicator.Items.Clear();
            ddlDeleteIndicator.Items.Clear();
            // Set the identifier drop down
            foreach (ListItem li in DropDownOptions)
            {
                if (!li.Value.Contains("CSVImport"))
                {
                    ddlIdentifierField.Items.Add(new ListItem(li.Text, li.Value)
                    {
                        Selected = (li.Text == PrimaryKeyFieldName)
                    });
                    ddlUpdateIndicator.Items.Add(new ListItem(li.Text, li.Value)
                    {
                        Selected = (li.Text == "UpdateIndicator")
                    });
                    ddlDeleteIndicator.Items.Add(new ListItem(li.Text, li.Value)
                    {
                        Selected = (li.Text == "DeleteIndicator")
                    });
                }
            }
        }

        /// <summary>
        /// Sets the Classes available in the drop down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {

            var ClassesQuery = DataClassInfoProvider.GetClasses().Where("(ClassIsCustomTable =1 or ClassIsForm = 1 or ClassResourceID in (Select ResourceID from CMS_Resource where ResourceVersion is not null))").Columns(@"(ClassDisplayName+ ' ['+ 
case ClassIsCustomTable when 1 then 'Custom Table' else '' end+
case ClassIsForm when 1 then 'Form' else '' end+
case when (ClassIsForm = 0 or ClassIsForm is null) and (ClassIsCustomTable = 0 or ClassIsCustomTable is null) then 'Module' else '' end+
']'
) as ClassDisplayName", "ClassID").OrderBy("ClassIsCustomTable desc, ClassIsForm desc, ClassDisplayName");
            string AllowedClasses = SettingsKeyInfoProvider.GetValue("ClassesAllowedToCSVImport", new SiteInfoIdentifier(SiteContext.CurrentSiteID));
            if (!string.IsNullOrWhiteSpace(AllowedClasses))
            {
                ClassesQuery = ClassesQuery.WhereIn("ClassName", AllowedClasses.Split(";|,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            }

            // Erase this if it's not a postback.
            if (!IsPostBack)
            {
                SessionHelper.SetValue("CSV_ClassID_" + MembershipContext.AuthenticatedUser.UserID, null);
                SessionHelper.SetValue("CSV_Delimeter_" + MembershipContext.AuthenticatedUser.UserID, null);
            }

            ddlClasses.DataSource = ClassesQuery;
            ddlClasses.DataTextField = "ClassDisplayName";
            ddlClasses.DataValueField = "ClassID";
            ddlClasses.DataBind();
            ddlClasses.Items.Insert(0, new ListItem("Select Class", "-1"));
            int ClassID = ValidationHelper.GetInteger(SessionHelper.GetValue("CSV_ClassID_" + MembershipContext.AuthenticatedUser.UserID), -1);
            string delimieter = ValidationHelper.GetString(SessionHelper.GetValue("CSV_Delimeter_" + MembershipContext.AuthenticatedUser.UserID), string.Empty);
            if (SessionHelper.GetValue("CSV_Import_" + ClassID) != null && pnlGetOrUploadCSV.Visible == false)
            {
                SetupMapping();
            }
        }

        /// <summary>
        /// Handles setting the current selected class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlClasses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentClassID > 0)
            {
                pnlGetOrUploadCSV.Visible = true;
                btnGetCSV.OnClientClick = string.Format("javascript: window.open(\"{0}?ClassID={1}\", \"_blank\");", URLHelper.ResolveUrl("~/CMSModules/CSVImport/Pages/CSVImportGetCSV.aspx"), CurrentClassID);
                SessionHelper.SetValue("CSV_Import_" + CurrentClassID, null);
                SessionHelper.SetValue("CSV_ClassID_" + MembershipContext.AuthenticatedUser.UserID, null);
                SessionHelper.SetValue("CSV_Delimeter_" + MembershipContext.AuthenticatedUser.UserID, null);
            }
        }

        /// <summary>
        /// Copies the File's string content into the current session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnParseCSV_Click(object sender, EventArgs e)
        {
            if (fileCSV.HasFile && fileCSV.FileName.ToLower().Contains(".csv"))
            {
                // Save the CSV data to session

                StreamReader content = new StreamReader(fileCSV.FileContent, GetEncoding(fileCSV.FileBytes));
                string CSVContent = content.ReadToEnd();
                SessionHelper.SetValue("CSV_Import_" + CurrentClassID, CSVContent);
                SessionHelper.SetValue("CSV_ClassID_" + MembershipContext.AuthenticatedUser.UserID, CurrentClassID);
                SessionHelper.SetValue("CSV_Delimeter_" + MembershipContext.AuthenticatedUser.UserID, ddlDelimiter.SelectedValue);
                // Clear and rebuild the drop downs.
                plcFields.Controls.Clear();
                SetupMapping();

            }
            else
            {
                AddError("Must upload a CSV file in order to continue.");
            }
        }

        private static Encoding GetEncoding(byte[] fileBytes)
        {
            if (fileBytes.Length >= 4)
            {
                // Read the BOM
                var bom = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    bom[i] = fileBytes[i];
                }
                // Analyze the BOM
                if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
                if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
                if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
                if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
                if (bom[0] == 0x49 && bom[1] == 0x74 && bom[2] == 0x65 && bom[3] == 0x6D) return Encoding.GetEncoding("ISO-8859-1");

                return Encoding.ASCII;
            }
            else
            {
                return Encoding.ASCII;
            }
        }

        /// <summary>
        /// Processes CSV.  MODIFY THIS if you wish to use your Custom Module's generated Classes (Info / Info Provider)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnProcessImport_Click(object sender, EventArgs e)
        {
            List<string> Errors = new List<string>();
            int Inserted = 0;
            int Updated = 0;
            int Deleted = 0;
            bool InsertUpdateDeletedCountsAvailable = true;
            // Build configuration
            DataClassInfo ClassObject = DataClassInfoProvider.GetDataClassInfo(CurrentClassID);
            XmlDocument ClassFormXml = new XmlDocument();
            ClassFormXml.LoadXml(ClassObject.ClassFormDefinition);
            var PrimaryKeyFieldNode = ClassFormXml.SelectSingleNode("/form/field[@isPK='true']");
            string PrimaryKeyFieldName = "";
            if (PrimaryKeyFieldNode != null)
            {
                PrimaryKeyFieldName = PrimaryKeyFieldNode.Attributes["column"].Value;
            }
            InsertModeEnums InsertMode = InsertModeEnums.None;
            UpdateModeEnums UpdateMode = UpdateModeEnums.None;
            DeleteModeEnums DeleteMode = DeleteModeEnums.None;
            string RowIDMapping = ddlIdentifierField.SelectedValue;
            string UpdateIndicatorMapping = ddlUpdateIndicator.SelectedValue;
            string DeleteIndicatorMapping = ddlDeleteIndicator.SelectedValue;
            bool RowIDMapped = !string.IsNullOrWhiteSpace(RowIDMapping);
            switch (ddlInsertMode.SelectedValue)
            {
                case "":
                    InsertMode = InsertModeEnums.None;
                    break;
                case "InsertAll":
                    InsertMode = InsertModeEnums.InsertAll;
                    break;
                case "Insert":
                    InsertMode = (RowIDMapped ? InsertModeEnums.InsertNoIDValue : InsertModeEnums.None);
                    break;
            }
            if (RowIDMapped)
            {
                switch (ddlUpdateMode.SelectedValue)
                {
                    case "":
                        UpdateMode = UpdateModeEnums.None;
                        break;
                    case "UpdateAll":
                        UpdateMode = UpdateModeEnums.UpdateAll;
                        break;
                    case "UpdateByIndicator":
                        UpdateMode = (!string.IsNullOrWhiteSpace(UpdateIndicatorMapping) ? UpdateModeEnums.UpdateByIndicator : UpdateModeEnums.None);
                        break;
                }

                switch (ddlDeleteMode.SelectedValue)
                {
                    case "":
                        DeleteMode = DeleteModeEnums.None;
                        break;
                    case "DeleteAll":
                        DeleteMode = DeleteModeEnums.DeleteAll;
                        break;
                    case "DeleteByIndicator":
                        DeleteMode = (!string.IsNullOrWhiteSpace(DeleteIndicatorMapping) ? DeleteModeEnums.DeleteByIndicator : DeleteModeEnums.None);
                        break;
                }
            }

            Dictionary<ClassField, string> ClassFieldToCSVField = new Dictionary<ClassField, string>();
            // loop through fields, creating a Label and drop down for each
            foreach (XmlNode fieldNode in ClassFormXml.SelectNodes("/form/field"))
            {

                string FieldName = fieldNode.Attributes["column"].Value;
                ClassField ClassFieldObject = new ClassField(ClassFormXml, FieldName);
                // Find the matching drop down by field name control ID
                foreach (Control control in plcFields.Controls)
                {
                    if (control.ID == "ddl_" + FieldName)
                    {
                        if (!string.IsNullOrWhiteSpace(((DropDownList)control).SelectedValue))
                        {
                            ClassFieldToCSVField.Add(ClassFieldObject, ((DropDownList)control).SelectedValue);
                        }
                    }
                }
            }

            // Time to start processing, based on the type
            string CSVContent = (string)SessionHelper.GetValue("CSV_Import_" + CurrentClassID);
            if (string.IsNullOrWhiteSpace(CSVContent))
            {
                // bad form, reload page
                URLHelper.RefreshCurrentPage();
            }
            StringReader CSVStringRead = new StringReader(CSVContent);
            var reader = new CsvReader(CSVStringRead, new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ddlDelimiter.SelectedValue
            });
            reader.Read();
            reader.ReadHeader();


            // Handle full delete if it's set to delete all first
            if (DeleteMode == DeleteModeEnums.DeleteAll)
            {
                switch (ClassObject.ClassName)
                {
                    //////////////////////////// MODIFY HERE ///////////////////////
                    // Copy the below commented section and add in your own class names for custom Module Classes if you want the API to track staging tasks and hit global events.
                    /*case "TestModule.MyCustomItem":
                        var DeletedItems = TestModule.MyCustomItemInfoProvider.GetMyCustomItems();
                            Deleted = DeletedItems.Count;
                            DeletedItems.ForEachObject(x => x.Delete());
                        break;*/
                    default:
                        // Custom Tables and Forms
                        if (ClassObject.ClassIsCustomTable)
                        {
                            var DeletedItems = CustomTableItemProvider.GetItems(ClassObject.ClassName);
                            Deleted = DeletedItems.Count;
                            DeletedItems.ForEachObject(x => x.Delete());
                        }
                        else if (ClassObject.ClassIsForm)
                        {
                            var DeletedItems = BizFormItemProvider.GetItems(ClassObject.ClassName);
                            Deleted = DeletedItems.Count;
                            DeletedItems.ForEachObject(x => x.Delete());
                        }
                        else
                        {
                            var ObjectFactory = new InfoObjectFactory(ClassObject.ClassName);
                            if (ObjectFactory.Singleton == null)
                            {
                                // manual delete using SQL
                                ConnectionHelper.ExecuteNonQuery("Delete from " + ClassObject.ClassTableName, null, QueryTypeEnum.SQLQuery);
                            }
                            else
                            {
                                // Delete using the ObjectQuery
                                new ObjectQuery(ClassObject.ClassName).ForEachObject(x => x.Delete());
                            }
                        }

                        break;
                }
            }

            while (reader.Read())
            {
                try
                {
                    // Determine the operation
                    SetValueTypeEnum RowOperation = SetValueTypeEnum.Insert;
                    int RowID = -1;
                    if (RowIDMapped && !string.IsNullOrWhiteSpace(reader.GetField(RowIDMapping)))
                    {
                        // Check delete first
                        if (DeleteMode == DeleteModeEnums.DeleteAll)
                        {
                            RowOperation = SetValueTypeEnum.Insert;
                        }
                        else if (DeleteMode == DeleteModeEnums.DeleteByIndicator && ParseDeleteIndicator(reader.GetField(DeleteIndicatorMapping)))
                        {
                            RowOperation = SetValueTypeEnum.Delete;
                            RowID = ValidationHelper.GetInteger(reader.GetField(RowIDMapping), -1);
                        }
                        else if (UpdateMode == UpdateModeEnums.UpdateAll || (UpdateMode == UpdateModeEnums.UpdateByIndicator && ParseUpdateIndicator(reader.GetField(UpdateIndicatorMapping))))
                        {
                            RowOperation = SetValueTypeEnum.Update;
                            RowID = ValidationHelper.GetInteger(reader.GetField(RowIDMapping), -1);
                        }
                        else if (InsertMode == InsertModeEnums.None)
                        {
                            RowOperation = SetValueTypeEnum.Ignore;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (InsertMode != InsertModeEnums.None)
                    {
                        RowOperation = SetValueTypeEnum.Insert;
                    }
                    else
                    {
                        RowOperation = SetValueTypeEnum.Ignore;
                    }

                    if (RowOperation == SetValueTypeEnum.Ignore)
                    {
                        continue;
                    }

                    switch (ClassObject.ClassName)
                    {
                        //////////////////////////// MODIFY HERE ///////////////////////
                        // Copy the below commented section and add in your own class names for custom Module Classes.
                        /*case "TestModule.MyCustomItem":
                            TestModule.MyCustomItemInfo ItemObject = null;
                                if (RowOperation == SetValueTypeEnum.Insert)
                                {
                                    ItemObject = new TestModule.MyCustomItemInfo();

                                } else
                                {
                                    ItemObject = TestModule.MyCustomItemInfoProvider.GetMyCustomItem(RowID);
                                    Updated++;
                                }

                                if(RowOperation != SetValueTypeEnum.Delete)
                                {
                                    SetBaseInfoItemValues(RowOperation, ItemObject, ClassFieldToCSVField, reader, ClassFormXml, ClassObject.ClassName);
                                    if(RowOperation == SetValueTypeEnum.Insert)
                                    {
                                        ItemObject.Insert();
                                        Inserted++;
                                    } else
                                    {
                                        ItemObject.Update();
                                        Updated++;
                                    }
                                } else
                                {
                                    ItemObject.Delete();
                                    Deleted++;
                                }
                            break;*/
                        default:
                            // Custom Tables
                            if (ClassObject.ClassIsCustomTable)
                            {
                                CustomTableItem ItemObject = null;
                                if (RowOperation == SetValueTypeEnum.Insert)
                                {
                                    ItemObject = CustomTableItem.New(ClassObject.ClassName);
                                }
                                else
                                {
                                    ItemObject = CustomTableItemProvider.GetItem(RowID, ClassObject.ClassName);
                                    if (ItemObject == null)
                                    {
                                        Errors.Add(GenerateCSVNoItemFoundError(reader));
                                    }
                                }

                                if (RowOperation != SetValueTypeEnum.Delete)
                                {
                                    SetBaseInfoItemValues(RowOperation, ItemObject, ClassFieldToCSVField, reader, ClassObject.ClassName);
                                    if (RowOperation == SetValueTypeEnum.Insert)
                                    {
                                        ItemObject.Insert();
                                        Inserted++;
                                    }
                                    else
                                    {
                                        ItemObject.Update();
                                        Updated++;
                                    }
                                }
                                else
                                {
                                    ItemObject.Delete();
                                    Deleted++;
                                }
                            }
                            else if (ClassObject.ClassIsForm)
                            {
                                BizFormItem ItemObject = null;
                                if (RowOperation == SetValueTypeEnum.Insert)
                                {
                                    ItemObject = BizFormItem.New(ClassObject.ClassName);
                                }
                                else
                                {
                                    ItemObject = BizFormItemProvider.GetItem(RowID, ClassObject.ClassName);
                                }

                                if (RowOperation != SetValueTypeEnum.Delete)
                                {
                                    SetBaseInfoItemValues(RowOperation, ItemObject, ClassFieldToCSVField, reader, ClassObject.ClassName);
                                    if (RowOperation == SetValueTypeEnum.Insert)
                                    {
                                        ItemObject.Insert();
                                        Inserted++;
                                    }
                                    else
                                    {
                                        ItemObject.Update();
                                        Updated++;
                                    }
                                }
                                else
                                {
                                    ItemObject.Delete();
                                    Deleted++;
                                }
                            }
                            else
                            {
                                // Try to create object using ObjectFactory
                                var JoinTableClassFactory = new InfoObjectFactory(ClassObject.ClassName);
                                if (JoinTableClassFactory.Singleton == null)
                                {
                                    // Generate SQL to do an insert, delete, etc
                                    ExecuteSQLItemOperation(RowOperation, ClassFieldToCSVField, reader, ClassObject);
                                    InsertUpdateDeletedCountsAvailable = false;
                                }
                                else
                                {
                                    BaseInfo ItemObject = null;
                                    if (RowOperation == SetValueTypeEnum.Insert)
                                    {
                                        ItemObject = (BaseInfo)JoinTableClassFactory.Singleton;

                                    }
                                    else
                                    {
                                        ItemObject = new ObjectQuery(ClassObject.ClassName).WithID(RowID).FirstOrDefault();
                                        Updated++;
                                    }

                                    if (RowOperation != SetValueTypeEnum.Delete)
                                    {
                                        SetBaseInfoItemValues(RowOperation, ItemObject, ClassFieldToCSVField, reader, ClassObject.ClassName);
                                        if (RowOperation == SetValueTypeEnum.Insert)
                                        {
                                            ItemObject.Insert();
                                            Inserted++;
                                        }
                                        else
                                        {
                                            ItemObject.Update();
                                            Updated++;
                                        }
                                    }
                                    else
                                    {
                                        ItemObject.Delete();
                                        Deleted++;
                                    }
                                }

                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Errors.Add(GenerateCSVItemError(reader, ex.Message));
                }
            }

            if (Errors.Count > 0)
            {
                AddError(string.Format("The following Errors Occured: <ul><li>{0}</ul></ul>", string.Join("</li><li>", Errors)));
                SetupMapping();
            }
            else
            {
                AddConfirmation((InsertUpdateDeletedCountsAvailable ? string.Format("Operation Successful. {0} Inserted, {1} Updated, {2} Deleted", Inserted, Updated, Deleted) : "Operation Successful."));
                ddlClasses.SelectedIndex = 0;
                pnlCSVMapAndSettings.Visible = false;
            }
        }

        /// <summary>
        /// Generates an error for when an referenced Item is not found for that particular CSV record.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private string GenerateCSVNoItemFoundError(CsvReader reader)
        {
            return string.Format("No Item Found for CSV Record with {0} = {1}", RowIDMapping, reader.GetField(RowIDMapping));
        }

        /// <summary>
        /// Generates an Error string for if an error occurs inserting/updating/deleting a CSV record
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private string GenerateCSVItemError(CsvReader reader, string message)
        {
            var data = reader.HeaderRecord.Select(x => reader[x]).ToArray();
            return string.Format("Error message {0} while processing CSV Record [\"{1}\"]", message, string.Join("\",\"", data));
        }

        /// <summary>
        /// Handles creating dynamic SQL to perform the INSERT, UPDATE, or DELETE
        /// </summary>
        /// <param name="Operation">The Operation this is running</param>
        /// <param name="ClassFieldToCSVField">The Dictionary of Class Fields to their mapping CSV Field</param>
        /// <param name="csvRecord">The current CSV Record</param>
        /// <param name="classObject">The Class Object</param>
        private void ExecuteSQLItemOperation(SetValueTypeEnum Operation, Dictionary<ClassField, string> ClassFieldToCSVField, CsvReader csvRecord, DataClassInfo classObject)
        {
            string DynamicSQL = "";
            if (Operation == SetValueTypeEnum.Insert)
            {
                DynamicSQL = string.Format("INSERT INTO {0} ", classObject.ClassTableName);
            }
            else if (Operation == SetValueTypeEnum.Update)
            {
                DynamicSQL = string.Format("UPDATE {0} SET ", classObject.ClassTableName);
            }
            else if (Operation == SetValueTypeEnum.Delete)
            {
                DynamicSQL = string.Format("DELETE FROM {0} WHERE {1} = {2}", classObject.ClassTableName, RowIDMapping, csvRecord.GetField(RowIDMapping));
            }

            List<ClassField> Fields = new List<ClassField>();
            List<object> Values = new List<object>();

            foreach (ClassField ClassFieldObject in ClassFieldToCSVField.Keys)
            {

                if (ClassFieldToCSVField[ClassFieldObject] == "CSVImport_Auto")
                {
                    // Handle automatic Default Values for non system fields, system fields such as Item ID, GUID, Modified/Created do not have a default value.
                    if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.DefaultValue != null)
                    {
                        Fields.Add(ClassFieldObject);
                        Values.Add(ClassFieldObject.DefaultValue);
                    }
                    else
                    {
                        if (ClassFieldObject.DataType == ColumnTypeEnum.Integer)
                        {
                            // Handle user ID and SiteID
                            if (ClassFieldObject.FieldName.ToLower().Contains("modified"))
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(MembershipContext.AuthenticatedUser.UserID);
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("created"))
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(MembershipContext.AuthenticatedUser.UserID);
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("siteid"))
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(SiteContext.CurrentSiteID);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.DateTime)
                        {
                            // Handle Modified When, Created should auto insert this date.
                            if (ClassFieldObject.FieldName.ToLower().Contains("modified"))
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(DateTime.Now);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.Text)
                        {
                            if (ClassFieldObject.FieldName.ToLower().Contains("codename") && Operation == SetValueTypeEnum.Insert)
                            {
                                // Handle code name generation
                                Fields.Add(ClassFieldObject);
                                Values.Add(string.Format("{0}_{1}_{2}", classObject.ClassName.Replace(".", "_"), ClassFieldObject.FieldName, DateTime.Now.ToString("yyyy-MM-dd")));
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("sitename"))
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(SiteContext.CurrentSiteName);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.GUID)
                        {
                            // Handle Auto Guid
                            if (ClassFieldObject.FieldName.ToLower().EndsWith("guid") && Operation == SetValueTypeEnum.Insert)
                            {
                                Fields.Add(ClassFieldObject);
                                Values.Add(Guid.NewGuid());
                            }
                        }
                    }
                }
                else if ((string.IsNullOrWhiteSpace(csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject])) && ClassFieldObject.DataType != ColumnTypeEnum.Text) ||
                   (ValidationHelper.GetString(csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject]), "").ToLower().Trim() == "null" && (ClassFieldObject.DataType != ColumnTypeEnum.Text || cbxTreatNullAsNull.Checked)))
                {
                    // No value, put NULL if allowed, otherwise default value
                    if (ClassFieldObject.AllowNull)
                    {
                        Fields.Add(ClassFieldObject);
                        Values.Add(null);
                    }
                    else
                    {
                        Fields.Add(ClassFieldObject);
                        Values.Add(ClassFieldObject.DefaultValue);
                    }
                }
                else
                {
                    Fields.Add(ClassFieldObject);
                    Values.Add(csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject]));
                }
            }

            if (Operation == SetValueTypeEnum.Insert || Operation == SetValueTypeEnum.Update)
            {
                List<string> FieldsStr = new List<string>();
                List<string> ValuesStr = new List<string>();
                List<string> UpdateStr = new List<string>();
                for (int i = 0; i < Fields.Count; i++)
                {
                    ClassField Field = Fields[i];
                    object Value = Values[i];
                    FieldsStr.Add(Field.FieldName);
                    if (Value == null)
                    {
                        ValuesStr.Add("NULL");
                        UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, "NULL"));
                    }
                    else
                    {
                        switch (Field.DataType)
                        {
                            case ColumnTypeEnum.Integer:
                            case ColumnTypeEnum.Double:
                            case ColumnTypeEnum.Decimal:
                                ValuesStr.Add(Value.ToString());
                                UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, Value.ToString()));
                                break;
                            case ColumnTypeEnum.GUID:
                            case ColumnTypeEnum.Text:
                                ValuesStr.Add("'" + SqlHelper.EscapeQuotes(Value.ToString()) + "'");
                                UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, "'" + SqlHelper.EscapeQuotes(Value.ToString()) + "'"));
                                break;
                            case ColumnTypeEnum.DateTime:
                                ValuesStr.Add("'" + ValidationHelper.GetDateTime(Value, new DateTime()).ToString("yyyy-MM-ddTHH:mm:ssZ") + "'");
                                UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, "'" + ValidationHelper.GetDateTime(Value, new DateTime()).ToString("yyyy-MM-ddTHH:mm:ssZ") + "'"));
                                break;
                            case ColumnTypeEnum.Binary:
                                ValuesStr.Add("CAST('" + SqlHelper.EscapeQuotes(ValidationHelper.GetBinary(Value, new byte[] { }).ToString()) + "' as VARBINARY)");
                                UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, "CAST('" + SqlHelper.EscapeQuotes(ValidationHelper.GetBinary(Value, new byte[] { }).ToString()) + "' as VARBINARY)"));
                                break;
                            case ColumnTypeEnum.Boolean:
                                ValuesStr.Add(ValidationHelper.GetBoolean(Value, false) ? "1" : "0");
                                UpdateStr.Add(string.Format("[{0}] = {1}", Field.FieldName, ValidationHelper.GetBoolean(Value, false) ? "1" : "0"));
                                break;
                        }
                    }
                }

                if (Operation == SetValueTypeEnum.Insert)
                {
                    DynamicSQL += string.Format(" ([{0}]) VALUES ({1})", string.Join("],[", FieldsStr), string.Join(",", ValuesStr));
                }
                if (Operation == SetValueTypeEnum.Update)
                {
                    DynamicSQL += string.Format(" {0} WHERE {1} = {2}", string.Join(",", UpdateStr), RowIDMapping, csvRecord.GetField(RowIDMapping));
                }
            }

            ConnectionHelper.ExecuteNonQuery(DynamicSQL, null, QueryTypeEnum.SQLQuery);
        }

        /// <summary>
        /// Sets the BaseInfo's property values.
        /// </summary>
        /// <param name="Operation">What type of Row operation this is (insert / update)</param>
        /// <param name="itemObject">The BaseInfo object that is being modified</param>
        /// <param name="ClassFieldToCSVField">The Dictionary of Class Fields to the CSV Field mappings</param>
        /// <param name="csvRecord">The current CSV Record</param>
        /// <param name="ClassName">The Class Name of the Table (Used for Code Name generation)</param>
        private void SetBaseInfoItemValues(SetValueTypeEnum Operation, BaseInfo itemObject, Dictionary<ClassField, string> ClassFieldToCSVField, CsvReader csvRecord, string ClassName)
        {
            foreach (ClassField ClassFieldObject in ClassFieldToCSVField.Keys)
            {

                if (ClassFieldToCSVField[ClassFieldObject] == "CSVImport_Auto")
                {
                    // Handle automatic Default Values for non system fields, system fields such as Item ID, GUID, Modified/Created do not have a default value.
                    if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.DefaultValue != null)
                    {
                        itemObject.SetValue(ClassFieldObject.FieldName, ClassFieldObject.DefaultValue);
                    }
                    else
                    {
                        if (ClassFieldObject.DataType == ColumnTypeEnum.Integer)
                        {
                            // Handle user ID and SiteID
                            if (ClassFieldObject.FieldName.ToLower().Contains("modified"))
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, MembershipContext.AuthenticatedUser.UserID);
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("created"))
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, MembershipContext.AuthenticatedUser.UserID);
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("siteid"))
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, SiteContext.CurrentSiteID);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.DateTime)
                        {
                            // Handle Modified When, Created should auto insert this date.
                            if (ClassFieldObject.FieldName.ToLower().Contains("modified"))
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, DateTime.Now);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.Text)
                        {
                            if (ClassFieldObject.FieldName.ToLower().Contains("codename") && Operation == SetValueTypeEnum.Insert)
                            {
                                // Handle code name generation
                                itemObject.SetValue(ClassFieldObject.FieldName, string.Format("{0}_{1}_{2}", ClassName.Replace(".", "_"), ClassFieldObject.FieldName, DateTime.Now.ToString("yyyy-MM-dd")));
                            }
                            else if (Operation == SetValueTypeEnum.Insert && ClassFieldObject.FieldName.ToLower().Contains("sitename"))
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, SiteContext.CurrentSiteName);
                            }
                        }
                        else if (ClassFieldObject.DataType == ColumnTypeEnum.GUID)
                        {
                            // Handle Auto Guid
                            if (ClassFieldObject.FieldName.ToLower().EndsWith("guid") && Operation == SetValueTypeEnum.Insert)
                            {
                                itemObject.SetValue(ClassFieldObject.FieldName, Guid.NewGuid());
                            }
                        }
                    }
                }
                else if ((string.IsNullOrWhiteSpace(csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject])) && ClassFieldObject.DataType != ColumnTypeEnum.Text) ||
                    (ValidationHelper.GetString(csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject]), "").ToLower().Trim() == "null" && (ClassFieldObject.DataType != ColumnTypeEnum.Text || cbxTreatNullAsNull.Checked)))
                {
                    // No value, put NULL if allowed, otherwise default value
                    if (ClassFieldObject.AllowNull)
                    {
                        itemObject.SetValue(ClassFieldObject.FieldName, null);
                    }
                    else
                    {
                        itemObject.SetValue(ClassFieldObject.FieldName, ClassFieldObject.DefaultValue);
                    }
                }
                else
                {
                    itemObject.SetValue(ClassFieldObject.FieldName, csvRecord.GetField(ClassFieldToCSVField[ClassFieldObject]));
                }
            }
        }

        private bool ParseUpdateIndicator(string value)
        {
            bool Update = false;
            string CleanValue = value.Trim().ToLower();
            if (Boolean.TryParse(CleanValue, out Update))
            {
                return Update;
            }
            if (CleanValue == "1" || CleanValue == "y" || CleanValue == "yes" || CleanValue == "true" || CleanValue == "update")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool ParseDeleteIndicator(string value)
        {
            bool Delete = false;
            string CleanValue = value.Trim().ToLower();
            if (Boolean.TryParse(CleanValue, out Delete))
            {
                return Delete;
            }
            if (CleanValue == "1" || CleanValue == "y" || CleanValue == "yes" || CleanValue == "true" || CleanValue == "delete")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    /// <summary>
    /// Helper class that stores various configuration information about a Classes field, based on the Form XML
    /// </summary>
    public class ClassField
    {
        public ColumnTypeEnum DataType;
        public bool AllowNull;
        public string FieldName;
        public object DefaultValue;

        public ClassField(XmlDocument ClassFormXml, string FieldName)
        {
            DataType = ColumnTypeEnum.Text;
            this.FieldName = FieldName;
            XmlNode FieldNode = ClassFormXml.SelectSingleNode("//field[@column='" + FieldName + "']");
            string ColumnTypeValue = FieldNode.Attributes["columntype"] != null ? FieldNode.Attributes["columntype"].Value : "";
            string DefaultValueStr = (FieldNode.SelectSingleNode("./properties/defaultvalue") != null ? FieldNode.SelectSingleNode("./properties/defaultvalue").InnerText : "");
            AllowNull = ValidationHelper.GetBoolean(FieldNode.Attributes["allowempty"] != null ? FieldNode.Attributes["allowempty"].Value : "false", false);

            switch (ColumnTypeValue.ToLower())
            {
                case "binary":
                    DataType = ColumnTypeEnum.Binary;
                    DefaultValue = ValidationHelper.GetBinary(DefaultValueStr, (AllowNull ? null : new byte[] { }));
                    break;
                case "boolean":
                    DataType = ColumnTypeEnum.Boolean;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetBoolean(DefaultValueStr, false);
                    }
                    break;
                case "date":
                case "datetime":
                    DataType = ColumnTypeEnum.DateTime;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetDateTime(DefaultValueStr, new DateTime());
                    }
                    break;
                case "decimal":
                    DataType = ColumnTypeEnum.Decimal;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetDecimal(DefaultValueStr, 0);
                    }
                    break;
                case "double":
                    DataType = ColumnTypeEnum.Double;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetDouble(DefaultValueStr, 0);
                    }
                    break;
                case "integer":
                case "timespan":
                case "longinteger":
                    DataType = ColumnTypeEnum.Integer;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetInteger(DefaultValueStr, 0);
                    }
                    break;
                case "longtext":
                case "text":
                default:
                    DataType = ColumnTypeEnum.Text;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = DefaultValueStr;
                    }
                    break;
                case "guid":
                    DataType = ColumnTypeEnum.GUID;
                    if (string.IsNullOrWhiteSpace(DefaultValueStr))
                    {
                        if (AllowNull)
                        {
                            DefaultValue = null;
                        }
                    }
                    else
                    {
                        DefaultValue = ValidationHelper.GetGuid(DefaultValueStr, new Guid());
                    }
                    break;
            }

        }
    }

    public enum ColumnTypeEnum
    {
        Integer,
        GUID,
        DateTime,
        Text,
        Double,
        Binary,
        Boolean,
        Decimal
    }

    public enum SetValueTypeEnum
    {
        Insert, Update, Delete, Ignore
    }

    public enum InsertModeEnums
    {
        None, InsertAll, InsertNoIDValue
    }
    public enum UpdateModeEnums
    {
        None, UpdateAll, UpdateByIndicator
    }
    public enum DeleteModeEnums
    {
        None, DeleteAll, DeleteByIndicator
    }
}