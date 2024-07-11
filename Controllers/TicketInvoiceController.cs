using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Diagnostics;
using System.Runtime;
using WebSupergoo.ABCpdf12;

namespace DCElectricWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketInvoiceController : ControllerBase
    {
        string bucket;
        string sql;
        DefaultDataLayer dl = new DefaultDataLayer();

        [HttpGet]
        public async Task<IActionResult> Get(int id=0, string template="", string referenceCode = "", string sid = "")
        {

            /* start security and data validation */
            var um = new UserModule(sid);
            if (!um.Secured) return Unauthorized();// new HttpResponseMessage(HttpStatusCode.Unauthorized);

            //sql = $@"Select * from Report where ReferenceCode='" + referenceCode + "'";
            //DataSet reportDataSet = dl.GetData(sql);
            //if (reportDataSet.Tables.Count == 0) return NoContent();
            //if (reportDataSet.Tables[0].Rows.Count == 0) return NoContent();







            var isProduction = this.dl.ConnectionString.IndexOf("Production") > 0;
            bucket = isProduction ? "fleet-files" : "fleet-files-dev";
            var gsm = new GoogleStorageModule();
 
            var isValid = XSettings.InstallLicense(@"[X/VKS0cPn5FgsCJaaK+NbIL+Lb9IQ4MYlq3wxL3
FA0ojxkiVPH3rYMVWQ0lkwg8KCtYw4j5AuSAdr6I
mQbV9xFMgfGSVBH423zFMO/XgBjbi1y7S5MlUFrj
UWBKMcmImUL1oUMFb8wtwCFVMoSiSIEERXiebQ2W
5r+Qn81U4T+/CpdgBuze3yXnWsbpWyRJddxMW83l
QxH+Ofn0BHagRllgNxXXMN6ZhZPv+MFaRTODAdik
liL6wAD07iDOwYuA=]");

            /* Generate a Doc object */
            Doc doc = new Doc();

            /* Get the template from google storage */
             doc = this.LoadTemplateFile(doc, template);

            /* Pull the form data out of the document */
            byte[] theData = null;
            theData = doc.GetData();
            XForm f = doc.Form;




            /* this loop is because reflection (GetProperty) doesn't seem to work with xForm.  */
            foreach (var field in f.Fields)
            {

                /* default values for TicketInvoice */
                if (field.Name == "CustomerName") f.Fields["CustomerName"].Value = $"Morris Development";
              
            }



            MemoryStream ms = new MemoryStream();

            doc.Save(ms);
            //Response.Headers.Add("Content-Disposition", "inline; filename=test.pdf");


            Byte[] b;
            b = ms.ToArray();
            string reportName = template.Replace(" ", "_");
            //var response = new HttpResponseMessage(HttpStatusCode.OK);
            //response.Content = new ByteArrayContent(b);
            //string downloadConfig = "inline; filename=" + reportName + ".pdf";

            //response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            //response.Content.Headers.Add(@"Content-Length", b.Length.ToString());

            //If the position is not set to '0' then the PDF will be empty. 
            ms.Position = 0;

            //Download the PDF document in the browser. 
            Response.Headers.Add(@"content-disposition", $"inline; filename={reportName}.pdf");

            return File(ms, "application/pdf");


            //FileStreamResult fileStreamResult = new FileStreamResult(ms, "application/pdf");

            //fileStreamResult.FileDownloadName = "Sample.pdf";
            // return fileStreamResult;

            //  return response;
        }


        private Doc LoadTemplateFile(Doc doc, string template)
        {

            template = template.Replace("'", "''");

            var dl = new DefaultDataLayer();
            var sql = $"Select TemplateFile, GoogleStorageURL FROM  ReportTemplate where ISNULL(ReportTemplateName , ReportName) = '{template}' and Status='Active';";
            var ds = dl.GetData(sql);
            MemoryStream form;
            var gs = new GoogleStorageModule();
            form = gs.GetFile(bucket, template);
            doc.Read(form);
    
      





            return doc;
        }




        private void updateField(XForm form, string fieldName, string value)
        {



            foreach (var field in form.Fields)
            {
                Debug.WriteLine(field.Name);

                if (field.Name.ToLower() == fieldName.ToLower())
                {

                    if (field.FieldType == WebSupergoo.ABCpdf12.Objects.FieldType.Checkbox)
                    {

                        if (value.ToLower() == "yes" || value.ToLower() == "on" || value.ToLower() == "checked" || value.ToLower() == "true" || value.ToLower() == "1") { field.Value = field.Options[0]; } else { field.Value = "off"; }
                    }
                    else
                    {

                        field.Value = value;

                    }

                }
            }
        }

    }
}

