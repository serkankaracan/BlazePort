namespace BlazePort.Models
{
    public class PingResult
    {
        // Ping başarılı mı?
        public bool Ok { get; private set; }

        // Ping süresi (ms)
        public long? RoundtripTime { get; private set; }

        // Hata mesajı (başarısızsa)
        public string? Error { get; private set; }

        // Constructor private — dışarıdan new edilmesini istemiyoruz
        private PingResult(bool ok, long? roundtripTime, string? error)
        {
            Ok = ok;
            RoundtripTime = roundtripTime;
            Error = error;
        }

        // Başarılı ping üretmek için
        public static PingResult Success(long roundtripTime)
        {
            return new PingResult(true, roundtripTime, null);
        }

        // Başarısız ping üretmek için
        public static PingResult Fail(string message)
        {
            return new PingResult(false, null, message);
        }
    }
}
