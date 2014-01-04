using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public partial class UserDefinedFunctions
{
    public static readonly Regex rxJsonAll_old = new Regex(
            "\"(?<itemKey>(?:\\^{0,1})[\\.\\*a-zA-Z0-9/\\-\\s\\#{}:/_]*/{0,1})\":(?<itemValue" +
            ">(?>\"([^\\\\\"]|\\\\\\\\|\\\\\")*\")|\\[(?>\\{(?>[^{}]+" +
            "|\\{(?<Element>)|\\}(?<-Element>))*(?(Element)(?!))(?:\\}[,]" +
            "*))*(?>\"(?>[^\\\"]+|\"(?<Element>)|\"(?<-Element>))*(?(El" +
            "ement)(?!))(?:\"[,]*))*(?>-{0,1}\\d*[.\\d+][,]*)*\\]|\\{" +
            "(?>[^{}]+|\\{(?<Element>)|\\}(?<-Element>))*(?(Element)(?!))" +
            "\\}|(?>true|false)|(?>-{0,1}\\d*[.\\d+])*)|(?<ItemValu" +
            "e>(?>\\{(?>[^{}]+|\\{(?<Element>)|\\}(?<-Element>))*(?(Eleme" +
            "nt)(?!))\\}+))",
        RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled
    );

    public static readonly Regex rxJsonAll = new Regex(
      "\"(?<itemKey>(?:\\^{0,1})[\\.\\*a-zA-Z0-9/\\-\\s\\#{}:/_]*/{" +
      "0,1})\":\\s*(?<itemValue>\r\n\\[(?>[^\\[\\]]+|\\[ (?<element>)" +
      "|\\](?<-element>))*(?(element)(?!))\\]\r\n|\\{(?>[^\\{\\}]+|\\{" +
      " (?<element>)|\\} (?<-element>))*(?(element)(?!))\\}\r\n|(?>\"(" +
      "?:[^\\\\\"]|\\\\\\\\|\\\\\")*\")\r\n|(?>true|false)\r\n|(?>-{0,1" +
      "}\\d*[.\\d+])*)\r\n|(?<itemValue>(?>\\{(?>[^{}]+|\\{(?<element" +
      ">)|\\}(?<-element>))*(?(element)(?!))\\}+))",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex jsUnixDate = new Regex(
      "^(?>(?:[\"]?[\\\\]?[/]?)(?<unixDate>(?:new\\s)?Date\\((?<value>[\\-]?\\d+)\\))(?:[\\\\]?[/]?[\"]?))$",
    RegexOptions.Singleline
    | RegexOptions.CultureInvariant
    | RegexOptions.Compiled
    );
     
    public static readonly Regex rxIncludedKey = new Regex(
            "(?:\"(?<IncludedKey>[\\*a-zA-Z0-9]*/{1})\":\"(?<DocumentID>[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12})\"){1,}",
        RegexOptions.IgnoreCase
        | RegexOptions.Multiline
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled
    );

    public static readonly Regex rxIsUUID = new Regex(
      "^(?<UUID>[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12})$",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxUrl = new Regex(
        "(?<Url>(?:/(?<Node>[\\*a-zA-Z0-9]*/{0,1}):(?<NodeKey>" +
        "(?<=:)[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-" +
        "9]{4}-[a-zA-Z0-9]{12}))+/(?<itemKey>[\\*a-zA-Z0-9]*/{0,1})|/" +
        "(?<itemKey>[\\*a-zA-Z0-9]*/{0,1}))",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
   );

    public static readonly Regex rxKeyInUrl = new Regex(
          "(?<Node>[\\*a-zA-Z0-9]*/{0,1}):(?<NodeKey>(?<=:)[a-zA" +
          "-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA" +
          "-Z0-9]{12})",
        RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled
        );

    public static readonly Regex rxUrlAncestry = new Regex(
      "(?>/(?<Object>[\\*a-zA-Z0-9]*:(?<=:)[a-zA-Z0-9]{8}-[a-zA-Z0-" +
      "9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}))+",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxParseArrayOfObjects = new Regex(
        "(?<ArrayObject>\\{(?>[^{}]+|\\{(?<Element>)|\\}(?<-Element>))*(?(Element)(?!))\\})+",
    RegexOptions.IgnoreCase
    | RegexOptions.Multiline
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxKey = new Regex(
          "(?:\"(?<ObjectKey>\\*[a-zA-Z0-9]*/{0,1})\":\"(?<ObjectID>[a-" +
          "zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-" +
          "zA-Z0-9]{12})\"){1,}",
        RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled
    );

    public static readonly Regex rxJsonIndexDocument = new Regex(
      "\"(?<IndexKey>(?:\\^{0,1})[\\*a-zA-Z0-9/\\-\\s\\#]*/{0,1})\":" +
      "(?<IndexValue>\r\n(?>\"(?:[^\\\\\"]|\\\\\\\\|\\\\\")*\")\r\n|\r\n\\[" +
      "\r\n    (?>\r\n        [^\\[\\]]+ \r\n    )*\r\n    (?(number)(?!))\r\n" +
      "\\]\r\n)",
    RegexOptions.IgnoreCase
    | RegexOptions.Multiline
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxJsonMapIndex = new Regex(
      "\"(?<Url>(?:(?<Reference>(?>[^/][a-zA_Z0-9]*){0,1})(?<Select" +
      "or>(?:/[\\*a-zA-Z0-9]*/{0,1}:\\{0\\})+/[\\*a-zA-Z0-9]*/{0,1}" +
      "\\${0,1}|/[\\*a-zA-Z0-9]*/{0,1}\\${0,1})))\"",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );
    public static readonly Regex rxJsonMergeIndex = new Regex(
    "(?<Merge>\r\n(?:\"Source\":\"(?<SourceUrl>(?:(?<SourceReferenc" +
    "e>(?>[^/][a-zA_Z0-9]*){0,1})(?<SourceSelector>(?:/[\\*a-zA-Z" +
    "0-9]*/{0,1}:\\{0\\})+/[\\*a-zA-Z0-9]*/{0,1}\\${0,1}|/[\\*a-z" +
    "A-Z0-9]*/{0,1}\\${0,1})))\"){1}\r\n(?:,\"Target\":\"(?<TargetU" +
    "rl>(?:(?<TargetReference>(?>[^/][a-zA_Z0-9]*){0,1})(?<Target" +
    "Selector>(?:/[\\*a-zA-Z0-9]*/{0,1}:\\{0\\})+/[\\*a-zA-Z0-9]*" +
    "/{0,1}\\${0,1}|/[\\*a-zA-Z0-9]*/{0,1}\\${0,1})))\"){1}\r\n(?:," +
    "\"SourceKey\":\"(?<SourceKey>(?:[^\\\\\"]|\\\\\\\\|\\\\\")*)" +
    "\"){0,1}\r\n(?:,\"TargetKey\":\"(?<TargetKey>(?:[^\\\\\"]|\\\\\\\\|" +
    "\\\\\")*)\"){0,1}\r\n(?:,\"SourceValue\":\"(?<SourceValue>(?:[" +
    "^\\\\\"]|\\\\\\\\|\\\\\")*)\"){0,1}\r\n(?:,\"TargetValue\":\"(" +
    "?<TargetValue>(?:[^\\\\\"]|\\\\\\\\|\\\\\")*)\"){0,1}\r\n)+",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxJsonReduceIndex = new Regex(
      "(?<Reduce>\r\n(?:\"Selector\":\"(?<Url>(?:(?<Reference>(?>[^/]" +
      "[a-zA_Z0-9]*){0,1})(?<Selector>(?:/[\\*a-zA-Z0-9]*/{0,1}:\\{" +
      "0\\})+/[\\*a-zA-Z0-9]*/{0,1}\\${0,1}|/[\\*a-zA-Z0-9]*/{0,1}\\$" +
      "{0,1})))\"){1}\r\n(?:,\"Filter\":\"(?<Filter>(?:[^\\\\\"]|\\\\\\\\|" +
      "\\\\\")*)\"){0,1}\r\n(?:,\"Operand\":\"(?<Operand>Equal|NotEqu" +
      "al|LessThan|GreaterThan)\"){0,1}\r\n(?:,\"FilterValue\":(?<Fil" +
      "terValue>(?>\"(?:[^\\\\\"]|\\\\\\\\|\\\\\")*\")|(?>true|fals" +
      "e)|(?>-{0,1}\\d*[.\\d+])*)){0,1}\r\n(?:,\"IsVisible\":(?<IsVis" +
      "ible>true|false)){0,1}\r\n(?:,\"Label\":\"(?<Label>(?:[^\\\\\"]" +
      "|\\\\\\\\|\\\\\")*)\"){0,1})+",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxSimpleStringArray = new Regex(
      "\"(?<ArrayItem>(?>(?:[^\\\\\"]|\\\\\\\\|\\\\\")*))\"\r\n",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

    public static readonly Regex rxSimpleNumericArray = new Regex(
          "(?<ArrayItem>(?>-{0,1}\\d*[.\\d]+[,]*?))\r\n",
        RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant
        | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled
        );
}
