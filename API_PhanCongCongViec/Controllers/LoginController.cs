﻿using System;
using API_Tracy.Models;
using Microsoft.AspNetCore.Mvc;
using TokenManagerProvider;

namespace API_PhanCongCongViec.Controllers
{
    [Route("authen/[action]")]
    [ApiController]
    public class LoginController : Controller
    {
        [HttpPost]
        public object Login([FromBody] dynamic login)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu !");
            if (login.username == "")
                response = new ResponseJson(null, true, "Chưa nhập tên đăng nhập !");
            else if (login.password == "")
                response = new ResponseJson(null, true, "Chưa nhập mật khẩu !");
            else
            {
                object Pass = Connect.getField("tb_User", "Password", "username", login.username.ToString());

                if (Pass != null)
                {
                    if (Pass.ToString() == login.password.ToString())
                    {
                        var token = TokenManager.GenerateToken(login.username.ToString());
                        var username = TokenManager.GenerateToken(login.username.ToString());

                        response = new ResponseJson(new string[] { token, username }, false, "Đăng nhập thành công");
                    }
                    else
                        response = new ResponseJson(null, true, "Sai mật khẩu !");
                }
            }

            return response;
        }

        [HttpGet]
        public object Validate(string token, string username)
        {
            ResponseJson response = null;
            try
            {
                string username_output = TokenManager.ValidateToken(username)[0];
                object user = Connect.getField("tb_User", "username", "username", username_output);
                if (user == null)
                    response = new ResponseJson(null, true, "Không có dữ liệu !");
                else
                {
                    string[] token_output = TokenManager.ValidateToken(token);
                    if (username_output.Equals(token_output[0]) && DateTime.Now < DateTime.Parse(token_output[1]))
                    {
                        response = new ResponseJson(null, false, "Thành công !");
                    }
                    else
                        response = new ResponseJson(null, true, "Đã hết phiên đăng nhập");
                }
            }
            catch { }
            return response;
        }

    }
}
