using System;
using System.Collections.Generic;
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
                DataTable item = Connect.GetTable(@"SELECT P.*, DE.name departmentName
                                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                                    WHERE P.id=@id", new string[1] { "@id" }, new object[1] { id });
                if (item != null)
                    if (item.Rows.Count > 0)
                        response = new ResponseJson(item, false, "");
            }
            return response;
        }

        [HttpGet]
        public object getList()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"SELECT P.*, DE.name departmentName
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
                pageNum -= 1;
                if (pageNum <= 0) pageNum = 0;
                int pageStart = pageNum * pageSize;

                DataTable list = Connect.GetTable(@"
                                    SELECT P.*, DE.name departmentName ,
                                            (select count(userID) from tb_PROJECT_MEMBER where projectID=P.id)
                                            +
                                            (select 0 from tb_PROJECT_MANAGER where projectID=P.id) memberAmount
                                    FROM tb_Project P LEFT JOIN tb_Department DE ON P.departmentID=DE.id
                                    ORDER BY ISNULL(P.isPriority,0) desc, P.id desc
                                    OFFSET " + pageStart + @" ROWS
                                    FETCH NEXT " + pageSize + @" ROWS ONLY;");
                if (list != null)
                    response = new ResponseJson(list, false, "");
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
                                              UNION
                                                    SELECT PM.userID, U.fullname, '1' tableStatus
                                                    FROM tb_Project_MEMBER PM LEFT JOIN tb_User U ON U.id = PM.userID
                                                    WHERE PM.projectID=@id", new string[1] { "@id" }, new object[1] { id });
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
                if (Connect.Exec(@"delete from tb_PROJECT where id=@id", new string[1] { "@id" }, new object[1] { id }))
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
                        response = new ResponseJson(null, true, "Chưa nhập Tên dự án !");
                    else if (item.departmentID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Phòng ban !");
                    else if (item.managerID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Người quản lý !");
                    else if (item.memberID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Người thực hiện !");
                    else
                    {
                        object newID = Connect.FirstResulfExec(@"
                                           INSERT INTO tb_PROJECT(name, startdate, enddate, isPriority, departmentID, description)
                                           VALUES (@name, @startdate, @enddate, @isPriority, @departmentID, @description ) select SCOPE_IDENTITY() ",

                                           new string[6] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID", "@description" },
                                           new object[6] { item.name.ToString(),
                                                           (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString()) ),
                                                           (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString()) ),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()),
                                                           item.description.ToString() });
                        if (newID != null)
                        {
                            {
                                string[] managerID = (item.managerID.ToString() + ",").Split(',');
                                for (int i = 0; i < managerID.Length; i++)
                                {
                                    if (managerID[i] != "")
                                    {
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
                    else if (item.departmentID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Phòng ban !");
                    else if (item.managerID.ToString().Trim() == "")
                        response = new ResponseJson(null, true, "Chưa chọn Người quản lý !");
                    else
                    {
                        if (Connect.Exec(@"UPDATE tb_PROJECT
                                        SET
                                            name = @name
                                           ,startdate = @startdate
                                           ,enddate = @enddate
                                           ,isPriority = @isPriority
                                           ,departmentID = @departmentID
                                       WHERE id = @id ",
                                           new string[6] { "@name", "@startdate", "@enddate", "@isPriority", "@departmentID", "@id" },
                                           new object[6] { item.name.ToString(),
                                                           (item.startDate == null? Convert.DBNull : DateTime.Parse(item.startDate.ToString()) ),
                                                           (item.endDate == null? Convert.DBNull : DateTime.Parse(item.endDate.ToString()) ),
                                                           bool.Parse(item.isPriority.ToString()),
                                                           int.Parse(item.departmentID.ToString()),
                                                           int.Parse(item.id.ToString()) })
                            )
                        {
                            #region Update Project Member
                            string[] memberID = Connect.GetTable(@"select userID from tb_Project_Member where projectID=@id ", new string[1] { "@id" }, new object[1] { int.Parse(item.id.ToString()) }).Rows.OfType<DataRow>().Select(k => k[0].ToString()).ToArray();
                            string[] member_delete = FindItemNotExist(memberID, item.memberID.ToString().Split(','));
                            string[] member_insert = FindItemNotExist(item.memberID.ToString().Split(','), memberID);

                            for (int i = 0; i < member_insert.Length; i++)
                            {
                                if (member_insert[i] != "")
                                {
                                    Connect.Exec(@"INSERT INTO tb_PROJECT_MEMBER(userID,projectID)
                                                       VALUES(@userID, @projectID)"
                                                , new string[2] { "@userID", "@projectID" }
                                                , new object[2] { member_insert[i], int.Parse(item.id.ToString()) });
                                }
                            }
                            for (int i = 0; i < member_delete.Length; i++)
                            {
                                if (member_delete[i] != "")
                                {
                                    Connect.Exec(@" Delete tb_Project_Member where userID=@userID and projectID=@projectID "
                                                , new string[2] { "@userID", "@projectID" }
                                                , new object[2] { member_delete[i], int.Parse(item.id.ToString()) });
                                }
                            }
                            #endregion

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

        string[] FindItemNotExist(string[] arrA, string[] arrB)
        {
            List<string> result = new List<string>();
            foreach (var item in arrA)
            {
                if (!arrB.Contains(item))
                    result.Add(item);
            }
            return result.ToArray();
        }
    }
}
