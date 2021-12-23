using System;
using API_Tracy.Models;

namespace API_PhanCongCongViec.Models
{
    public static class StaticClass
    {
        public static ResponseJson InsertUserTeam(ResponseJson response, string userID, string teamID)
        {
            if (teamID.Trim() == "" || userID.Trim() == "")
                response = new ResponseJson(null, true, "Chưa đủ thông tin !");
            else
            {
                //checkExist
                var checkExist = Connect.GetTable(@"SELECT teamID FROM tb_TEAM_USER  
                                                            WHERE teamID=@teamID and userID=@userID",
                        new string[2] { "@teamID", "@userID" },
                        new object[2] { int.Parse(teamID), int.Parse(userID) });

                if (checkExist.Rows.Count > 0)
                    response = new ResponseJson(null, true, "Đã tồn tại thành viên này !");

                else if (Connect.Exec(@"INSERT INTO tb_TEAM_USER(teamID, userID)
                                           VALUES (@teamID, @userID ) ",
                                   new string[2] { "@teamID", "@userID" },
                                   new object[2] { int.Parse(teamID) ,
                                                   int.Parse(userID), })
                    )
                    response = new ResponseJson(null, false, "Đã thêm thành công !");
            }
            return response;
        }
    }
}
