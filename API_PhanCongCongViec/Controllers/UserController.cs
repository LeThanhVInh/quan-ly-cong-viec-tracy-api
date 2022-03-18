using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    DataTable item = Connect.GetTable(@"select * from tb_User where id=@id", new string[1] { "@id" }, new object[1] { id });
                    if (item != null)
                        if (item.Rows.Count > 0)
                            response = new ResponseJson(item, false, "");
                }
            }
            return response;
        }

        [HttpGet]
        public object GetFullName_ByUsername(string username)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                username = TokenManagerProvider.TokenManager.ValidateToken(username)[0];
                DataTable fullname = Connect.GetTable(@"select fullname from tb_User where username=@username", new string[1] { "@username" }, new object[1] { username });
                if (fullname != null)
                    response = new ResponseJson(fullname.Rows[0][0], false, "");
            }

            return response;
        }

        [HttpGet]
        public object getByTeamID(string teamID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator" || author == "ProjectManager")
                {
                    if (teamID.Trim() != "")
                    {
                        DataTable list = Connect.GetTable(@"
                            SELECT TU.*, U.fullname
                            FROM tb_TEAM_USER TU LEFT JOIN tb_USER U ON U.id=TU.userID
                            WHERE TU.teamID IN
                                        (select name from SplitString(@arr,',')) ", new string[1] { "@arr" }, new object[1] { teamID });

                        if (list != null)
                            response = new ResponseJson(list, false, "");
                    }
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
                    DataTable list = Connect.GetTable(@"SELECT U.*, UT.name userType 
                                                           , ( --ISNULL(UT.name,'Chưa phân quyền') +' - '+
                                                               ISNULL(U.fullname,'') ) selectionName
                                                    FROM tb_USER U LEFT JOIN tb_User_Type UT ON U.userTypeID=UT.id ");

                    if (list != null)
                        response = new ResponseJson(list, false, "");
                }
            }

            return response;
        }

        [HttpGet]
        public object getTelegramConfirmedList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator" || author == "ProjectManager")
                {
                    DataTable list = Connect.GetTable(@"SELECT U.*, UT.name userType 
                                                           , ( --ISNULL(UT.name,'Chưa phân quyền') +' - '+
                                                               ISNULL(U.fullname,'') ) selectionName
                                                    FROM tb_USER U LEFT JOIN tb_User_Type UT ON U.userTypeID=UT.id
                                                    WHERE U.isTelegramConfirm = 1 ");

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
                    if (Connect.Exec(@"delete from tb_USER where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                        if (item.fullname.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập Tên !");
                        else if (item.userTypeID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn loại thành viên !");
                        else if (item.phoneNumber.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập số điện thoại !");
                        else
                        {
                            string username_check = (Connect.getField("tb_User", "username", "username", item.username.ToString().Trim()) ?? "").ToString();
                            if (username_check != "")
                                response = new ResponseJson(null, true, "Tên đăng nhập đã tồn tại !");
                            else
                            {
                                if (Connect.Exec(@"INSERT INTO tb_USER(fullname, userTypeID, username, password, phoneNumber)
                                           VALUES (@fullname, @userTypeID, @username, @password, @phoneNumber) "
                                                    , new string[5] { "@fullname", "@userTypeID", "@username", "@password", "@phoneNumber" }
                                                    , new object[5] { item.fullname.ToString().Trim()
                                                    , int.Parse(item.userTypeID.ToString())
                                                    , (item.username.ToString().Trim() == "" ? Convert.DBNull : item.username.ToString().Trim())
                                                    , (item.password.ToString().Trim() == "" ? Convert.DBNull : item.password.ToString().Trim())
                                                    , item.phoneNumber.ToString().Trim()
                                                    })
                                        )
                                    response = new ResponseJson(null, false, "Đã thêm thành công !");
                            }
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
                    string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                    if (author == "Administrator" || author == "ProjectManager")
                    {
                        if (item.fullname.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập Tên !");
                        else if (item.userTypeID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn loại thành viên !");
                        else if (item.phoneNumber.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập số điện thoại !");
                        else
                        {
                            string id_check = (Connect.getField("tb_User", "id", "username", item.username.ToString().Trim()) ?? "").ToString();
                            string username_check = (Connect.getField("tb_User", "username", "username", item.username.ToString().Trim()) ?? "").ToString();
                            if (username_check != "" && id_check != item.id.ToString())
                                response = new ResponseJson(null, true, "Tên đăng nhập đã tồn tại !");
                            else
                            {
                                bool isTelegramConfirm = (bool)Connect.getField("tb_User", "ISNULL(isTelegramConfirm,0)", "id", int.Parse(item.id.ToString()));
                                string phoneNumber = Connect.getField("tb_User", "phoneNumber", "id", int.Parse(item.id.ToString())).ToString();
                                if (item.phoneNumber.ToString().Trim() != phoneNumber.Trim())
                                    isTelegramConfirm = false;

                                if (Connect.Exec(@"UPDATE tb_USER
                                        SET
                                            fullname = @fullname
                                           ,userTypeID = @userTypeID
                                           ,username = @username
                                           ,password = @password
                                           ,phoneNumber = @phoneNumber
                                           ,isTelegramConfirm = @isTelegramConfirm
                                       WHERE id = @id "
                                                , new string[7] { "@fullname", "@userTypeID", "@username", "@password", "@phoneNumber", "@isTelegramConfirm", "@id" }
                                                , new object[7] { item.fullname.ToString().Trim()
                                                    , int.Parse(item.userTypeID.ToString())
                                                    , (item.username.ToString().Trim() == "" ? Convert.DBNull : item.username.ToString().Trim())
                                                    , (item.password.ToString().Trim() == "" ? Convert.DBNull : item.password.ToString().Trim())
                                                    , item.phoneNumber.ToString().Trim()
                                                    , (isTelegramConfirm ? 1 : 0)
                                                    , int.Parse(item.id.ToString())})
                                    )
                                {
                                    response = new ResponseJson(null, false, "Đã cập nhật thành công !");
                                }
                                else
                                    response = new ResponseJson(null, true, "Lỗi, Không lưu được !");
                            }
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

        [HttpPost]
        public object ChangePassword([FromBody] dynamic item)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                try
                {
                    int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                    string oldPasswordDB = (Connect.getField("tb_User", "password", "id", authorID) ?? "0").ToString();

                    if (item.oldPassword.ToString().Trim() == "" || item.newPassword.ToString().Trim() == "" || item.confirmPassword.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa nhập đủ thông tin !");
                    else if (item.newPassword.ToString().Trim() != item.confirmPassword.ToString().Trim())
                        response = new ResponseJson(null, true, "Mật khẩu mới không khớp !");
                    else if (item.oldPassword.ToString().Trim() != oldPasswordDB)
                        response = new ResponseJson(null, true, "Mật khẩu cũ không đúng !");
                    else
                    {
                        if (Connect.Exec(@"UPDATE tb_USER
                                        SET 
                                           password = @password 
                                       WHERE id = @id "
                                        , new string[2] { "@password", "@id" }
                                        , new object[2] {
                                                (item.newPassword.ToString().Trim() == "" ? Convert.DBNull : item.newPassword.ToString().Trim())
                                                    , authorID
                                        })
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
