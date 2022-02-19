using System;
using System.Data;
using System.Diagnostics;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Mvc;


namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TaskGroupController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"select * from tb_Task_Group where id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object getListByProjectID(int id, int pageNum, int pageSize)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                pageNum -= 1;
                if (pageNum <= 0) pageNum = 0;
                int pageStart = pageNum * pageSize;

                DataTable list = Connect.GetTable(@"
                                    SELECT * FROM tb_Task_Group TG
                                    WHERE projectID=@id 
                                    ORDER BY TG.id desc
                                    OFFSET " + pageStart + @" ROWS
                                    FETCH NEXT " + pageSize + @" ROWS ONLY;",
                                                new string[1] { "@id" },
                                                new object[1] { id });

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
                if (Connect.Exec(@"delete from tb_Task_Group where id=@id", new string[1] { "@id" }, new object[1] { id }))
                    response = new ResponseJson(null, false, "Đã xóa thành công !");
            }

            return response;
        }

        [HttpPost]
        public object insert([FromBody] dynamic item)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                try
                {
                    if (item.name.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa nhập Tên !");
                    else
                    {
                        if (Connect.Exec(@"INSERT INTO tb_Task_Group(name, description, projectID)
                                       VALUES (@name, @description ,@projectID ) ",
                                       new string[3] { "@name", "@description", "@projectID" },
                                       new object[3] { item.name.ToString(), item.description.ToString(), int.Parse(item.projectID.ToString()) }))
                            response = new ResponseJson(null, false, "Đã thêm thành công !");
                    }
                }
                catch (Exception ex)
                {
                    // Get stack trace for the exception with source file information
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(st.FrameCount - 1);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();

                    response = new ResponseJson(null, true, ex.Message + Environment.NewLine + "line: " + line);
                }
            }
            return response;
        }

        [HttpPut]
        public object update([FromBody] dynamic item)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                try
                {
                    if (item.name.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa nhập Tên !");
                    else
                    {
                        if (Connect.Exec(@"UPDATE tb_Task_Group
                                        SET
                                            name = @name
                                           ,description = @description 
                                       WHERE id = @id ",
                                       new string[3] { "@name", "@description", "@id" },
                                       new object[3] { item.name.ToString(), item.description.ToString(), int.Parse(item.id.ToString()) }))
                        {
                            response = new ResponseJson(null, false, "Đã cập nhật thành công !");
                        }
                        else
                            response = new ResponseJson(null, true, "Lỗi, Không lưu được !");
                    }
                }
                catch (Exception ex)
                {
                    // Get stack trace for the exception with source file information
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(st.FrameCount - 1);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();

                    response = new ResponseJson(null, true, ex.Message + Environment.NewLine + "line: " + line);
                }
            }
            return response;
        }
    }
}
