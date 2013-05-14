using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

public static class JsonExtension
{
    public static String WithIdentity(this String ix)
    {
        if (String.IsNullOrEmpty(ix))
            return String.Empty;
        String rxUUID = "[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}";
        String pattern;
        if (ix.CompareString().Contains("{0}"))
            pattern = String.Concat("^", String.Format(ix, rxUUID));
        else pattern = String.Concat("^", ix);

        return pattern;
    }

    public static Boolean isUUID(this String text)
    {
        return Regex.Match(text, "^[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}$").Success;
    }

    public static String CompareString(this String text)
    {
        return String.IsNullOrEmpty(text).Equals(true) ? String.Empty : text.ToLower();
    }

    public static String UnQuote(this String text)
    {
        return String.IsNullOrEmpty(text).Equals(true) | text.Equals("\"\"") ? String.Empty : text.StartsWith("\"") && text.Length > 2 ? text.Substring(1, text.Length - 2) : text;
    }
}