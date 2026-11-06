using Newtonsoft.Json;

namespace SchRequests
{
    /// <summary>
    /// Thrown when schedule data can't be obtained or parsed:
    /// no connection, timeout, HTTP error status or a malformed (non-JSON) response.
    /// </summary>
    public class ScheduleUnavailableException : Exception
    {
        public ScheduleUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    public class SchRequests
    {
        private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };

        /// <summary>
        /// Выполнение запроса и возврат строки-ответа (синхронно)
        /// </summary>
        /// <exception cref="ScheduleUnavailableException"/>
        public static string Request(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = _client.Send(request);
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException)
            {
                throw new ScheduleUnavailableException($"Request failed: {url}", e);
            }
        }

        /// <summary>
        /// Выполнение запроса и возврат строки-ответа (асинхронно)
        /// </summary>
        /// <exception cref="ScheduleUnavailableException"/>
        public static async Task<string> RequestAsync(string url, CancellationToken ct = default)
        {
            try
            {
                return await _client.GetStringAsync(url, ct);
            }
            catch (Exception e) when (e is HttpRequestException or IOException || (e is TaskCanceledException && !ct.IsCancellationRequested))
            {
                throw new ScheduleUnavailableException($"Request failed: {url}", e);
            }
        }

        /// <summary>
        /// Десериализация в объект по строке
        /// </summary>
        /// <exception cref="ScheduleUnavailableException"/>
        public static T DeSerializationObjFromStr<T>(string str) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(str)
                    ?? throw new ScheduleUnavailableException("Empty response");
            }
            catch (JsonException e)
            {
                throw new ScheduleUnavailableException("Malformed response", e);
            }
        }

        /// <summary>
        /// Десериализация в массив объектов по строке
        /// </summary>
        /// <exception cref="ScheduleUnavailableException"/>
        public static T[] DeSerializationFromStr<T>(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<T[]>(str)
                    ?? throw new ScheduleUnavailableException("Empty response");
            }
            catch (JsonException e)
            {
                throw new ScheduleUnavailableException("Malformed response", e);
            }
        }
    }
}
