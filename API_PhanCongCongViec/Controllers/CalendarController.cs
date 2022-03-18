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
    public class CalendarController : Controller
    {
        [HttpGet]
        public object getTaskByMonth(int projectID, int month, int year)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                try
                {
                    string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                    int authorID = AuthenFunctionProviders.GetAuthorityID(Request.Headers);
                    string clientRouter = Request.Headers["clientRouter"].ToString().Trim();
                    if (clientRouter.ToLower() == "/cong-viec-cua-toi")
                        author = "Member";
                    //int projectManagerID = int.Parse((Connect.getField("tb_Project_Manager", "userID", "userID=" + authorID + " AND projectID", projectID) ?? "0").ToString());

                    //if (author == "Administrator" || (author == "ProjectManager" && projectManagerID == authorID))
                    {
                        if (projectID > 0 && month > 0 && year > 0)
                        {
                            DateTime startOfTheMonth = new DateTime(year, month, 1);
                            DateTime endOfTheMonth = startOfTheMonth.AddMonths(1).AddDays(-1);

                            #region sql
                            string sql = @"         SELECT DATEPART(WEEKDAY, T.endDate) 'dayOfWeek' , T.endDate date, T.id, T.name
                                                                , " + StaticClass.sqlGetTaskStatus + @"
                                                    FROM tb_Task_Group TG LEFT JOIN tb_Task T ON TG.id=T.taskGroupID
                                                    WHERE TG.projectID=@id
                                                          and endDate >= @startDate and endDate <= @endDate
                                                    ORDER BY T.endDate ";

                            if (author == "ProjectManager")
                            {
                                #region sql
                                sql = @"            SELECT DATEPART(WEEKDAY, T.endDate) 'dayOfWeek' , T.endDate date, T.id, T.name
                                                                , " + StaticClass.sqlGetTaskStatus + @"
                                                    FROM tb_Task_Group TG LEFT JOIN tb_Task T ON TG.id=T.taskGroupID
                                                                          LEFT JOIN tb_Project_Manager PM ON PM.projectID=TG.projectID
                                                                          LEFT JOIN tb_TASK_Member TM ON TM.taskID=T.id
                                                    WHERE TG.projectID=@id
                                                          and endDate >= @startDate and endDate <= @endDate
                                                          and ( PM.userID=" + authorID + @" OR TM.userID=" + authorID + @" )
                                                    ORDER BY T.endDate ";
                                #endregion
                            }

                            if (author == "Member")
                            {
                                #region sql
                                sql = @"            SELECT DATEPART(WEEKDAY, T.endDate) 'dayOfWeek' , T.endDate date, T.id, T.name
                                                                , " + StaticClass.sqlGetTaskStatus + @"
                                                    FROM tb_Task_Group TG LEFT JOIN tb_Task T ON TG.id=T.taskGroupID
                                                                          LEFT JOIN tb_TASK_Member TM ON TM.taskID=T.id
                                                    WHERE TG.projectID=@id
                                                          and endDate >= @startDate and endDate <= @endDate
                                                          and TM.userID=" + authorID + @"
                                                    ORDER BY T.endDate ";
                                #endregion
                            }
                            #endregion

                            DataTable list = Connect.GetTable(sql, new string[3] { "@id", "@startDate", "@endDate" },
                                                                   new object[3] { projectID, startOfTheMonth.ToString("MM/dd/yyyy 00:00:00"), endOfTheMonth.ToString("MM/dd/yyyy 23:59:59") });

                            if (list != null)
                            {
                                if (list.Rows.Count > 0)
                                {
                                    if (list.Rows[0]["date"].ToString() != startOfTheMonth.ToString("MM/dd/yyyy"))
                                    {
                                        DataRow newRow = list.NewRow();
                                        newRow["dayOfWeek"] = ((int)startOfTheMonth.DayOfWeek) + 1;
                                        newRow["date"] = startOfTheMonth.ToString("MM/dd/yyyy");
                                        newRow["id"] = "-1";
                                        newRow["name"] = "";
                                        newRow["status"] = "";
                                        list.Rows.InsertAt(newRow, 0);
                                    }

                                    if (list.Rows[list.Rows.Count - 1]["date"].ToString() != endOfTheMonth.ToString("MM/dd/yyyy"))
                                    {
                                        DataRow newRow = list.NewRow();
                                        newRow["dayOfWeek"] = ((int)endOfTheMonth.DayOfWeek) + 1;
                                        newRow["date"] = endOfTheMonth.ToString("MM/dd/yyyy");
                                        newRow["id"] = "-1";
                                        newRow["name"] = "";
                                        newRow["status"] = "";
                                        list.Rows.Add(newRow);
                                    }
                                }
                                else
                                {
                                    list = new DataTable();
                                    list.Columns.Add("dayOfWeek", typeof(Int32));
                                    list.Columns.Add("date", typeof(DateTime));
                                    list.Columns.Add("id", typeof(Int32));
                                    list.Columns.Add("name");
                                    list.Columns.Add("status");

                                    DataRow newRow = list.NewRow();
                                    newRow["dayOfWeek"] = ((int)startOfTheMonth.DayOfWeek) + 1;
                                    newRow["date"] = startOfTheMonth.ToString("MM/dd/yyyy");
                                    newRow["id"] = "-1";
                                    newRow["name"] = "";
                                    newRow["status"] = "";
                                    list.Rows.InsertAt(newRow, 0);

                                    newRow = list.NewRow();
                                    newRow["dayOfWeek"] = ((int)endOfTheMonth.DayOfWeek) + 1;
                                    newRow["date"] = endOfTheMonth.ToString("MM/dd/yyyy");
                                    newRow["id"] = "-1";
                                    newRow["name"] = "";
                                    newRow["status"] = "";
                                    list.Rows.Add(newRow);
                                }

                                response = new ResponseJson(list, false, "");
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
    }
}
