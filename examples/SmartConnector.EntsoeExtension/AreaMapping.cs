using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartConnector.EntsoeExtension
{
    public enum SupportedCountry
    {
        Austria,
        Belgium,
        Bulgaria,
        Croatia,
        CzechRepublic,
        Denmark,
        Estonia,
        Finland,
        France,
        Germany,
        GreatBritain,
        Greece,
        Hungary,
        Ireland,
        Italy,
        Latvia,
        Lithuania,
        Luxembourg,
        Netherlands,
        Norway,
        Poland,
        Portugal,
        Romania,
        Serbia,
        Slovakia,
        Slovenia,
        Spain,
        Sweden,
        Switzerland
    }

    public static class AreaMapping
    {
        public sealed class AreaInfo
        {
            public string Code { get; }
            public string EicCode { get; }
            public string Description { get; }
            public string TimeZoneId { get; }

            public AreaInfo(string code, string eicCode, string description, string timeZoneId)
            {
                Code = code ?? throw new ArgumentNullException(nameof(code));
                EicCode = eicCode ?? throw new ArgumentNullException(nameof(eicCode));
                Description = description ?? throw new ArgumentNullException(nameof(description));
                TimeZoneId = timeZoneId ?? throw new ArgumentNullException(nameof(timeZoneId));
            }
        }

        private static readonly Dictionary<string, AreaInfo> Areas =
            new Dictionary<string, AreaInfo>(StringComparer.OrdinalIgnoreCase)
            {
                // Western Europe
                { "AT", new AreaInfo("AT", "10YAT-APG------L", "Austria", "W. Europe Standard Time") },
                { "BE", new AreaInfo("BE", "10YBE----------2", "Belgium", "Romance Standard Time") },
                { "CH", new AreaInfo("CH", "10YCH-SWISSGRIDZ", "Switzerland", "W. Europe Standard Time") },
                { "DE_LU", new AreaInfo("DE_LU", "10Y1001A1001A82H", "Germany-Luxembourg", "W. Europe Standard Time") },
                { "FR", new AreaInfo("FR", "10YFR-RTE------C", "France", "Romance Standard Time") },
                { "LU", new AreaInfo("LU", "10YLU-CEGEDEL-NQ", "Luxembourg", "W. Europe Standard Time") },
                { "NL", new AreaInfo("NL", "10YNL----------L", "Netherlands", "W. Europe Standard Time") },

                // Nordic
                { "DK_1", new AreaInfo("DK_1", "10YDK-1--------W", "Denmark West", "Romance Standard Time") },
                { "DK_2", new AreaInfo("DK_2", "10YDK-2--------M", "Denmark East", "Romance Standard Time") },
                { "FI", new AreaInfo("FI", "10YFI-1--------U", "Finland", "FLE Standard Time") },
                { "NO_1", new AreaInfo("NO_1", "10YNO-1--------2", "Norway South-East", "W. Europe Standard Time") },
                { "NO_2", new AreaInfo("NO_2", "10YNO-2--------T", "Norway South-West", "W. Europe Standard Time") },
                { "NO_3", new AreaInfo("NO_3", "10YNO-3--------J", "Norway Central", "W. Europe Standard Time") },
                { "NO_4", new AreaInfo("NO_4", "10YNO-4--------9", "Norway North", "W. Europe Standard Time") },
                { "NO_5", new AreaInfo("NO_5", "10Y1001A1001A48H", "Norway West", "W. Europe Standard Time") },
                { "SE_1", new AreaInfo("SE_1", "10Y1001A1001A44P", "Sweden North", "W. Europe Standard Time") },
                { "SE_2", new AreaInfo("SE_2", "10Y1001A1001A45N", "Sweden Central-North", "W. Europe Standard Time") },
                { "SE_3", new AreaInfo("SE_3", "10Y1001A1001A46L", "Sweden Central-South", "W. Europe Standard Time") },
                { "SE_4", new AreaInfo("SE_4", "10Y1001A1001A47J", "Sweden South", "W. Europe Standard Time") },

                // Eastern Europe
                { "BG", new AreaInfo("BG", "10YCA-BULGARIA-R", "Bulgaria", "FLE Standard Time") },
                { "CZ", new AreaInfo("CZ", "10YCZ-CEPS-----N", "Czech Republic", "Central Europe Standard Time") },
                { "EE", new AreaInfo("EE", "10Y1001A1001A39I", "Estonia", "FLE Standard Time") },
                { "HR", new AreaInfo("HR", "10YHR-HEP------M", "Croatia", "Central Europe Standard Time") },
                { "HU", new AreaInfo("HU", "10YHU-MAVIR----U", "Hungary", "Central Europe Standard Time") },
                { "LT", new AreaInfo("LT", "10YLT-1001A0008Q", "Lithuania", "FLE Standard Time") },
                { "LV", new AreaInfo("LV", "10YLV-1001A00074", "Latvia", "FLE Standard Time") },
                { "PL", new AreaInfo("PL", "10YPL-AREA-----S", "Poland", "Central Europe Standard Time") },
                { "RO", new AreaInfo("RO", "10YRO-TEL------P", "Romania", "GTB Standard Time") },
                { "SK", new AreaInfo("SK", "10YSK-SEPS-----K", "Slovakia", "Central Europe Standard Time") },
                { "SI", new AreaInfo("SI", "10YSI-ELES-----O", "Slovenia", "Central Europe Standard Time") },

                // Southern Europe
                { "ES", new AreaInfo("ES", "10YES-REE------0", "Spain", "Romance Standard Time") },
                { "GR", new AreaInfo("GR", "10YGR-HTSO-----Y", "Greece", "GTB Standard Time") },
                { "IT_NORD", new AreaInfo("IT_NORD", "10Y1001A1001A73I", "Italy North", "W. Europe Standard Time") },
                { "PT", new AreaInfo("PT", "10YPT-REN------W", "Portugal", "GMT Standard Time") },
                { "RS", new AreaInfo("RS", "10YCS-SERBIATSOV", "Serbia", "Central Europe Standard Time") },

                // British Isles
                { "GB", new AreaInfo("GB", "10YGB----------A", "Great Britain", "GMT Standard Time") },
                { "IE_SEM", new AreaInfo("IE_SEM", "10Y1001A1001A59C", "Ireland (SEM)", "GMT Standard Time") },
            };

        private static readonly Dictionary<SupportedCountry, (string DisplayName, string[] ZoneCodes)> CountryZones =
            new Dictionary<SupportedCountry, (string, string[])>
            {
                { SupportedCountry.Austria,       ("Austria",       new[] { "AT" }) },
                { SupportedCountry.Belgium,       ("Belgium",       new[] { "BE" }) },
                { SupportedCountry.Bulgaria,      ("Bulgaria",      new[] { "BG" }) },
                { SupportedCountry.Croatia,       ("Croatia",       new[] { "HR" }) },
                { SupportedCountry.CzechRepublic, ("Czech Republic", new[] { "CZ" }) },
                { SupportedCountry.Denmark,       ("Denmark",       new[] { "DK_1", "DK_2" }) },
                { SupportedCountry.Estonia,       ("Estonia",       new[] { "EE" }) },
                { SupportedCountry.Finland,       ("Finland",       new[] { "FI" }) },
                { SupportedCountry.France,        ("France",        new[] { "FR" }) },
                { SupportedCountry.Germany,       ("Germany",       new[] { "DE_LU" }) },
                { SupportedCountry.GreatBritain,  ("Great Britain", new[] { "GB" }) },
                { SupportedCountry.Greece,        ("Greece",        new[] { "GR" }) },
                { SupportedCountry.Hungary,       ("Hungary",       new[] { "HU" }) },
                { SupportedCountry.Ireland,       ("Ireland",       new[] { "IE_SEM" }) },
                { SupportedCountry.Italy,         ("Italy",         new[] { "IT_NORD" }) },
                { SupportedCountry.Latvia,        ("Latvia",        new[] { "LV" }) },
                { SupportedCountry.Lithuania,     ("Lithuania",     new[] { "LT" }) },
                { SupportedCountry.Luxembourg,    ("Luxembourg",    new[] { "LU" }) },
                { SupportedCountry.Netherlands,   ("Netherlands",   new[] { "NL" }) },
                { SupportedCountry.Norway,        ("Norway",        new[] { "NO_1", "NO_2", "NO_3", "NO_4", "NO_5" }) },
                { SupportedCountry.Poland,        ("Poland",        new[] { "PL" }) },
                { SupportedCountry.Portugal,      ("Portugal",      new[] { "PT" }) },
                { SupportedCountry.Romania,       ("Romania",       new[] { "RO" }) },
                { SupportedCountry.Serbia,        ("Serbia",        new[] { "RS" }) },
                { SupportedCountry.Slovakia,      ("Slovakia",      new[] { "SK" }) },
                { SupportedCountry.Slovenia,      ("Slovenia",      new[] { "SI" }) },
                { SupportedCountry.Spain,         ("Spain",         new[] { "ES" }) },
                { SupportedCountry.Sweden,        ("Sweden",        new[] { "SE_1", "SE_2", "SE_3", "SE_4" }) },
                { SupportedCountry.Switzerland,   ("Switzerland",   new[] { "CH" }) },
            };

        public static AreaInfo Lookup(string zoneCode)
        {
            if (string.IsNullOrWhiteSpace(zoneCode))
                return null;

            Areas.TryGetValue(zoneCode.Trim(), out var area);
            return area;
        }

        public static string[] GetZoneCodes(SupportedCountry country)
        {
            return CountryZones.TryGetValue(country, out var entry)
                ? entry.ZoneCodes
                : new string[0];
        }

        public static string GetCountryDisplayName(SupportedCountry country)
        {
            return CountryZones.TryGetValue(country, out var entry)
                ? entry.DisplayName
                : country.ToString();
        }

        public static IReadOnlyCollection<string> SupportedCodes =>
            Areas.Keys.ToList().AsReadOnly();

        public static TimeZoneInfo GetTimeZone(string zoneCode)
        {
            var area = Lookup(zoneCode);
            if (area == null) return null;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(area.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
        }
    }
}
