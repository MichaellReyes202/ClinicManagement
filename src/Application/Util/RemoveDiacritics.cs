using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Util;
public static class RemoveDiacritics
{
    public static string Remove(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (char c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string FormatNextAppointment(DateTime dt, DateTime tomorrow)
    {
        var isTomorrow = dt.Date == tomorrow;
        var timeStr = dt.ToString("hh:mm tt", new CultureInfo("es-ES")).ToUpper();
        return isTomorrow ? $"Mañana {timeStr}" : timeStr;
    }
}
