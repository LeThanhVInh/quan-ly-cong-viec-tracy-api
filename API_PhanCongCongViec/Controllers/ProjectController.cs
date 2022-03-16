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
    [ApiController]
    public class ProjectController : Controller
    {
        public object GetById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                //string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                //int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                //int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", id) ?? "0").ToString());

                //if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                {
                    DataTable item = Connect.GetTable(@"SELECT P.*, DE.name departmentName
                                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                                    WHERE P.id=@id", new string[1] { "@id" }, new object[1] { id });
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
                DataTable list = Connect.GetTable(@"
                                        SELECT P.*, DE.name departmentName 
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                    ORDER BY ISNULL(P.isPriority,0) desc");
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
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                pageNum -= 1;
                if (pageNum <= 0) pageNum = 0;
                int pageStart = pageNum * pageSize;

                #region sql
                string sql = @"SELECT * from
                               (
                                    SELECT P.*, DE.name departmentName ,
                                        (select count(*) from
                                            (       SELECT PM.userID
                                                    FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id
                                              UNION ALL
                                                    SELECT PM.userID
                                                    FROM tb_Project_MEMBER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id and PM.userID NOT IN 
                                                            ( 
                                                                SELECT PM.userID
                                                                FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                                WHERE PM.projectID=P.id
                                                            )
                                             ) as tb1
                                        ) memberAmount,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id  
                                         where TG.projectID=P.id  
                                        ) TotalTask ,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID= P.id
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where
                                               (
                                                   (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and T.isFinished = 0
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where TG.projectID= P.id
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID= P.id
                                               and T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID= P.id
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                               ) AS tb1
                               GROUP BY tb1.id, tb1.name, tb1.startdate, tb1.enddate, tb1.departmentID, tb1.isPriority, tb1.[description], tb1.procedureID, tb1.departmentName, tb1.memberAmount, 
                                        tb1.TotalTask, tb1.LateTask, tb1.ProcessingTask, tb1.AccomplishedTask, tb1.FailedTask, tb1.WaitingTask
                               ORDER BY ISNULL(tb1.isPriority,0) desc, tb1.id desc
                               OFFSET " + pageStart + @" ROWS
                               FETCH NEXT " + pageSize + @" ROWS ONLY; ";

                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @"   SELECT * from
                               (
                                    SELECT P.*, DE.name departmentName ,
                                        (select count(*) from
                                            (       SELECT PM.userID
                                                    FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id
                                              UNION ALL
                                                    SELECT PM.userID
                                                    FROM tb_Project_MEMBER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id and PM.userID NOT IN 
                                                            ( 
                                                                SELECT PM.userID
                                                                FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                                WHERE PM.projectID=P.id
                                                            )
                                             ) as tb1
                                        ) memberAmount,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id  
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id  
                                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) TotalTask ,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.isFinished = 2
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                                      LEFT JOIN tb_Project_Manager PM ON PM.projectID=P.id
                                                      LEFT JOIN tb_Project_Member PMm ON PMm.projectID=P.id
                                    WHERE PM.userID=" + authorID + " OR PMm.userID=" + authorID + @"
                               ) AS tb1
                               GROUP BY tb1.id, tb1.name, tb1.startdate, tb1.enddate, tb1.departmentID, tb1.isPriority, tb1.[description], tb1.procedureID, tb1.departmentName, tb1.memberAmount, 
                                        tb1.TotalTask, tb1.LateTask, tb1.ProcessingTask, tb1.AccomplishedTask, tb1.FailedTask, tb1.WaitingTask
                               ORDER BY ISNULL(tb1.isPriority,0) desc, tb1.id desc
                               OFFSET " + pageStart + @" ROWS
                               FETCH NEXT " + pageSize + @" ROWS ONLY; ";
                    #endregion
                }
                else if (author == "Member")
                {
                    #region sql
                    sql = @"SELECT * from
                               (
                                    SELECT P.*, DE.name departmentName ,
                                        (select count(*) from
                                            (       SELECT PM.userID
                                                    FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id
                                              UNION ALL
                                                    SELECT PM.userID
                                                    FROM tb_Project_MEMBER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=P.id and PM.userID NOT IN 
                                                            ( 
                                                                SELECT PM.userID
                                                                FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                                WHERE PM.projectID=P.id
                                                            )
                                             ) as tb1
                                        ) memberAmount,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id 
                                         where TG.projectID=P.id
                                                and TM.userID=" + authorID + @"
                                        ) TotalTask ,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and TM.userID=" + authorID + @"
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and TM.userID=" + authorID + @"
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and TM.userID=" + authorID + @"
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and T.isFinished = 2
                                               and TM.userID=" + authorID + @"
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID= P.id
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and TM.userID=" + authorID + @"
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                                      LEFT JOIN tb_Project_Member PMm ON PMm.projectID=P.id
                                    WHERE PMm.userID=" + authorID + @"
                               ) AS tb1
                               GROUP BY tb1.id, tb1.name, tb1.startdate, tb1.enddate, tb1.departmentID, tb1.isPriority, tb1.[description], tb1.procedureID, tb1.departmentName, tb1.memberAmount, 
                                        tb1.TotalTask, tb1.LateTask, tb1.ProcessingTask, tb1.AccomplishedTask, tb1.FailedTask, tb1.WaitingTask
                               ORDER BY ISNULL(tb1.isPriority,0) desc, tb1.id desc
                               OFFSET " + pageStart + @" ROWS
                               FETCH NEXT " + pageSize + @" ROWS ONLY; ";
                    #endregion
                }
                #endregion
                DataTable list = Connect.GetTable(sql);
                if (list != null)
                    response = new ResponseJson(list, false, "");

            }

            return response;
        }

        [HttpGet]
        public object getMemberStatisticById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                #region sql
                string sql = @" SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (
                                        SELECT U.fullname,U.id userID, TG.projectID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                       LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                       LEFT JOIN tb_USER U ON U.id=TM.userID
                                        WHERE TG.projectID=@id
                                        GROUP BY U.fullname, U.id, TG.projectID
                                    ) as tb_Member";

                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @" SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (
                                        SELECT U.fullname,U.id userID, TG.projectID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                       LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                       LEFT JOIN tb_USER U ON U.id=TM.userID
                                                       LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                        WHERE TG.projectID=@id
                                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        GROUP BY U.fullname, U.id, TG.projectID
                                    ) as tb_Member";
                    #endregion
                }

                if (author == "Member")
                {
                    #region sql
                    sql = @" SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID
                                               and (TM.userID=" + authorID + @")
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID 
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and (TM.userID=" + authorID + @")
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID 
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and (TM.userID=" + authorID + @")
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID 
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and (TM.userID=" + authorID + @")
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID 
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                               and (TM.userID=" + authorID + @")
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID 
                                         where TG.projectID=tb_Member.projectID 
                                               and TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and (TM.userID=" + authorID + @")
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (
                                        SELECT U.fullname,U.id userID, TG.projectID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                       LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                       LEFT JOIN tb_USER U ON U.id=TM.userID 
                                        WHERE TG.projectID=@id
                                               and (TM.userID=" + authorID + @")
                                        GROUP BY U.fullname, U.id, TG.projectID
                                    ) as tb_Member";
                    #endregion
                }
                #endregion
                DataTable item = Connect.GetTable(sql, new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }
        [HttpGet]
        public object getMemberStatisticByAllProject()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                //if (author == "Administrator" || author == "ProjectManager")
                {
                    #region sql
                    string sql = @"
                                SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (   SELECT U.fullname,U.id userID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                    LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                    LEFT JOIN tb_USER U ON U.id=TM.userID
                                        GROUP BY U.fullname, U.id
                                    ) as tb_Member ";

                    if (author == "Member")
                    {
                        #region sql
                        sql = @"
                                SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                                           LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID
                                                and PM.userID = " + authorID + @"
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and PM.userID = " + authorID + @"
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and PM.userID = " + authorID + @"
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and PM.userID = " + authorID + @"
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                               and PM.userID = " + authorID + @"
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MEMBER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and PM.userID = " + authorID + @"
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (          SELECT U.fullname,U.id userID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                    LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                    LEFT JOIN tb_USER U ON U.id=TM.userID 
                                        WHERE  TM.userID = " + authorID + @"
                                        GROUP BY U.fullname, U.id
                                    ) as tb_Member  ";
                        #endregion
                    }

                    if (author == "ProjectManager")
                    {
                        #region sql
                        sql = @"
                                SELECT tb_Member.* ,
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.enddate < GETDATE() 
                                               and T.finishPercent < 100
                                               and T.isFinished = 0
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.finishPercent=100 
                                               and T.isFinished=1 
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and T.isFinished = 2
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                         where  TM.userID=tb_Member.userID 
                                               and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                               and T.isFinished = 3
                                               and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                FROM 
                                    (          SELECT U.fullname,U.id userID
                                        FROM tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                    LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                                    LEFT JOIN tb_USER U ON U.id=TM.userID
                                                    LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                        WHERE  (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        GROUP BY U.fullname, U.id
                                    ) as tb_Member  ";
                        #endregion
                    }
                    #endregion

                    DataTable item = Connect.GetTable(sql);
                    if (item != null)
                        if (item.Rows.Count > 0)
                            response = new ResponseJson(item, false, "");
                }
            }
            return response;
        }

        [HttpGet]
        public object GetStatisticById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);

                #region sql
                string sql = @"     SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where TG.projectID=P.id 
                                            and
                                            (
                                                 (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                              OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P
                                    WHERE P.id=@id ";

                if (author == "ProjectManager")
                {
                    #region sql
                    sql = @"     SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id
                                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and
                                               (
                                                    (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                                 OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                               )
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @")
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P
                                    WHERE P.id=@id ";
                    #endregion
                }
                if (author == "Member")
                {
                    #region sql
                    sql = @"     SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id
                                            and (TM.userID=" + authorID + @")
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                            and (TM.userID=" + authorID + @")
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id  
                                            and
                                            (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                            and (TM.userID=" + authorID + @")
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                            and (TM.userID=" + authorID + @")
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                            and (TM.userID=" + authorID + @")
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_Task_Member TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and (TM.userID=" + authorID + @")
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P
                                    WHERE P.id=@id ";
                    #endregion
                }

                #endregion
                DataTable list = Connect.GetTable(sql, new string[1] { "@id" }, new object[1] { id });
                if (list != null)
                    if (list.Rows.Count > 0)
                        response = new ResponseJson(list, false, "");
            }
            return response;
        }
        [HttpGet]
        public object GetStatisticByAllProject()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                //if (author == "Administrator" || author == "ProjectManager")
                {
                    #region sql
                    string sql = @" SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where  T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where
                                            (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where  T.finishPercent=100 
                                            and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where  T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where  DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                     ";

                    if (author == "ProjectManager")
                        sql = @" SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where  (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where  T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0 
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where
                                            (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where  T.finishPercent=100 
                                            and T.isFinished=1  
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where  T.isFinished = 2
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON TG.projectID=PM.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where  DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------ ";

                    if (author == "Member")
                        sql = @" SELECT
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  TM.userID = " + authorID + @"
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                            and TM.userID = " + authorID + @"
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where
                                            (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                            and TM.userID = " + authorID + @"
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  T.finishPercent=100 
                                            and T.isFinished=1 
                                            and TM.userID = " + authorID + @"
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  T.isFinished = 2
                                            and TM.userID = " + authorID + @"
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where  DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and TM.userID = " + authorID + @"
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------ ";
                    #endregion

                    DataTable list = Connect.GetTable(sql);
                    if (list != null)
                        if (list.Rows.Count > 0)
                            response = new ResponseJson(list, false, "");
                }
            }
            return response;
        }
        [HttpGet]
        public object GetTaskStatisticByAllProject()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                //if (author == "Administrator" || author == "ProjectManager")
                {
                    string sql = @" SELECT P.name ProjectName ,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where TG.projectID=P.id 
                                            and
                                            (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P
                                    ORDER BY ISNULL(P.isPriority,0) desc , P.id desc ";


                    if (author == "ProjectManager")
                    {
                        #region sql
                        sql = @"SELECT * from
                               (
                                    SELECT P.id ProjectID, P.name ProjectName , P.isPriority ,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                                           LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                                and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and (
                                                (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 0) 
                                             OR (DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0 and T.isFinished = 3) 
                                            )
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_PROJECT_MANAGER PM ON PM.projectID=TG.projectID
                                                        LEFT JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and (PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P LEFT JOIN tb_Project_Manager PM ON PM.projectID=P.id
                                                      LEFT JOIN tb_Project_Member PMm ON PMm.projectID=P.id
                                    WHERE  PM.userID=" + authorID + " OR PMm.userID=" + authorID + @"
                               ) AS tb1
                               GROUP BY tb1.ProjectName , tb1.ProjectID, tb1.isPriority ,
                                        tb1.TotalTask, tb1.LateTask, tb1.ProcessingTask, tb1.AccomplishedTask, tb1.FailedTask, tb1.WaitingTask
                               ORDER BY ISNULL(tb1.isPriority,0) desc , tb1.ProjectID desc  ";
                        #endregion
                    }
                    else if (author == "Member")
                    {
                        #region sql
                        sql = @"    SELECT P.name ProjectName ,
                                        ------------------------------------------------------------------------------------
                                        (select count(T.id) from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                                           LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id
                                                and TM.userID=" + authorID + @"
                                        ) TotalTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id and T.enddate < GETDATE() 
                                            and T.finishPercent < 100
                                            and T.isFinished = 0
                                            and TM.userID=" + authorID + @"
                                        ) LateTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, GETDATE(), T.enddate ) > 0
                                            and T.isFinished = 0
                                            and TM.userID=" + authorID + @"
                                        ) ProcessingTask,
                                        ------------------------------------------------------------------------------------
                                        (select ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id 
                                            and T.finishPercent=100 
                                            and T.isFinished=1 
                                            and TM.userID=" + authorID + @"
                                        ) AccomplishedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id 
                                            and T.isFinished = 2
                                            and TM.userID=" + authorID + @"
                                        ) FailedTask,
                                        ------------------------------------------------------------------------------------
                                        (select  ISNULL( CAST(count(T.id) as nvarchar) +'_'+  CAST( sum(ISNULL(T.finishPercent,0)) as nvarchar) , '0_0')
                                         from tb_TASK T LEFT JOIN tb_TASK_GROUP TG ON T.taskGroupID=TG.id 
                                                        LEFT JOIN tb_TASK_MEMBER TM ON T.id=TM.taskID
                                         where TG.projectID=P.id 
                                            and DATEDIFF(MINUTE, T.startdate ,GETDATE()) < 0 
                                            and T.isFinished = 3
                                            and TM.userID=" + authorID + @"
                                        ) WaitingTask
                                        ------------------------------------------------------------------------------------
                                    FROM tb_PROJECT P
                                                      LEFT JOIN tb_Project_Member PMm ON PMm.projectID=P.id
                                    WHERE PMm.userID=" + authorID + @"
                                    ORDER BY ISNULL(P.isPriority,0) desc , P.id desc";
                        #endregion
                    }

                    DataTable list = Connect.GetTable(sql);
                    if (list != null)
                        if (list.Rows.Count > 0)
                            response = new ResponseJson(list, false, "");
                }
            }
            return response;
        }

        [HttpGet]
        public object GetMemberById(int id)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"SELECT PM.userID, U.fullname, '0' tableStatus
                                                    FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=@id
                                              UNION ALL
                                                    SELECT PM.userID, U.fullname, '1' tableStatus
                                                    FROM tb_Project_MEMBER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=@id and PM.userID NOT IN 
                                                            ( 
                                                                SELECT PM.userID
                                                                FROM tb_Project_MANAGER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                                WHERE PM.projectID=@id
                                                            )"
                                            , new string[1] { "@id" }, new object[1] { id });
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
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    if (Connect.Exec(@"delete from tb_PROJECT where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                            response = new ResponseJson(null, true, "Chưa nhập Tên dự án !");
                        else if (item.departmentID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Phòng ban !");
                        else if (item.managerID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Người quản lý !");
                        else if (item.memberID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                        else if (item.isProcedure.ToString().Trim() == "True" && item.procedureID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Quy trình !");
                        else
                        {
                            object newID = Connect.FirstResulfExec(@"
                                           INSERT INTO tb_PROJECT(name, startdate, enddate, isPriority, departmentID, description,procedureID )
                                           VALUES (@name, @startdate, @enddate, @isPriority, @departmentID, @description, @procedureID ) select SCOPE_IDENTITY() ",

                                               new string[7] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID", "@description", "@procedureID" },
                                               new object[7] { item.name.ToString(),
                                                           (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString()) ),
                                                           (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString()) ),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()),
                                                           item.description.ToString(),
                                                           (item.procedureID.ToString() == "" ? Convert.DBNull : int.Parse(item.procedureID.ToString()))
                                               });
                            if (newID != null)
                            {
                                {
                                    string[] managerID = (item.managerID.ToString() + ",").Split(',');
                                    for (int i = 0; i < managerID.Length; i++)
                                    {
                                        if (managerID[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(managerID[i]),
                                                  "🔔 Admin vừa thêm bạn vào quản lý cho dự án: <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@"INSERT INTO tb_PROJECT_MANAGER(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { managerID[i], newID });
                                        }
                                    }
                                    ////////////////////////////////////////////////////////////////
                                    string[] teamID = (item.teamID.ToString() + ",").Split(',');
                                    for (int i = 0; i < teamID.Length; i++)
                                    {
                                        if (teamID[i] != "")
                                        {
                                            Connect.Exec(@"INSERT INTO tb_PROJECT_TEAM(teamID,projectID)
                                                       VALUES(@teamID, @projectID)"
                                                        , new string[2] { "@teamID", "@projectID" }
                                                        , new object[2] { teamID[i], newID });
                                        }
                                    }
                                    ////////////////////////////////////////////////////////////////
                                    string[] memberID = (item.memberID.ToString() + ",").Split(',');
                                    for (int i = 0; i < memberID.Length; i++)
                                    {
                                        if (memberID[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(memberID[i]),
                                                  "🔔 Admin vừa thêm bạn tham gia vào dự án: <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@"INSERT INTO tb_PROJECT_MEMBER(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { memberID[i], newID });
                                        }
                                    }
                                }
                                {
                                    Connect.Exec(@"INSERT INTO tb_Task_Group(projectID,name)
                                               VALUES(@id, N'Nhóm chưa đặt tên') ", new string[] { "@id" }, new object[] { newID });
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
                    int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", int.Parse(item.id.ToString())) ?? "0").ToString());

                    if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                    {
                        if (item.name.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa nhập Tên !");
                        else if (item.departmentID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Phòng ban !");
                        else if (item.managerID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Người quản lý !");
                        else if (item.isProcedure.ToString().Trim() == "True" && item.procedureID.ToString().Trim() == "")
                            response = new ResponseJson(null, true, "Chưa chọn Quy trình !");
                        else
                        {
                            bool existWorkFlow = false;
                            #region check workFlow
                            string procedureID_OLD = (Connect.getField("tb_Project", "procedureID", "id", int.Parse(item.id.ToString())) ?? "").ToString();
                            if (procedureID_OLD != item.procedureID.ToString().Trim())
                            {
                                DataTable tbWorkFlow = Connect.GetTable(@"SELECT T.*
                                                                  FROM tb_Task T LEFT JOIN tb_Task_Group TG ON TG.id=T.taskGroupID
                                                                  WHERE TG.projectID = @projectID
                                                                        and T.workFlowID IN (
                                                                                             select id from tb_Work_Flow
                                                                                             where procedureID=@procedureID ) "
                                                                            , new string[2] { "@projectID", "@procedureID" }
                                                                            , new object[2] { int.Parse(item.id.ToString()), procedureID_OLD });
                                if (tbWorkFlow.Rows.Count > 0)
                                    existWorkFlow = true;
                            }
                            #endregion

                            if (existWorkFlow)
                                response = new ResponseJson(null, true, "Không thể đổi quy trình vì có tác vụ đã được phân loại theo quy trình cũ !");
                            else
                            {
                                if (Connect.Exec(@"UPDATE tb_PROJECT
                                        SET
                                            name = @name
                                           ,startdate = @startdate
                                           ,enddate = @enddate
                                           ,isPriority = @isPriority
                                           ,departmentID = @departmentID
                                           ,procedureID = @procedureID
                                       WHERE id = @id ",
                                                   new string[7] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID", "@procedureID", "@id" },
                                                   new object[7] { item.name.ToString(),
                                                           (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString()) ),
                                                           (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString()) ),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()),
                                                           (item.procedureID.ToString() == "" ? Convert.DBNull : int.Parse(item.procedureID.ToString())),
                                                           int.Parse(item.id.ToString()) })
                                    )
                                {
                                    #region Update Project Member
                                    string[] memberID = Connect.GetTable(@"select userID from tb_Project_Member where projectID=@id ", new string[1] { "@id" }, new object[1] { int.Parse(item.id.ToString()) }).Rows.OfType<DataRow>().Select(k => k[0].ToString()).ToArray();
                                    string[] member_delete = FunctionProviders.FindItemNotExist(memberID, item.memberID.ToString().Split(','));
                                    string[] member_insert = FunctionProviders.FindItemNotExist(item.memberID.ToString().Split(','), memberID);

                                    for (int i = 0; i < member_delete.Length; i++)
                                    {
                                        if (member_delete[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(member_delete[i]),
                                                  "🔔 Admin vừa xoá bạn ra khỏi dự án: <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@" Delete tb_Project_Member where userID=@userID and projectID=@projectID "
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { member_delete[i], int.Parse(item.id.ToString()) });
                                        }
                                    }
                                    for (int i = 0; i < member_insert.Length; i++)
                                    {
                                        if (member_insert[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(member_insert[i]),
                                                  "🔔 Admin vừa thêm bạn vào dự án: <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@"INSERT INTO tb_PROJECT_MEMBER(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { member_insert[i], int.Parse(item.id.ToString()) });
                                        }
                                    }
                                    #endregion

                                    #region Update Project Manager
                                    string[] managerID = Connect.GetTable(@"select userID from tb_Project_manager where projectID=@id ", new string[1] { "@id" }, new object[1] { int.Parse(item.id.ToString()) }).Rows.OfType<DataRow>().Select(k => k[0].ToString()).ToArray();
                                    string[] manager_delete = FunctionProviders.FindItemNotExist(managerID, item.managerID.ToString().Split(','));
                                    string[] manager_insert = FunctionProviders.FindItemNotExist(item.managerID.ToString().Split(','), managerID);

                                    for (int i = 0; i < manager_delete.Length; i++)
                                    {
                                        if (manager_delete[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(manager_delete[i]),
                                                  "🔔 Admin vừa xoá bạn ra khỏi dự án bạn quản lý : <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@" Delete tb_Project_manager where userID=@userID and projectID=@projectID "
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { manager_delete[i], int.Parse(item.id.ToString()) });
                                        }
                                    }
                                    for (int i = 0; i < manager_insert.Length; i++)
                                    {
                                        if (manager_insert[i] != "")
                                        {
                                            TelegramController.SendMessage(int.Parse(manager_insert[i]),
                                                  "🔔 Admin vừa thêm bạn vào quản lý cho dự án: <b>" + item.name.ToString() + "</b>");

                                            Connect.Exec(@"INSERT INTO tb_PROJECT_manager(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                        , new string[2] { "@userID", "@projectID" }
                                                        , new object[2] { manager_insert[i], int.Parse(item.id.ToString()) });
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


        [HttpGet]
        public object CheckMemberHaveTask(int userID, int projectID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable item = Connect.GetTable(@"
                                        select TM.userID
                                        from tb_PROJECT_MEMBER PM INNER JOIN tb_TASK_GROUP TG ON PM.projectID=TG.projectID
                                                                  INNER JOIN tb_TASK T ON TG.id=T.taskGroupID
                                                                  INNER JOIN tb_TASK_MEMBER TM ON TM.taskID=T.id
                                        where TM.userID =@userID and PM.projectID=@projectID
                                        group by TM.userID", new string[2] { "@userID", "@projectID" }, new object[2] { userID, projectID });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "Không thể bỏ thành viên này, vì đã được giao công việc");
            }
            return response;
        }
    }
}
