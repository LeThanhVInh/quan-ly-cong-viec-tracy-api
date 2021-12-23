using System.Data;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Mvc;

namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class UserTypeController : Controller
    {
        [HttpGet]
        public object List()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                DataTable list = Connect.GetTable(@"SELECT * FROM tb_User_type ");

                if (list != null)
                    response = new ResponseJson(list, false, "");
            }

            return response;
        }

    }
}
