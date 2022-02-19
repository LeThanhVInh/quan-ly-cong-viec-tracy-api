using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TaskAttachmentController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;

        public TaskAttachmentController(IWebHostEnvironment environment)
        {
            _hostingEnvironment = environment;
        }

        [HttpGet]
        public object GetListByTaskID(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"
                                             select TA.*,
                                                    ('" + _hostingEnvironment.WebRootPath + @"/File/TaskAttachments/' +TA.filename + TA.extension) link
                                             from tb_Task_Attachment TA
                                             where TA.taskID=@id", new string[1] { "@id" }, new object[1] { id });

                if (list != null)
                    response = new ResponseJson(list, false, "");
            }

            return response;
        }

        [HttpDelete]
        public object Delete(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string link = _hostingEnvironment.WebRootPath + (Connect.getField("tb_task_Attachment", "('/File/TaskAttachments/' +filename + extension) link", "id", id) ?? "").ToString();
                if (Connect.Exec(@"delete from tb_Task_Attachment where id=@id", new string[1] { "@id" }, new object[1] { id }))
                {
                    if (System.IO.File.Exists(link))
                        System.IO.File.Delete(link);
                    response = new ResponseJson(null, false, "Đã xóa thành công !");
                }
            }

            return response;
        }


        [HttpPost]
        public async Task<object> upload([FromForm] List<IFormFile> files, int taskID)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");
            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string uploads = Path.Combine(_hostingEnvironment.WebRootPath, "File/TaskAttachments");
                foreach (IFormFile file in files)
                {
                    if (file.Length > 0)
                    {
                        string Time = DateTime.Now.ToString("dd.MM.yyyy.HH.mm.ss");
                        string filePath = Path.Combine(uploads, Time + "_" + file.FileName);
                        using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        #region Check fileType

                        string fileType = null;
                        string fileExtension = Path.GetExtension(file.FileName);
                        if (fileExtension == ".png" || fileExtension == ".jpeg" || fileExtension == ".jpg" || fileExtension == ".heic")
                            fileType = "image";
                        else if (fileExtension == ".doc" || fileExtension == ".docx")
                            fileType = "word";
                        else if (fileExtension == ".xlsx" || fileExtension == ".xls" || fileExtension == ".csv")
                            fileType = "excel";
                        else if (fileExtension == ".pdf")
                            fileType = "pdf";
                        else if (fileExtension == ".zip" || fileExtension == ".rar")
                            fileType = "archive";

                        #endregion
                        Connect.Exec(@"INSERT INTO tb_Task_Attachment(filename, extension, type, taskID)
                                       VALUES(@filename, @extension, @type, @taskID)",
                                       new string[] { "@filename", "@extension", "@type", "@taskID" },
                                       new object[] { Time + "_" + Path.GetFileNameWithoutExtension(file.FileName),
                                                       fileExtension,
                                                       fileType ?? Convert.DBNull,
                                                       taskID });
                    }
                }

                response = new ResponseJson(null, false, "");
            }
            return response;
        }
    }
}
