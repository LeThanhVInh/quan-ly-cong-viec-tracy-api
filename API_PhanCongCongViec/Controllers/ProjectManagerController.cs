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
    public class ProjectManagerController : Controller
    {
        public object GetByProjectID(int projectID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator" || author == "ProjectManager")
                {
                    DataTable item = Connect.GetTable(@"select * from tb_PROJECT_MANAGER where projectID=@projectID",
                        new string[1] { "@projectID" },
                        new object[1] { projectID });
                    if (item != null)
                        if (item.Rows.Count > 0)
                            response = new ResponseJson(item, false, "");
                }
            }
            return response;
        }


        [HttpDelete]
        public object Delete(int userID, int projectID)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    if (Connect.Exec(@"delete from tb_PROJECT_MANAGER where userID=@userID and projectID=@projectID",
                        new string[2] { "@userID", "@projectID" },
                        new object[2] { userID, projectID })
                        )
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
                    if (author == "Administrator" || author == "ProjectManager")
                    {
                        if (item.userID.ToString().Trim() == "" || item.projectID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập đủ thông tin !");
                        else
                        {
                            if (Connect.Exec(@"INSERT INTO tb_PROJECT_MANAGER(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { item.userID, item.projectID })
                                )
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

    }
}
