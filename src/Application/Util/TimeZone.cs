using Microsoft.AspNetCore.Http;
using TimeZoneConverter;

namespace Application.Util;
public static class GetTimeZone
{
    public static TimeZoneInfo GetRequestTimeZone(IHttpContextAccessor httpContextAccessor)
    {
        // 1. Intentar leer la cabecera "X-Timezone"
        var timezoneHeader = httpContextAccessor.HttpContext?.Request.Headers["X-Timezone"].ToString();

        if (string.IsNullOrEmpty(timezoneHeader))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TZConvert.GetTimeZoneInfo(timezoneHeader);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
