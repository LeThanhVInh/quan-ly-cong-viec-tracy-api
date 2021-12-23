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
    public class ProjectController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"SELECT P.*, DE.name departmentName
                                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                                    WHERE P.id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "âaâ");
            }
            return response;
        }

        [HttpGet]
        public object getList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"SELECT P.*, DE.name departmentName
                                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id");
                if (list != null)
                    response = new ResponseJson(list, false, "");
            }

            return response;
        }

        [HttpGet]
        public object getListByPageNumber(int pageNum, int pageSize)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                pageNum -= 1;
                if (pageNum <= 0) pageNum = 0;
                int pageStart = pageNum * pageSize;

                DataTable list = Connect.GetTable(@"
                                    SELECT P.*, DE.name departmentName
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                    ORDER BY P.id
                                    OFFSET " + pageStart + @" ROWS
                                    FETCH NEXT " + pageSize + @" ROWS ONLY;");
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
                if (Connect.Exec(@"delete from tb_PROJECT where id=@id", new string[1] { "@id" }, new object[1] { id }))
                    response = new ResponseJson(null, false, "Đã xóa thành công !");
                else
                    response = new ResponseJson(null, true, "Không có dữ liệu !");
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
                        if (Connect.Exec(@"INSERT INTO tb_PROJECT(name, startdate, enddate, isPriority, departmentID)
                                           VALUES (@name, @startdate, @enddate, @isPriority, @departmentID ) ",

                                           new string[5] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID" },
                                           new object[5] { item.name.ToString(),
                                                           DateTime.Parse(item.startdate.ToString()),
                                                           DateTime.Parse(item.enddate.ToString()),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()) })
                            )
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
                        if (Connect.Exec(@"UPDATE tb_PROJECT
                                        SET
                                            name = @name
                                           ,startdate = @startdate
                                           ,enddate = @enddate
                                           ,isPriority = @isPriority
                                           ,departmentID = @departmentID
                                       WHERE id = @id ",
                                           new string[6] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID", "@id" },
                                           new object[6] { item.name.ToString(),
                                                           DateTime.Parse(item.startdate.ToString()),
                                                           DateTime.Parse(item.enddate.ToString()),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()),
                                                           int.Parse(item.id.ToString()) })
                            )
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
