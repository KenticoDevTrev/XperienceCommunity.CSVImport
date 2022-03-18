<%@ Page Language="C#" AutoEventWireup="true" EnableEventValidation="false" Inherits="XperienceCommunity.CSVImport.CSVImport" MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" Theme="Default" %>

<asp:Content runat="server" ContentPlaceHolderID="plcContent" ID="plcCSVImport">
    <style>
        .cms-bootstrap .form-control.mapping-control, 
        .cms-bootstrap .form-label.mapping-label {
          display: inline-block !important;
          width: auto;
        }
        .cms-bootstrap .form-label.mapping-label {
            padding-left: 15px;
        }
        .clear {
            display:block;
            clear:both;
        }
        .editing-form-value-cell em {
          word-spacing: 0;
          display: block;
        }
    </style>

    <div class="container">
        <asp:Panel runat="server" Visible="true" ID="pnlSelectTable">
            <h3>Select your Table</h3>
            <div class="form-horizontal">
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlClasses" Text="Class" CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlClasses" CssClass="dropdown" AutoPostBack="true" OnSelectedIndexChanged="ddlClasses_SelectedIndexChanged" />
                    </div>
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlGetOrUploadCSV" Visible="false">
            <style>
                .ConfigureAndImport {
                    display:none;
                }
            </style>
            <h3>Upload CSV File</h3>
            <p>Upload your CSV File, if you do not have one, you can <asp:LinkButton runat="server" ID="btnGetCSV" ClientIDMode="Static" CssClass="getCSVLink" Text="click here to generate a default csv file" /> (only available in comma separation).</p>
            <p>***Note: For maximum compatability, please make sure your CSV is saved as UTF-8, Unicode, or UTF-32. <a href="https://www.xadapter.com/how-to-save-csv-excel-file-as-utf-8-encoded" target="_blank">Learn how to encode your CSV</a>.***</p>
            <p>Delimiter: 
                <asp:DropDownList runat="server" ID="ddlDelimiter" CssClass="form-control" ClientIDMode="Static">
                <asp:ListItem Text="comma (,)" Value=","></asp:ListItem>
                <asp:ListItem Text="semi-colon (;)" Value=";"></asp:ListItem>
                    <asp:ListItem Text="pipe (|)" Value="|"></asp:ListItem>
            </asp:DropDownList>
            <div class="form-horizontal">
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="fileCSV" Text="Upload CSV" CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <asp:FileUpload runat="server" ID="fileCSV" />
                    </div>
                </div>
                 <div class="form-group">
                    <asp:Button runat="server" ID="btnParseCSV" CssClass="btn btn-default" OnClick="btnParseCSV_Click" Text="Upload CSV" />
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlCSVMapAndSettings" Visible="false" CssClass="ConfigureAndImport">
            <h3>Configure and Import</h3>
            <p>Please configure your import.  Hover over each element to learn more about the setting.</p>
            <div class="form-horizontal">
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="cbxTreatNullAsNull" Text="Text NULL to set Null value" ToolTip="If checked, any text field that has NULL in it will have a database null value placed in it, instead of the text 'NULL.'  Non text fields with NULL will put a null value, unless nulls are not allowed for that field." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <asp:CheckBox ID="cbxTreatNullAsNull" Checked="true" runat="server" />
                    </div>
                </div>
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlInsertMode" Text="Insert Mode" ToolTip="If set to 'None' then no insertions will be processed.  &#10;If set to 'Insert All' then every record will be treated as an insert.    &#10;If set to 'Insert By No Value in Identifier' is selected, then any entry that does not contain a row id will be inserted as a new record." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlInsertMode" ToolTip="If set to 'None' then no insertions will be processed. &#10;If set to 'Insert All' then every record will be treated as an insert.  &#10;If set to 'Insert By No Value in Identifier' is selected, then any entry that does not contain a row id will be inserted as a new record.">
                            <asp:ListItem Text="None" Value="" />
                            <asp:ListItem Text="Insert All" Value="InsertAll" />
                            <asp:ListItem Text="Insert By No Value in Identifier" Value="Insert" Selected="True" />
                        </cms:CMSDropDownList>
                    </div>
                </div>
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlIdentifierField" Text="Identifier Mapping" ToolTip="Must select the field that matches this classes ID field." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlIdentifierField" ToolTip="Must select the field that matches this classes ID field"></cms:CMSDropDownList>
                        <em>If this is left blank, no Updates or Deletes can occur and every entry will be inserted as long as the Insert Mode is not "None"</em>
                    </div>
                    
                </div>
                
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlUpdateMode" Text="Update Mode" ToolTip="If set to 'None' then any record with a row ID will not be processed.  &#10;If set to 'Update All', then any entry that contains a row id will be update. &#10;If set to 'Update by Indicator' then if an entry's Update Indicator Field in the below mapping resolves to true then that record will be updated." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlUpdateMode" ToolTip="If set to 'None' then any record with a row ID will not be processed.  &#10;If set to 'Update All', then any entry that contains a row id will be update. &#10;If set to 'Update by Indicator' then if an entry's Update Indicator Field in the below mapping resolves to true then that record will be updated.">
                            <asp:ListItem Text="None" Value=""/>
                            <asp:ListItem Text="Update All" Value="UpdateAll" />
                            <asp:ListItem Text="Update By Indicator" Value="UpdateByIndicator" Selected="True"  />
                        </cms:CMSDropDownList>
                    </div>
                </div>
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlUpdateIndicator" Text="Update Indicator Mapping" ToolTip="Optional, this boolean (ex 'true' or 'false') containing field indicates if a record should be updated." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlUpdateIndicator" ToolTip="Must select the field that matches this classes ID field"></cms:CMSDropDownList>
                    </div>
                    <em>Any of the following values is treated as an indicator to update: 1, y, yes true, update</em>

                </div>
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlDeleteMode" Text="Delete Mode" ToolTip="If set to 'None' then no records will be deleted.  &#10;If set to 'Delete All', all the records will be deleted prior to import. &#10;If set to 'Delete by Indicator' then if an entry's Delete Indicator Field in the below mapping resolves to true then that record will be delete." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlDeleteMode" ToolTip="If set to 'None' then no records will be deleted.  &#10;If set to 'Delete All', all the records will be deleted prior to import. &#10;If set to 'Delete by Indicator' then if an entry's Delete Indicator Field in the below mapping resolves to true then that record will be delete.">
                            <asp:ListItem Text="None" Value="" />
                            <asp:ListItem Text="Delete All" Value="DeleteAll" />
                            <asp:ListItem Text="Delete By Indicator" Value="DeleteByIndicator" Selected="True" />
                        </cms:CMSDropDownList>
                    </div>
                    <em>Any of the following values is treated as an indicator to delete: 1, y, yes true, delete</em>
                </div>
                <div class="form-group">
                    <div class="editing-form-label-cell">
                        <cms:FormLabel runat="server" AssociatedControlID="ddlDeleteIndicator" Text="Delete Indicator Mapping" ToolTip="Optional, this boolean (ex 'true' or 'false') containing field indicates if a record should be deleted." CssClass="control-label" />
                    </div>
                    <div class="editing-form-value-cell">
                        <cms:CMSDropDownList runat="server" ID="ddlDeleteIndicator" ToolTip="Must select the field that matches this classes ID field"></cms:CMSDropDownList>
                    </div>
                </div>
                
                <h4>Table Mapping</h4>
                <p>Map the CSV Field (left) to the Class Field (Right).<br />
                    Use "Auto" for fields that should be automatically handled on insert/update, such as the Created By/Modified By, Created/Modified When, and GUID.<br />
                </p>
                <asp:PlaceHolder runat="server" ID="plcFields">

                </asp:PlaceHolder>

                 <div class="form-group">
                    <asp:Button runat="server" ID="btnProcessImport" CssClass="btn btn-default" OnClick="btnProcessImport_Click" Text="Import" />
                </div>
            </div>

        </asp:Panel>
    </div>
</asp:Content>
