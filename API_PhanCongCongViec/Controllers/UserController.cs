using System;
using System.Data;
using System.Diagnostics;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Mvc;


namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        [HttpGet]
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"select * from tb_User where id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetFullName_ByUsername(string username)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable fullname = Connect.GetTable(@"select fullname from tb_User where username=@username", new string[1] { "@username" }, new object[1] { username });
                if (fullname != null)
                    response = new ResponseJson(fullname.Rows[0][0], false, "");
            }

            return response;
        }

        [HttpGet]
        public object getList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"SELECT U.*, UT.name userType
                                                    FROM tb_USER U LEFT JOIN tb_User_Type UT ON U.userTypeID=UT.id ");

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
                if (Connect.Exec(@"delete from tb_USER where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                    if (item.fullname.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa nhập Tên !");
                    else if (item.userTypeID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn loại thành viên !");
                    else
                    {
                        if (Connect.Exec(@"INSERT INTO tb_USER(fullname, userTypeID, username, password)
                                       VALUES (@fullname, @userTypeID, @username, @password) "
                                        , new string[4] { "@fullname", "@userTypeID", "@username", "@password" }
                                        , new object[4] { item.fullname.ToString()
                                                    , int.Parse(item.userTypeID.ToString())
                                                    , item.username.ToString()
                                                    , item.password.ToString() })
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
                    if (item.fullname.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa nhập Tên !");
                    else
                    {
                        if (Connect.Exec(@"UPDATE tb_USER
                                        SET
                                            fullname = @fullname
                                           ,userTypeID = @userTypeID
                                           ,username = @username
                                           ,password = @password
                                       WHERE id = @id "
                                        , new string[5] { "@fullname", "@userTypeID", "@username", "@password", "@id" }
                                        , new object[5] { item.fullname.ToString()
                                                    , int.Parse(item.userTypeID.ToString())
                                                    , item.username.ToString()
                                                    , item.password.ToString()
                                                    , int.Parse(item.id.ToString())})
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
