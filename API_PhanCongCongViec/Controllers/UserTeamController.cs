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
    public class UserTeamController : Controller
    {
        [HttpGet]
        public object getListByTeamID(int teamID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator" || author == "ProjectManager")
                {
                    DataTable list = Connect.GetTable(@"
                        SELECT T.name TeamName, U.fullname UserFullName , TU.*
                        FROM tb_TEAM_USER TU LEFT JOIN tb_User U ON U.id=TU.userID
                                             LEFT JOIN tb_Team T ON T.id=TU.teamID
                        WHERE TU.teamID = @teamID ",
                                                        new string[1] { "@teamID" },
                                                        new object[1] { teamID });

                    if (list != null)
                        response = new ResponseJson(list, false, "");
                }
            }

            return response;
        }

        [HttpDelete]
        public object Delete(int teamID, int userID)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    if (Connect.Exec(@"delete from tb_TEAM_USER where teamID=@teamID and userID=@userID",
                                    new string[2] { "@teamID", "@userID" },
                                    new object[2] { teamID, userID })
                        )
                        response = new ResponseJson(null, false, "Đã xóa thành công !");
                    else
                        response = new ResponseJson(null, true, "Đã có lỗi xảy ra !");
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
                        response = StaticClass.InsertUserTeam(response, item.userID.ToString(), item.teamID.ToString());
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
