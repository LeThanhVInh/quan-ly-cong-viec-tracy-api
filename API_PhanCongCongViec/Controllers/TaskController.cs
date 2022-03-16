using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
                                    SELECT T.* , U.fullName CreatorName , ISNULL(WF.name, N'Chưa phân loại') WorkFlowName,
                                           (SELECT TM.userID
                                            FROM tb_Task_Member TM
                                            WHERE TM.taskID=T.id
                                           ) memberID,
                                           (SELECT U.fullname
                                            FROM tb_Task_Member TM LEFT JOIN tb_User U ON U.id=TM.userID
                                            WHERE TM.taskID=T.id
                                           ) memberName,
                                            TG.name TaskGroupName,
                                            " + StaticClass.sqlGetTaskStatus + @"
                                    FROM tb_Task T
                                                    LEFT JOIN tb_Task_Group TG ON TG.id=T.taskgroupID
                                                    LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                    LEFT JOIN tb_Work_Flow WF ON WF.id=T.workFlowId
                                    WHERE T.id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetByTaskGroupID(int id, string status, string taskMemberID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                #region sql

                string sql = @"         SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                        WHERE T.taskGroupID=@id ";
                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @"            SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                                       LEFT JOIN tb_Task_Group TG ON TG.id=T.taskGroupID
                                                       LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID 
                                        WHERE T.taskGroupID=@id
                                              and (PM.userID=" + authorID + " OR TM.userID=" + authorID + " )";
                    #endregion
                }
                if (author == "Member")
                {
                    #region sql
                    sql = @"            SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id 
                                        WHERE T.taskGroupID=@id
                                              and TM.userID=" + authorID;
                    #endregion
                }

                string[] stringParam = new string[] { "@id" };
                object[] objectParam = new object[] { id };

                #region status filter
                if (status == "WaitingTask")
                    sql += @"                  and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 and T.isFinished = 3 ";
                else if (status == "FailedTask")
                    sql += @"                  and T.isFinished = 2 ";
                else if (status == "AccomplishedTask")
                    sql += @"                  and T.finishPercent=100 and T.isFinished=1 ";
                else if (status == "ProcessingTask")
                    sql += @"                  and DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0 ";
                else if (status == "LateTask")
                    sql += @"                  and T.enddate < GETDATE() and T.finishPercent < 100 and T.isFinished = 0 ";
                #endregion

                #region filter task member
                if ((taskMemberID ?? "").Trim() != "")
                {
                    sql += @"                  and TM.userID=@taskMemberID ";

                    stringParam = new string[] { "@id", "@taskMemberID" };
                    objectParam = new object[] { id, taskMemberID };
                }
                #endregion

                #endregion

                DataTable list = Connect.GetTable(sql, stringParam, objectParam);
                if (list != null)
                    if (list.Rows.Count > 0)
                        response = new ResponseJson(list, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetByProjectID(int id, string status, string taskMemberID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                #region sql
                string sql = @"         SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                TG.name TaskGroupName,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Group TG ON TG.id=T.taskGroupID
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                        WHERE TG.projectID=@id ";

                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @"         SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                TG.name TaskGroupName,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Group TG ON TG.id=T.taskGroupID
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                                       LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                        WHERE TG.projectID=@id
                                                and (PM.userID=" + authorID + " or TM.userID=" + authorID + " ) ";
                    #endregion
                }

                if (author == "Member")
                {
                    #region sql
                    sql = @"         SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                                TG.name TaskGroupName,
                                                (select top 1 U2.fullname
                                                 from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                                 where TM.taskID=T.id
                                                ) MemberName
                                        FROM tb_Task T LEFT JOIN tb_User U ON T.userCreateID=U.id
                                                       LEFT JOIN tb_Task_Group TG ON TG.id=T.taskGroupID
                                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id 
                                        WHERE TG.projectID=@id and TM.userID=" + authorID;
                    #endregion
                }

                string[] stringParam = new string[] { "@id" };
                object[] objectParam = new object[] { id };

                #region status filter
                if (status == "WaitingTask")
                    sql += @"                  and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 and T.isFinished = 3 ";
                else if (status == "FailedTask")
                    sql += @"                  and T.isFinished = 2 ";
                else if (status == "AccomplishedTask")
                    sql += @"                  and T.finishPercent=100 and T.isFinished=1 ";
                else if (status == "ProcessingTask")
                    sql += @"                  and DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0 ";
                else if (status == "LateTask")
                    sql += @"                  and T.enddate < GETDATE() and T.finishPercent < 100 and T.isFinished = 0 ";
                #endregion

                #region filter task member
                if ((taskMemberID ?? "").Trim() != "")
                {
                    sql += @"                  and TM.userID=@taskMemberID ";

                    stringParam = new string[] { "@id", "@taskMemberID" };
                    objectParam = new object[] { id, taskMemberID };
                }
                #endregion

                sql += @"
                                        ORDER BY TG.id desc, T.id asc ";

                #endregion
                DataTable list = Connect.GetTable(sql, stringParam, objectParam);

                if (list != null)
                    if (list.Rows.Count > 0)
                        response = new ResponseJson(list, false, "");
            }
            return response;
        }

        [HttpGet]
        public object GetWorkFlowByProjectId(int id, string status, string taskMemberID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                #region sql
                string sql = @"
                        SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                   TG.name TaskGroupName,
                                   (select top 1 U2.fullname
                                    from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                    where TM.taskID=T.id
                                   ) MemberName
                        FROM tb_TASK T LEFT JOIN 
                                              (select WF.id workFlowID, WF.name 
                                               from tb_PROJECT P LEFT JOIN tb_WORK_FLOW WF ON WF.procedureID=P.procedureID
                                               where P.id=@id ) tb_WF 
                                       ON tb_WF.workFlowID=T.workFlowID
                                       LEFT JOIN tb_TASK_GROUP TG ON TG.id=T.taskGroupID
                                       LEFT JOIN tb_User U ON T.userCreateID=U.id
                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                        WHERE TG.projectID=@id ";

                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @"
                        SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                   TG.name TaskGroupName,
                                   (select top 1 U2.fullname
                                    from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                    where TM.taskID=T.id
                                   ) MemberName
                        FROM tb_TASK T LEFT JOIN 
                                              (select WF.id workFlowID, WF.name 
                                               from tb_PROJECT P LEFT JOIN tb_WORK_FLOW WF ON WF.procedureID=P.procedureID
                                               where P.id=@id ) tb_WF 
                                       ON tb_WF.workFlowID=T.workFlowID
                                       LEFT JOIN tb_TASK_GROUP TG ON TG.id=T.taskGroupID
                                       LEFT JOIN tb_User U ON T.userCreateID=U.id
                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                       LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                        WHERE TG.projectID=@id
                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + " ) ";
                    #endregion
                }

                if (author == "Member")
                {
                    #region sql
                    sql = @"
                        SELECT T.* , U.fullname CreatorName , " + StaticClass.sqlGetTaskStatus + @" ,
                                   TG.name TaskGroupName,
                                   (select top 1 U2.fullname
                                    from tb_Task_Member TM LEFT JOIN tb_User U2 ON  U2.id=TM.userID
                                    where TM.taskID=T.id
                                   ) MemberName
                        FROM tb_TASK T LEFT JOIN 
                                              (select WF.id workFlowID, WF.name 
                                               from tb_PROJECT P LEFT JOIN tb_WORK_FLOW WF ON WF.procedureID=P.procedureID
                                               where P.id=@id ) tb_WF 
                                       ON tb_WF.workFlowID=T.workFlowID
                                       LEFT JOIN tb_TASK_GROUP TG ON TG.id=T.taskGroupID
                                       LEFT JOIN tb_User U ON T.userCreateID=U.id
                                       LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                        WHERE TG.projectID=@id
                                and (TM.userID=" + authorID + " ) ";
                    #endregion
                }

                string[] stringParam = new string[] { "@id" };
                object[] objectParam = new object[] { id };

                #region status filter
                if (status == "WaitingTask")
                    sql += @"                  and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 and T.isFinished = 3 ";
                else if (status == "FailedTask")
                    sql += @"                  and T.isFinished = 2 ";
                else if (status == "AccomplishedTask")
                    sql += @"                  and T.finishPercent=100 and T.isFinished=1 ";
                else if (status == "ProcessingTask")
                    sql += @"                  and DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0 ";
                else if (status == "LateTask")
                    sql += @"                  and T.enddate < GETDATE() and T.finishPercent < 100 and T.isFinished = 0 ";
                #endregion

                #region filter task member
                if ((taskMemberID ?? "").Trim() != "")
                {
                    sql += @"                  and TM.userID=@taskMemberID ";

                    stringParam = new string[] { "@id", "@taskMemberID" };
                    objectParam = new object[] { id, taskMemberID };
                }
                #endregion

                sql += @"ORDER BY TG.id desc, T.id asc ";

                #endregion

                DataTable item = Connect.GetTable(sql, stringParam, objectParam);
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object SetWorkFlowById(int id, int workFlowID)
        {
            ResponseJson response = new ResponseJson(null, true, "Đã xảy ra lỗi");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                if (Connect.Exec(@"
                        UPDATE tb_TASK
                          SET
                              workFlowID = @workFlowID
                        WHERE id=@id ",
                                   new string[2] { "@workFlowID", "@id" },
                                   new object[2] { (workFlowID == 0 ? Convert.DBNull : workFlowID), id }))
                    response = new ResponseJson(null, false, "");
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

                int taskGroupID = int.Parse((Connect.getField("tb_Task", "taskGroupID", "id", id) ?? "0").ToString());
                int projectID = int.Parse((Connect.getField("tb_Task_Group", "projectID", "id", taskGroupID) ?? "0").ToString());
                int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", projectID) ?? "0").ToString());

                if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                {
                    string userID = (Connect.getField("tb_Task_Member", "userID", "taskID", id) ?? "").ToString();
                    string taskName = (Connect.getField("tb_Task", "name", "id", id) ?? "").ToString();

                    if (Connect.Exec(@"delete from tb_Task where id=@id", new string[1] { "@id" }, new object[1] { id }))
                    {
                        if (userID != "")
                            TelegramController.SendMessage(int.Parse(userID),
                                  "🔔 Admin vừa xoá tác vụ đã giao cho bạn : <b>" + taskName + "</b>");

                        response = new ResponseJson(null, false, "Đã xóa thành công !");
                    }
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
                        else if (item.taskGroupID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Nhóm công việc !");
                        else if (item.memberID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                        else
                        {
                            string username_output = TokenManagerProvider.TokenManager.ValidateToken(Request.Headers["username"].ToString())[0];
                            string creatorID = (Connect.getField("tb_USER", "id", "username", username_output) ?? "").ToString();

                            int isFinished = 0;
                            if (item.startDate != null)
                                if (DateTime.Parse(item.startDate.ToString()) > DateTime.Now)
                                    isFinished = 3;

                            object newID = Connect.FirstResulfExec(@"
                                    INSERT INTO tb_Task(name, description, taskGroupID, userCreateID, isActive, isFinished, startdate, enddate )
                                    VALUES (@name, @description, @taskGroupID, @userCreateID, 0, " + isFinished + @", @startdate, @enddate ) select SCOPE_IDENTITY()",
                                              new string[6] { "@name", "@description", "@taskGroupID", "@userCreateID", "@startdate", "@enddate" },
                                              new object[6] { item.name.ToString(),
                                                          item.description.ToString(),
                                                          int.Parse(item.taskGroupID.ToString()),
                                                          int.Parse(creatorID) ,
                                                          (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString())),
                                                          (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString())),
                                                             });
                            if (newID != null)
                            {
                                {
                                    string[] memberID = (item.memberID.ToString() + ",").Split(',');
                                    for (int i = 0; i < memberID.Length; i++)
                                    {
                                        if (memberID[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(memberID[i]),
                                                  "🔔 Admin vừa giao bạn một tác vụ : <b>" + item.name.ToString() + "</b>");

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
                        else if (item.taskGroupID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Nhóm công việc !");
                        else if (item.memberID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                        else
                        {
                            int isFinished = int.Parse(((Connect.getField("tb_Task", "ISNULL(isFinished,0)", "id", int.Parse(item.id.ToString()))) ?? "0").ToString());
                            if (item.startDate != null)
                                if (DateTime.Parse(item.startDate.ToString()) > DateTime.Now)
                                    isFinished = 3;

                            if (Connect.Exec(@"UPDATE tb_Task
                                        SET
                                            name = @name
                                          , description = @description
                                          , taskGroupID  = @taskGroupID
                                          , startdate = @startdate
                                          , enddate = @enddate
                                          , isFinished = " + isFinished + @"
                                       WHERE id = @id ",
                                           new string[6] { "@name", "@description", "@taskGroupID", "@startdate", "@enddate", "@id" },
                                           new object[6] { item.name.ToString(),
                                                       item.description.ToString(),
                                                       int.Parse(item.taskGroupID.ToString()),
                                                       (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString())),
                                                       (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString())),
                                                       int.Parse(item.id.ToString()) }))
                            {
                                #region Update task Member
                                string[] memberID = Connect.GetTable(@"select userID from tb_Task_member where taskID=@id ", new string[1] { "@id" }, new object[1] { int.Parse(item.id.ToString()) }).Rows.OfType<DataRow>().Select(k => k[0].ToString()).ToArray();
                                string[] member_delete = FunctionProviders.FindItemNotExist(memberID, (item.memberID.ToString() + ",").Split(','));
                                string[] member_insert = FunctionProviders.FindItemNotExist(item.memberID.ToString().Split(','), memberID);

                                for (int i = 0; i < member_delete.Length; i++)
                                {
                                    if (member_delete[i] != "")
                                    {
                                        TelegramController.SendMessage(int.Parse(member_delete[i]),
                                              "🔔 Admin vừa xoá một tác vụ mà bạn được giao : <b>" + item.name.ToString() + "</b>");

                                        Connect.Exec(@" Delete tb_Task_Member where userID=@userID and taskID=@taskID "
                                                    , new string[2] { "@userID", "@taskID" }
                                                    , new object[2] { member_delete[i], int.Parse(item.id.ToString()) });
                                    }
                                }
                                for (int i = 0; i < member_insert.Length; i++)
                                {
                                    if (member_insert[i] != "")
                                    {
                                        TelegramController.SendMessage(int.Parse(member_insert[i]),
                                              "🔔 Admin vừa giao bạn vào một tác vụ : <b>" + item.name.ToString() + "</b>");

                                        Connect.Exec(@"INSERT INTO tb_TASK_MEMBER(userID,taskID)
                                                       VALUES(@userID, @taskID)"
                                                    , new string[2] { "@userID", "@taskID" }
                                                    , new object[2] { memberID[i], int.Parse(item.id.ToString()) });
                                    }
                                }
                                #endregion
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
