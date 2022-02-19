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
    public class TaskController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"
                                    SELECT T.* ,
                                           (SELECT TM.userID
                                            FROM tb_Task_Member TM
                                            WHERE TM.taskID=T.id
                                           ) memberID,
                                           (SELECT U.fullname
                                            FROM tb_Task_Member TM LEFT JOIN tb_User U ON U.id=TM.userID
                                            WHERE TM.taskID=T.id
                                           ) memberName,
                                            " + StaticClass.sqlGetTaskStatus + @"
                                    FROM tb_Task T
                                    WHERE T.id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetByTaskGroupID(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"
                                        SELECT * , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                        WHERE taskGroupID=@id",
                                        new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetMemberByProjectId(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"
                        --select userID,
                        --       ( ISNULL(UT.name,'Chưa phân quyền') +' - '+ ISNULL(U.fullname,'') ) selectionName
                        --from tb_Project_Manager PM  LEFT JOIN tb_USER U ON U.id=PM.userID
                        --                             LEFT JOIN tb_User_Type UT ON U.userTypeID=UT.id 
                        --where projectID=@id
                    --UNION
                        select userID,
                               ( ISNULL(UT.name,'Chưa phân quyền') +' - '+ ISNULL(U.fullname,'') ) selectionName
                        from tb_Project_Member PM LEFT JOIN tb_USER U ON U.id=PM.userID
                                                  LEFT JOIN tb_User_Type UT ON U.userTypeID=UT.id 
                        where projectID=@id ",
                    new string[1] { "@id" },
                    new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpDelete]
        public object Delete(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                if (Connect.Exec(@"delete from tb_Task where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                    else if (item.taskGroupID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Nhóm công việc !");
                    else if (item.memberID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                    else
                    {
                        string username_output = TokenManagerProvider.TokenManager.ValidateToken(Request.Headers["username"].ToString())[0];
                        string creatorID = (Connect.getField("tb_USER", "id", "username", username_output) ?? "").ToString();
                        object newID = Connect.FirstResulfExec(@"
                                    INSERT INTO tb_Task(name, description, taskGroupID, userCreateID, isActive, isFinished, startdate, enddate)
                                    VALUES (@name, @description, @taskGroupID, @userCreateID, 0, 0, @startdate, @enddate ) select SCOPE_IDENTITY()",
                                          new string[6] { "@name", "@description", "@taskGroupID", "@userCreateID", "@startdate", "@enddate" },
                                          new object[6] { item.name.ToString(),
                                                          item.description.ToString(),
                                                          int.Parse(item.taskGroupID.ToString()),
                                                          int.Parse(creatorID) ,
                                                          (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString())),
                                                          (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString()))
                                                         });
                        if (newID != null)
                        {
                            {
                                string[] memberID = (item.memberID.ToString() + ",").Split(',');
                                for (int i = 0; i < memberID.Length; i++)
                                {
                                    if (memberID[i] != "")
                                    {
                                        Connect.Exec(@"INSERT INTO tb_TASK_MEMBER(userID,taskID)
                                                       VALUES(@userID, @taskID)"
                                                    , new string[2] { "@userID", "@taskID" }
                                                    , new object[2] { memberID[i], newID });
                                    }
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
                    else if (item.taskGroupID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Nhóm công việc !");
                    else if (item.memberID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                    else
                    {
                        if (Connect.Exec(@"UPDATE tb_Task
                                        SET
                                            name = @name
                                          , description = @description
                                          , taskGroupID  = @taskGroupID
                                          , startdate = @startdate
                                          , enddate = @enddate
                                       WHERE id = @id ",
                                       new string[6] { "@name", "@description", "@taskGroupID", "@startdate", "@enddate", "@id" },
                                       new object[6] { item.name.ToString(),
                                                       item.description.ToString(),
                                                       int.Parse(item.taskGroupID.ToString()),
                                                       (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString())),
                                                       (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString())),
                                                       int.Parse(item.id.ToString()) }))
                        {
                            {
                                Connect.Exec("delete tb_Task_Member where taskID=@id", new string[1] { "@id" }, new object[1] { int.Parse(item.id.ToString()) });
                                string[] memberID = (item.memberID.ToString() + ",").Split(',');
                                for (int i = 0; i < memberID.Length; i++)
                                {
                                    if (memberID[i] != "")
                                    {
                                        Connect.Exec(@"INSERT INTO tb_TASK_MEMBER(userID,taskID)
                                                       VALUES(@userID, @taskID)"
                                                    , new string[2] { "@userID", "@taskID" }
                                                    , new object[2] { memberID[i], int.Parse(item.id.ToString()) });
                                    }
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

        [HttpPut]
        public object setStatus([FromBody] dynamic item)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã có lỗi xảy ra");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                try
                {
                    if (item.status.ToString().Trim() == "" || item.taskID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa có đủ thông số !");
                    else
                    {
                        if (item.status.ToString() == "1")
                            item.finishPercent = 100;

                        if (Connect.Exec(@"UPDATE tb_Task
                                        SET
                                            isFinished = @isFinished 
                                          , finishPercent = @finishPercent
                                          , failReason = @failReason
                                       WHERE id = @id ",
                                       new string[4] { "@isFinished", "@finishPercent", "@failReason", "@id" },
                                       new object[4] { int.Parse(item.status.ToString()),
                                                      int.Parse(item.finishPercent.ToString()),
                                                      item.failReason.ToString(),
                                                      int.Parse(item.taskID.ToString()) }))
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
