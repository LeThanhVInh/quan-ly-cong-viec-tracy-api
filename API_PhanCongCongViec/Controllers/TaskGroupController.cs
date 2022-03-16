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
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                int taskGroupID = int.Parse((Connect.getField("tb_Task", "taskGroupID", "id", id) ?? "0").ToString());
                int projectID = int.Parse((Connect.getField("tb_Task_Group", "projectID", "taskGroupID", taskGroupID) ?? "0").ToString());
                int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", projectID) ?? "0").ToString());

                if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                {
                    DataTable item = Connect.GetTable(@"select * from tb_Task_Group where id=@id", new string[1] { "@id" }, new object[1] { id });
                    if (item != null)
                        if (item.Rows.Count > 0)
                            response = new ResponseJson(item, false, "");
                }
            }
            return response;
        }

        [HttpGet]
        public object getListByProjectID(int id, int pageNum, int pageSize)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                pageNum -= 1;
                if (pageNum <= 0) pageNum = 0;
                int pageStart = pageNum * pageSize;

                #region sql
                string sql = @" SELECT * FROM tb_Task_Group TG
                                WHERE projectID=@id 
                                ORDER BY TG.id desc
                                OFFSET " + pageStart + @" ROWS
                                FETCH NEXT " + pageSize + @" ROWS ONLY; ";

                if (author == "ProjectManager")//sql chưa đúng
                {
                    #region sql
                    sql = @"  SELECT TG.*
                              FROM tb_Task_Group TG LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                    LEFT JOIN tb_Project_Member PMm ON PMm.projectID=TG.projectID
                              WHERE TG.projectID=@id
                                    and ( PM.userID=" + authorID + @" OR PMm.userId =" + authorID + @" )
                              GROUP BY TG.id, TG.projectID, TG.name, TG.[description]
                              ORDER BY TG.id desc
                              OFFSET " + pageStart + @" ROWS
                              FETCH NEXT " + pageSize + @" ROWS ONLY; ";
                    #endregion
                }

                if (author == "Member")//sql chưa đúng
                {
                    #region sql
                    sql = @"  SELECT * FROM tb_Task_Group TG LEFT JOIN tb_Project_Member PM ON PM.projectID=TG.projectID
                              WHERE TG.projectID=@id
                                    and PM.userID=" + authorID + @"
                              ORDER BY TG.id desc
                              OFFSET " + pageStart + @" ROWS
                              FETCH NEXT " + pageSize + @" ROWS ONLY; ";
                    #endregion
                }
                #endregion

                DataTable list = Connect.GetTable(sql, new string[1] { "@id" }, new object[1] { id });

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
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                int projectID = int.Parse((Connect.getField("tb_Task_Group", "projectID", "id", id) ?? "0").ToString());
                int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", projectID) ?? "0").ToString());

                if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                {
                    if (Connect.Exec(@"delete from tb_Task_Group where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                    int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                    int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", int.Parse(item.projectID.ToString())) ?? "0").ToString());

                    if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
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
                    int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                    int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", int.Parse(item.projectID.ToString())) ?? "0").ToString());

                    if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
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
