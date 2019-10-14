using System.Text;
using System.Net;
using System;
using Newtonsoft.Json;

namespace SchRequests
{
    /// <summary>
    /// Используем вебреквест
    /// </summary>
    public class TimedWebClient : WebClient
    {
        // Timeout in milliseconds, default = 600,000 msec
        public int Timeout { get; set; }

        public TimedWebClient()
        {
            this.Timeout = 600000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = this.Timeout;
            return objWebRequest;
        }
    }
    public class SchRequests
    {
        /// <summary>
        /// Выполнение запроса и возврат строки-ответа
        /// </summary>
        public static string Request(string url)
        {
            // Выполняем запрос по адресу и получаем ответ в виде строки (Используем вебреквест!)
            string response = new TimedWebClient { Timeout = 5000 }.DownloadString(url);
            // Исправляем кодировку
            //ChangeEncoding(ref response);
            // Возвращаем строку-ответ (формат JSON)
            return response;
        }


        /// <summary>
        /// Десериализация в объект по строке
        /// </summary>
        public static T DeSerializationObjFromStr<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }

        /// <summary>
        /// Десериализация в массив объектов по строке
        /// </summary>
        public static T[] DeSerializationFromStr<T>(string str)
        {
            return JsonConvert.DeserializeObject<T[]>(str);
        }
    }
}
