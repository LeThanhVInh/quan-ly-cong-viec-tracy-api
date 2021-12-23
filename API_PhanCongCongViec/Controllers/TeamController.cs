using System;
using System.Data;
using System.Diagnostics;
using API_PhanCongCongViec.Models;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Mvc;


namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TeamController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"select * from tb_TEAM where id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object getList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"
                            SELECT T.* ,
                               (select count(userID) from tb_Team_User where teamid=T.id) memberAmount
                            FROM tb_TEAM T ");

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
                if (Connect.Exec(@"delete from tb_TEAM where id=@id", new string[1] { "@id" }, new object[1] { id }))
                    response = new ResponseJson(null, false, "Đã xóa thành công !");
                else
                    response = new ResponseJson(null, true, "Đã có lỗi xảy ra !");
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
                        object newID = Connect.FirstResulfExec(@"INSERT INTO tb_TEAM(name) VALUES (@name ) select SCOPE_IDENTITY()",
                            new string[1] { "@name" }, new object[1] { item.name.ToString() });
                        if (newID != null)
                        {
                            if (item.userID.ToString() != "" && item.userID.ToString().Contains(","))
                            {
                                string[] id_array = item.userID.ToString().Split(",");
                                foreach (var userID in id_array)
                                {
                                    if (userID != "")
                                        StaticClass.InsertUserTeam(response, userID, newID.ToString());
                                }
                            }
                            response = new ResponseJson(null, false, "Đã thêm thành công !");
                        }
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
                        if (Connect.Exec(@"UPDATE tb_TEAM
                                        SET
                                            name = @name 
                                       WHERE id = @id ", new string[2] { "@name", "@id" }, new object[2] { item.name.ToString(), int.Parse(item.id.ToString()) }))
                        {
                            if (item.userID.ToString() != "" && item.userID.ToString().Contains(","))
                            {
                                string[] id_array = item.userID.ToString().Split(",");
                                foreach (var userID in id_array)
                                {
                                    if (userID != "")
                                        StaticClass.InsertUserTeam(response, userID, item.id.ToString());
                                }
                            }

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
