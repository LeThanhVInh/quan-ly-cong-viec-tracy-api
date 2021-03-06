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
    public class ProcedureController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    DataTable item = Connect.GetTable(@"select * from tb_procedure where id=@id", new string[1] { "@id" }, new object[1] { id });
                    if (item != null)
                        if (item.Rows.Count > 0)
                            response = new ResponseJson(item, false, "");
                }
            }
            return response;
        }

        [HttpGet]
        public object getList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator" || author == "ProjectManager")
                {
                    DataTable list = Connect.GetTable(@"select * from tb_procedure");

                    if (list != null)
                        response = new ResponseJson(list, false, "");
                }
            }

            return response;
        }

        [HttpDelete]
        public object Delete(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    if (Connect.Exec(@"delete from tb_procedure where id=@id", new string[1] { "@id" }, new object[1] { id }))
                        response = new ResponseJson(null, false, "Đã xóa thành công !");
                }
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
                    string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                    if (author == "Administrator")
                    {
                        if (item.name.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập Tên !");
                        else
                        {
                            object newID = Connect.FirstResulfExec(@"INSERT INTO tb_procedure(name,note)
                                       VALUES (@name ,@note ) select SCOPE_IDENTITY()", new string[2] { "@name", "@note" }, new object[2] { item.name.ToString(), item.note.ToString() });

                            if (newID != null)
                                response = new ResponseJson(newID.ToString(), false, "Đã thêm thành công !");
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
                        string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                        if (author == "Administrator")
                        {
                            if (Connect.Exec(@"UPDATE tb_procedure
                                        SET
                                            name = @name
                                           ,note = @note
                                       WHERE id = @id ", new string[3] { "@name", "@note", "@id" }
                                                           , new object[3] { item.name.ToString(), item.note.ToString(), int.Parse(item.id.ToString()) }))
                            {
                                response = new ResponseJson(null, false, "Đã cập nhật thành công !");
                            }
                            else
                                response = new ResponseJson(null, true, "Lỗi, Không lưu được !");
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
    }
}
