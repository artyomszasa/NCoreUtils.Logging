namespace NCoreUtils.Logging.Google.Data
{
    public class ErrorContext
    {
        public string Method { get; }

        public string Url { get; }

        public string UserAgent { get; }

        public string Referer { get; }

        public int ResponseStatusCode { get; }

        public string RemoteIp { get; }

        public string User { get; }

        public ErrorContext(string method, string url, string userAgent, string referer, int responseStatusCode, string remoteIp, string user)
        {
            Method = method;
            Url = url;
            UserAgent = userAgent;
            Referer = referer;
            ResponseStatusCode = responseStatusCode;
            RemoteIp = remoteIp;
            User = user;
        }
    }
}