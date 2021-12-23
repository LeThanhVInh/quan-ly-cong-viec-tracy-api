using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace API_Tracy.Models
{
    public class ResponseJson
    {
        public ResponseJson(object _data, bool _isError, string _message )
        {
            this.Data = _data;
            this.IsError = _isError;
            this.Message = _message; 
        }
        public object Data { get; set; }
        public bool IsError { get; set; }
        public string Message { get; set; } 
    }
}