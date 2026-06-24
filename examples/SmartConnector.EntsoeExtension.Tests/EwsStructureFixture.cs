using NUnit.Framework;

namespace SmartConnector.EntsoeExtension.Tests
{
    [TestFixture]
    public class EwsStructureFixture
    {
        // -- Root-level constants ------------------------------------------------

        [Test]
        public void Root_IsEntsoe()
        {
            Assert.That(EwsStructure.Root, Is.EqualTo("/ENTSOE"));
        }

        [Test]
        public void CountriesContainer_IsUnderRoot()
        {
            Assert.That(EwsStructure.CountriesContainer, Is.EqualTo("/ENTSOE/Countries"));
        }

        // -- Root-level Meta paths -----------------------------------------------

        [Test]
        public void RootMetaLastUpdate_CorrectPath()
        {
            Assert.That(EwsStructure.MetaLastUpdate,
                Is.EqualTo("/ENTSOE/Meta/LastUpdate"));
        }

        [Test]
        public void RootMetaDataHealthy_CorrectPath()
        {
            Assert.That(EwsStructure.MetaDataHealthy,
                Is.EqualTo("/ENTSOE/Meta/DataHealthy"));
        }

        [Test]
        public void RootMetaLastError_CorrectPath()
        {
            Assert.That(EwsStructure.MetaLastError,
                Is.EqualTo("/ENTSOE/Meta/LastError"));
        }

        [Test]
        public void RootMetaNextUpdate_CorrectPath()
        {
            Assert.That(EwsStructure.MetaNextUpdate,
                Is.EqualTo("/ENTSOE/Meta/NextUpdate"));
        }

        [Test]
        public void RootMetaCycleCount_CorrectPath()
        {
            Assert.That(EwsStructure.MetaCycleCount,
                Is.EqualTo("/ENTSOE/Meta/CycleCount"));
        }

        [Test]
        public void RootMetaRowsConsumed_CorrectPath()
        {
            Assert.That(EwsStructure.MetaRowsConsumed,
                Is.EqualTo("/ENTSOE/Meta/RowsConsumed"));
        }

        [Test]
        public void RootMetaTimeZone_CorrectPath()
        {
            Assert.That(EwsStructure.MetaTimeZone,
                Is.EqualTo("/ENTSOE/Meta/TimeZone"));
        }

        // -- Country folder helper -----------------------------------------------

        [Test]
        public void CountryFolder_IncludesCountryName()
        {
            Assert.That(EwsStructure.CountryFolder("Netherlands"),
                Is.EqualTo("/ENTSOE/Countries/Netherlands"));
        }

        // -- Per-zone structure: path prefix --------------------------------------

        [Test]
        public void ZoneStructure_Root_IncludesCountryAndZone()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.Root,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL"));
        }

        [Test]
        public void ZoneStructure_CountryFolder_MatchesStaticHelper()
        {
            var zone = EwsStructure.ForZone("Sweden", "SE_3");
            Assert.That(zone.CountryFolder,
                Is.EqualTo("/ENTSOE/Countries/Sweden"));
        }

        [Test]
        public void ZoneStructure_MultiZoneCountry_PathsAreDistinct()
        {
            var se1 = EwsStructure.ForZone("Sweden", "SE_1");
            var se3 = EwsStructure.ForZone("Sweden", "SE_3");
            Assert.That(se1.Root, Is.Not.EqualTo(se3.Root));
            Assert.That(se1.CountryFolder, Is.EqualTo(se3.CountryFolder));
        }

        // -- Per-zone structure: Meta paths ---------------------------------------

        [Test]
        public void ZoneStructure_MetaLastUpdate_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.MetaLastUpdate,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Meta/LastUpdate"));
        }

        [Test]
        public void ZoneStructure_MetaCountry_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.MetaCountry,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Meta/Country"));
        }

        [Test]
        public void ZoneStructure_MetaBiddingZone_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Germany", "DE_LU");
            Assert.That(zone.MetaBiddingZone,
                Is.EqualTo("/ENTSOE/Countries/Germany/DE_LU/Meta/BiddingZone"));
        }

        [Test]
        public void ZoneStructure_MetaDataHealthy_CorrectPath()
        {
            var zone = EwsStructure.ForZone("France", "FR");
            Assert.That(zone.MetaDataHealthy,
                Is.EqualTo("/ENTSOE/Countries/France/FR/Meta/DataHealthy"));
        }

        [Test]
        public void ZoneStructure_MetaTimeZone_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.MetaTimeZone,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Meta/TimeZone"));
        }

        // -- Per-zone structure: Price paths --------------------------------------

        [Test]
        public void ZoneStructure_PriceCurrentPrice_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.PriceCurrentPrice,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Price/CurrentPrice"));
        }

        [Test]
        public void ZoneStructure_PriceNextHour_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Germany", "DE_LU");
            Assert.That(zone.PriceNextHour,
                Is.EqualTo("/ENTSOE/Countries/Germany/DE_LU/Price/NextHour"));
        }

        [Test]
        public void ZoneStructure_PriceLevel_CorrectPath()
        {
            var zone = EwsStructure.ForZone("France", "FR");
            Assert.That(zone.PriceLevel,
                Is.EqualTo("/ENTSOE/Countries/France/FR/Price/Level"));
        }

        // -- Hourly prices -------------------------------------------------------

        [Test]
        public void ZoneStructure_HourlyPrices_CorrectContainer()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.HourlyPrices,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Price/Hourly"));
        }

        [Test]
        public void ZoneStructure_HourlyPriceId_Hour00_PadsCorrectly()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.HourlyPriceId(0),
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Price/Hourly/Hour00"));
        }

        [Test]
        public void ZoneStructure_HourlyPriceId_Hour23_PadsCorrectly()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.HourlyPriceId(23),
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Price/Hourly/Hour23"));
        }

        // -- Load paths ----------------------------------------------------------

        [Test]
        public void ZoneStructure_LoadCurrent_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.LoadCurrent,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Load/Current"));
        }

        [Test]
        public void ZoneStructure_LoadForecastPeakToday_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.LoadForecastPeakToday,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Load/ForecastPeakToday"));
        }

        [Test]
        public void ZoneStructure_LoadForecastPeakHour_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.LoadForecastPeakHour,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Load/ForecastPeakHour"));
        }

        [Test]
        public void ZoneStructure_LoadPeakRiskLevel_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.LoadPeakRiskLevel,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Load/PeakRiskLevel"));
        }

        // -- Generation paths (Wind/Solar split) ---------------------------------

        [Test]
        public void ZoneStructure_GenerationForecastTotal_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.GenerationForecastTotal,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Generation/ForecastTotal"));
        }

        [Test]
        public void ZoneStructure_GenerationWind_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.GenerationWind,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Generation/Wind"));
        }

        [Test]
        public void ZoneStructure_GenerationSolar_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.GenerationSolar,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Generation/Solar"));
        }

        // -- Load-shifting paths -------------------------------------------------

        [Test]
        public void ZoneStructure_ShiftIsCurrentlyCheap_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.ShiftIsCurrentlyCheap,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/LoadShifting/IsCurrentlyCheap"));
        }

        [Test]
        public void ZoneStructure_ShiftRenewablePercent_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.ShiftRenewablePercent,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/LoadShifting/RenewablePercent"));
        }

        // -- Trend log paths -----------------------------------------------------

        [Test]
        public void ZoneStructure_TrendCurrentPrice_EndsWithHistory()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.TrendCurrentPrice,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Price/CurrentPriceHistory"));
        }

        [Test]
        public void ZoneStructure_TrendWind_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.TrendWind,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Generation/WindHistory"));
        }

        [Test]
        public void ZoneStructure_TrendSolar_CorrectPath()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.TrendSolar,
                Is.EqualTo("/ENTSOE/Countries/Netherlands/NL/Generation/SolarHistory"));
        }

        // -- Trend definitions array ---------------------------------------------

        [Test]
        public void ZoneStructure_TrendDefinitions_Has11Entries()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.TrendDefinitions.Length, Is.EqualTo(11));
        }

        [Test]
        public void ZoneStructure_TrendDefinitions_AllHaveNonEmptyFields()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            foreach (var (trendId, valueId, name) in zone.TrendDefinitions)
            {
                Assert.That(trendId, Is.Not.Null.And.Not.Empty,
                    $"TrendId is empty for '{name}'");
                Assert.That(valueId, Is.Not.Null.And.Not.Empty,
                    $"ValueId is empty for '{name}'");
                Assert.That(name, Is.Not.Null.And.Not.Empty,
                    "Trend name is empty");
            }
        }

        [Test]
        public void ZoneStructure_TrendDefinitions_TrendIdsDifferFromValueIds()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            foreach (var (trendId, valueId, name) in zone.TrendDefinitions)
            {
                Assert.That(trendId, Is.Not.EqualTo(valueId),
                    $"TrendId and ValueId are identical for '{name}'");
            }
        }

        [Test]
        public void ZoneStructure_TrendDefinitions_ContainsWindAndSolarSeparately()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            var names = new System.Collections.Generic.List<string>();
            foreach (var (_, _, name) in zone.TrendDefinitions)
                names.Add(name);

            Assert.That(names, Does.Contain("Wind MW History Netherlands NL"));
            Assert.That(names, Does.Contain("Solar MW History Netherlands NL"));
            Assert.That(names, Has.None.Contains("Wind/Solar"));
        }

        [Test]
        public void ZoneStructure_TrendDefinitions_NamesIncludeCountryAndZone()
        {
            var zone = EwsStructure.ForZone("Sweden", "SE_1");
            foreach (var (_, _, name) in zone.TrendDefinitions)
            {
                Assert.That(name, Does.EndWith("Sweden SE_1"),
                    $"Trend name '{name}' should be scoped to country and zone");
            }
        }

        [Test]
        public void ZoneStructure_TrendDefinitions_NamesAreUniqueAcrossZones()
        {
            var se1 = EwsStructure.ForZone("Sweden", "SE_1").TrendDefinitions;
            var se2 = EwsStructure.ForZone("Sweden", "SE_2").TrendDefinitions;

            for (int i = 0; i < se1.Length; i++)
            {
                Assert.That(se1[i].Name, Is.Not.EqualTo(se2[i].Name),
                    "Trend names must differ between zones to avoid EWS collisions");
            }
        }

        // -- Properties preserved ------------------------------------------------

        [Test]
        public void ZoneStructure_ZoneCode_PreservesInput()
        {
            var zone = EwsStructure.ForZone("Germany", "DE_LU");
            Assert.That(zone.ZoneCode, Is.EqualTo("DE_LU"));
        }

        [Test]
        public void ZoneStructure_CountryName_PreservesInput()
        {
            var zone = EwsStructure.ForZone("Sweden", "SE_3");
            Assert.That(zone.CountryName, Is.EqualTo("Sweden"));
        }

        // -- Cross-extension path alignment --------------------------------------

        [Test]
        public void StandardInterface_PricePaths_MatchGridStatusConvention()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.PriceCurrentPrice, Does.EndWith("/Price/CurrentPrice"));
            Assert.That(zone.PriceNextHour, Does.EndWith("/Price/NextHour"));
            Assert.That(zone.PriceLevel, Does.EndWith("/Price/Level"));
            Assert.That(zone.PricePriceCategory, Does.EndWith("/Price/PriceCategory"));
            Assert.That(zone.PriceCheapThreshold, Does.EndWith("/Price/CheapThreshold"));
            Assert.That(zone.PriceExpensiveThreshold, Does.EndWith("/Price/ExpensiveThreshold"));
            Assert.That(zone.PriceCurrency, Does.EndWith("/Price/Currency"));
            Assert.That(zone.PriceUnit, Does.EndWith("/Price/Unit"));
        }

        [Test]
        public void StandardInterface_MetaPaths_MatchGridStatusConvention()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.MetaLastUpdate, Does.EndWith("/Meta/LastUpdate"));
            Assert.That(zone.MetaDataHealthy, Does.EndWith("/Meta/DataHealthy"));
            Assert.That(zone.MetaTimeZone, Does.EndWith("/Meta/TimeZone"));
        }

        [Test]
        public void StandardInterface_RootMetaPaths_MatchGridStatusConvention()
        {
            Assert.That(EwsStructure.MetaLastUpdate, Does.EndWith("/Meta/LastUpdate"));
            Assert.That(EwsStructure.MetaDataHealthy, Does.EndWith("/Meta/DataHealthy"));
            Assert.That(EwsStructure.MetaLastError, Does.EndWith("/Meta/LastError"));
        }

        [Test]
        public void StandardInterface_LoadShiftingPaths_MatchGridStatusConvention()
        {
            var zone = EwsStructure.ForZone("Netherlands", "NL");
            Assert.That(zone.ShiftIsCurrentlyCheap,
                Does.EndWith("/LoadShifting/IsCurrentlyCheap"));
            Assert.That(zone.ShiftIsCurrentlyExpensive,
                Does.EndWith("/LoadShifting/IsCurrentlyExpensive"));
            Assert.That(zone.ShiftRenewablePercent,
                Does.EndWith("/LoadShifting/RenewablePercent"));
        }
    }
}
