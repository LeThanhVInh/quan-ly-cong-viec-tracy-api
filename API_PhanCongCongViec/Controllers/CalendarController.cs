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
                    if (projectID > 0 && month > 0 && year > 0)
                    {
                        DateTime startOfTheMonth = new DateTime(year, month, 1);
                        DateTime endOfTheMonth = startOfTheMonth.AddMonths(1).AddDays(-1);

                        DataTable list = Connect.GetTable(@"SELECT DATEPART(WEEKDAY, T.endDate) 'dayOfWeek' , T.endDate date, T.id, T.name
                                                                , " + StaticClass.sqlGetTaskStatus + @"
                                                    FROM tb_Task_Group TG LEFT JOIN tb_Task T ON TG.id=T.taskGroupID
                                                    WHERE TG.projectID=@id
                                                          and endDate >= @startDate and endDate <= @endDate
                                                    ORDER BY T.endDate ",
                                                              new string[3] { "@id", "@startDate", "@endDate" },
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
