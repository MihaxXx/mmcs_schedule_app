using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SchRequests
{
    public class ScheduleUnavailableException : Exception
    {
        public ScheduleUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Collects whether data of a load was served from the offline cache and how old the oldest part is
    /// </summary>
    public class FetchInfo
    {
        public DateTime? StaleSince { get; private set; }

        public string? StaleMessage => StaleSince is { } date
            ? $"Нет подключения. Данные от {date.ToLocalTime():dd.MM.yyyy HH:mm}"
            : null;

        public void Add(DateTime cachedAt)
        {
            if (StaleSince is null || cachedAt < StaleSince)
            {
                StaleSince = cachedAt;
            }
        }

        public void Merge(FetchInfo other)
        {
            if (other.StaleSince is { } date)
            {
                Add(date);
            }
        }
    }

    public class SchRequests
    {
        private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly string _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cache");

        /// <summary>
        /// Set once HTTPS fails without an HTTP status but plain HTTP works (missing root certificate on old devices,
        /// self-signed or expired certificate); requests then go over HTTP for the rest of the session.
        /// </summary>
        private static bool _useCleartext;

        /// <summary>
        /// Requests <paramref name="url"/> and deserializes the JSON response. A valid response is cached on disk;
        /// if the request fails, the last cached response is returned and its date is added to <paramref name="info"/>.
        /// </summary>
        /// <exception cref="ScheduleUnavailableException">Request failed and nothing is cached</exception>
        public static async Task<T> GetAsync<T>(string url, FetchInfo? info = null) where T : class
        {
            try
            {
                string response = await RequestAsync(url);
                T result = DeSerializationObjFromStr<T>(response);
                await SaveCacheAsync(url, response);
                return result;
            }
            catch (ScheduleUnavailableException)
            {
                if (await ReadCacheAsync(url) is not var (cached, cachedAt))
                {
                    throw;
                }

                T result = DeSerializationObjFromStr<T>(cached);
                info?.Add(cachedAt);
                return result;
            }
        }

        /// <summary>
        /// Выполнение запроса и возврат строки-ответа
        /// </summary>
        public static async Task<string> RequestAsync(string url, CancellationToken ct = default)
        {
            try
            {
                return await _client.GetStringAsync(_useCleartext ? ToCleartext(url) : url, ct);
            }
            catch (HttpRequestException e) when (e.StatusCode is null && CanFallBackToCleartext(url))
            {
                string response;
                try
                {
                    response = await _client.GetStringAsync(ToCleartext(url), ct);
                }
                catch (Exception fallbackError) when (IsRequestFailure(fallbackError, ct))
                {
                    throw new ScheduleUnavailableException($"Request failed: {url}", e);
                }

                _useCleartext = true;
                return response;
            }
            catch (Exception e) when (IsRequestFailure(e, ct))
            {
                throw new ScheduleUnavailableException($"Request failed: {url}", e);
            }
        }

        /// <summary>
        /// Десериализация в объект по строке
        /// </summary>
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

        private static string ToCleartext(string url)
        {
            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "http://" + url["https://".Length..] : url;
        }

        private static bool CanFallBackToCleartext(string url)
        {
            return !_useCleartext
                && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
        }

        private static bool IsRequestFailure(Exception e, CancellationToken ct)
        {
            return e is HttpRequestException or IOException || (e is TaskCanceledException && !ct.IsCancellationRequested);
        }

        private static string GetCachePath(string url)
        {
            Uri uri = new(url);
            return Path.Combine(_cacheDirectory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.Host + uri.PathAndQuery))) + ".json");
        }

        /// <summary>
        /// Semesters start on 1 February and 1 September; week numbering and schedules don't carry over
        /// </summary>
        private static DateTime GetSemesterStart(DateTime now)
        {
            if (now >= new DateTime(now.Year, 9, 1))
            {
                return new DateTime(now.Year, 9, 1);
            }
            return now >= new DateTime(now.Year, 2, 1) ? new DateTime(now.Year, 2, 1) : new DateTime(now.Year - 1, 9, 1);
        }

        private static async Task SaveCacheAsync(string url, string response)
        {
            try
            {
                Directory.CreateDirectory(_cacheDirectory);
                string path = GetCachePath(url);
                string tempPath = path + ".tmp";
                await File.WriteAllTextAsync(tempPath, response, Encoding.UTF8);
                File.Move(tempPath, path, true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        private static async Task<(string Response, DateTime CachedAt)?> ReadCacheAsync(string url)
        {
            string path = GetCachePath(url);
            try
            {
                DateTime cachedAt = File.GetLastWriteTimeUtc(path);
                if (File.Exists(path) && cachedAt.ToLocalTime() >= GetSemesterStart(DateTime.Now))
                {
                    return (await File.ReadAllTextAsync(path, Encoding.UTF8), cachedAt);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
            return null;
        }
    }
}
