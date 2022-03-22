using System.Data;
using System.Net;
using System.Threading.Tasks;
using API_Tracy.Models;
using API_Tracy.Providers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace API_PhanCongCongViec.Controllers
{
    [Route("[controller]/[action]")]
    public class TelegramController : Controller
    {
        static string token_telegram = "5175503888:AAELfLeVolTTI3re5-xCxPLD11lEvoBOzuo";

        [HttpGet]
        public async Task<object> ConfirmUser(int userID)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    string phoneNumber = (Connect.getField("tb_User", "phoneNumber", "id", userID) ?? "").ToString();
                    if (phoneNumber == "")
                        response = new ResponseJson(null, true, "Chưa thấy số điện thoại");
                    else
                    {
                        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                        RestClient client = new RestClient(string.Format("https://api.telegram.org/bot{0}/getUpdates", token_telegram));
                        RestRequest request = new RestRequest();
                        request.RequestFormat = DataFormat.Json;
                        request.AddHeader("Accept", "application/json");
                        //request.AddQueryParameter("foo", "bar");
                        //request.AddJsonBody(someObject);

                        dynamic res = JObject.Parse((await client.GetAsync(request)).Content);
                        string message = "";
                        foreach (dynamic item in res.result)
                        {
                            if ((object)item.message != null)
                            {
                                if ((object)item.message.edited_message != null)
                                    message = item.edited_message.text.ToString();
                                else
                                    message = item.message.text.ToString();

                                if (message.Trim() == phoneNumber.Trim())
                                {
                                    message = item.message.chat.id.ToString();
                                    break;
                                }
                                message = "";
                            }
                        }
                        if (message.Trim() == "")
                            response = new ResponseJson(null, true, "Telegram không tìm thấy số điện thoại");
                        else
                        {
                            if (Connect.Exec("UPDATE tb_user set isTelegramConfirm=1, chatID_telegram=@chatID where id=@id",
                                            new string[2] { "@chatID", "@id" },
                                            new object[2] { message, userID }))
                                response = new ResponseJson(null, false, "Đã xác minh");
                        }
                    }
                }
            }
            return response;
        }

        [HttpGet]
        public async Task<object> AutoConfirmUser()
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string author = AuthenFunctionProviders.GetAuthority(Request.Headers);
                if (author == "Administrator")
                {
                    DataTable table = Connect.GetTable("select id from tb_USER where isTelegramConfirm=0 ");
                    if (table.Rows.Count > 0)
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            int userID = int.Parse(row["id"].ToString());

                            string phoneNumber = (Connect.getField("tb_User", "phoneNumber", "id", userID) ?? "").ToString();
                            if (phoneNumber == "")
                                response = new ResponseJson(null, true, "Chưa thấy số điện thoại");
                            else
                            {
                                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                                RestClient client = new RestClient(string.Format("https://api.telegram.org/bot{0}/getUpdates", token_telegram));
                                RestRequest request = new RestRequest();
                                request.RequestFormat = DataFormat.Json;
                                request.AddHeader("Accept", "application/json");
                                //request.AddQueryParameter("foo", "bar");
                                //request.AddJsonBody(someObject);

                                dynamic res = JObject.Parse((await client.GetAsync(request)).Content);
                                string message = "";
                                foreach (dynamic item in res.result)
                                {
                                    if ((object)item.message != null)
                                    {
                                        if ((object)item.message.edited_message != null)
                                            message = item.edited_message.text.ToString();
                                        else
                                            message = item.message.text.ToString();

                                        if (message.Trim() == phoneNumber.Trim())
                                        {
                                            message = item.message.chat.id.ToString();
                                            break;
                                        }
                                        message = "";
                                    }
                                }
                                if (message.Trim() != "")
                                {
                                    Connect.Exec("UPDATE tb_user set isTelegramConfirm=1, chatID_telegram=@chatID where id=@id",
                                                   new string[2] { "@chatID", "@id" },
                                                   new object[2] { message, userID });
                                }
                            }
                        }
                    }
                }
            }
            return response;
        }


        public static object SendMessage(int userID, string message)
        {
            ResponseJson response = new ResponseJson(null, true, "Không có dữ liệu");

            //if (AuthenFunctionProviders.CheckValidate(Request.Headers))
            {
                string chat_id = (Connect.getField("tb_User", "chatID_telegram", "id", userID) ?? "").ToString();
                if (chat_id == "")
                    response = new ResponseJson(null, true, "Chưa xác minh với Telegram");
                else
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                    RestClient client = new RestClient(string.Format("https://api.telegram.org/bot{0}/sendMessage", token_telegram));
                    RestRequest request = new RestRequest();
                    request.RequestFormat = DataFormat.Json;
                    request.AddHeader("Accept", "application/json");

                    request.AddQueryParameter("chat_id", chat_id);
                    request.AddQueryParameter("text", message);
                    //request.AddQueryParameter("disable_notification", false);
                    request.AddQueryParameter("parse_mode", "html");
                    //request.AddJsonBody(someObject);

                    client.GetAsync(request);
                }
            }
            return response;
        }
    }
}
