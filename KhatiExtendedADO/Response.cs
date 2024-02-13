using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KhatiExtendedADO
{
    public class Response<T>
    {
        public bool Success { set; get; }
        public T? Data { set; get; }
        public string? Message { set; get; }
        public string? Exception { set; get; }
    }
}
