using CMS.UIControls;
using System;
using System.Linq;
using CMS.Helpers;
using CMS.DataEngine;
using CMS.ImportExport;
using System.Data;


namespace XperienceCommunity.CSVImport
{
    [UIElement("CSVImport", "CSVImport", CheckPermissions = true)]
    public partial class CSVImportGetCSV : CMSPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            int ClassID = QueryHelper.GetInteger("ClassID", -1);
            DataClassInfo ClassObject = DataClassInfoProvider.GetClasses().WhereEquals("ClassID", ClassID).Where("(ClassIsCustomTable =1 or ClassIsForm = 1 or ClassResourceID in (Select ResourceID from CMS_Resource where ResourceVersion is not null))").FirstOrDefault();
            if (ClassObject != null)
            {
                int totalRecords = 0;
                DataSet TableDS = ConnectionHelper.ExecuteQuery(ClassObject.ClassName + ".selectall", null, null, null, -1, null, -1, -1, ref totalRecords);
                DataExportHelper exporter = new DataExportHelper(TableDS);
                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream())
                {
                    // Add indicator columns
                    TableDS.Tables[0].Columns.Add("UpdateIndicator", typeof(bool));
                    TableDS.Tables[0].Columns.Add("DeleteIndicator", typeof(bool));
                    exporter.ExportToCSV(TableDS, 0, memStream);

                    // Transmit through stream
                    Response.Clear();
                    Response.ClearHeaders();

                    Response.ContentType = "application/octet-stream";
                    Response.AddHeader("content-disposition", "attachment;filename=\"" + ClassObject.ClassName.Replace(".", "_") + "_Export.csv\"");

                    byte[] bytesInStream = memStream.ToArray(); // simpler way of converting to array
                    memStream.Close();
                    Response.BinaryWrite(bytesInStream);
                    Response.End();

                }


            }
        }


    }
}