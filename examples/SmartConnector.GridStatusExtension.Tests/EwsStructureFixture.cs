// ---------------------------------------------------------------------------
// EWS Structure Tests
//
// Verifies that EWS path generation produces the correct AlternateId strings
// for all points in the Standard Energy Market Interface.
// ---------------------------------------------------------------------------

using NUnit.Framework;

namespace SmartConnector.GridStatusExtension.Tests
{
    [TestFixture]
    public class EwsStructureFixture
    {
        // -- Root-level constants ------------------------------------------------

        [Test]
        public void Root_IsGrid()
        {
            Assert.That(EwsStructure.Root, Is.EqualTo("/Grid"));
        }

        [Test]
        public void MetaLastUpdate_IsUnderGridMeta()
        {
            Assert.That(EwsStructure.MetaLastUpdate, Is.EqualTo("/Grid/Meta/LastUpdate"));
        }

        [Test]
        public void MetaDataHealthy_IsUnderGridMeta()
        {
            Assert.That(EwsStructure.MetaDataHealthy, Is.EqualTo("/Grid/Meta/DataHealthy"));
        }

        [Test]
        public void MetaLastError_IsUnderGridMeta()
        {
            Assert.That(EwsStructure.MetaLastError, Is.EqualTo("/Grid/Meta/LastError"));
        }

        [Test]
        public void MetaCycleCount_IsUnderGridMeta()
        {
            Assert.That(EwsStructure.MetaCycleCount, Is.EqualTo("/Grid/Meta/CycleCount"));
        }

        [Test]
        public void MetaRowsConsumed_IsUnderGridMeta()
        {
            Assert.That(EwsStructure.MetaRowsConsumed, Is.EqualTo("/Grid/Meta/RowsConsumed"));
        }

        // -- Per-ISO structure: Price paths -----------------------------------

        [Test]
        public void IsoStructure_PriceCurrentPrice_ContainsIsoCode()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceCurrentPrice, Is.EqualTo("/Grid/CAISO/Price/CurrentPrice"));
        }

        [Test]
        public void IsoStructure_PriceNextHour_CorrectPath()
        {
            var iso = EwsStructure.ForIso("ERCOT");
            Assert.That(iso.PriceNextHour, Is.EqualTo("/Grid/ERCOT/Price/NextHour"));
        }

        [Test]
        public void IsoStructure_PriceLevel_CorrectPath()
        {
            var iso = EwsStructure.ForIso("PJM");
            Assert.That(iso.PriceLevel, Is.EqualTo("/Grid/PJM/Price/Level"));
        }

        [Test]
        public void IsoStructure_PricePriceCategory_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PricePriceCategory, Is.EqualTo("/Grid/CAISO/Price/PriceCategory"));
        }

        [Test]
        public void IsoStructure_PriceRankToday_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceRankToday, Is.EqualTo("/Grid/CAISO/Price/RankToday"));
        }

        [Test]
        public void IsoStructure_PriceNegativeActive_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceNegativeActive, Is.EqualTo("/Grid/CAISO/Price/NegativeActive"));
        }

        [Test]
        public void IsoStructure_PriceCheapThreshold_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceCheapThreshold, Is.EqualTo("/Grid/CAISO/Price/CheapThreshold"));
        }

        [Test]
        public void IsoStructure_PriceCurrency_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceCurrency, Is.EqualTo("/Grid/CAISO/Price/Currency"));
        }

        [Test]
        public void IsoStructure_PriceUnit_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceUnit, Is.EqualTo("/Grid/CAISO/Price/Unit"));
        }

        // -- Hourly prices ----------------------------------------------------

        [Test]
        public void IsoStructure_HourlyPrices_CorrectContainer()
        {
            var iso = EwsStructure.ForIso("MISO");
            Assert.That(iso.HourlyPrices, Is.EqualTo("/Grid/MISO/Price/Hourly"));
        }

        [Test]
        public void IsoStructure_HourlyPriceId_Hour00_PadsCorrectly()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.HourlyPriceId(0), Is.EqualTo("/Grid/CAISO/Price/Hourly/Hour00"));
        }

        [Test]
        public void IsoStructure_HourlyPriceId_Hour23_PadsCorrectly()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.HourlyPriceId(23), Is.EqualTo("/Grid/CAISO/Price/Hourly/Hour23"));
        }

        [Test]
        public void IsoStructure_HourlyPriceId_Hour09_PadsWithLeadingZero()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.HourlyPriceId(9), Is.EqualTo("/Grid/CAISO/Price/Hourly/Hour09"));
        }

        // -- Load shifting paths ----------------------------------------------

        [Test]
        public void IsoStructure_ShiftIsCurrentlyCheap_CorrectPath()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.ShiftIsCurrentlyCheap,
                Is.EqualTo("/Grid/CAISO/LoadShifting/IsCurrentlyCheap"));
        }

        // -- Load paths -------------------------------------------------------

        [Test]
        public void IsoStructure_LoadCurrent_CorrectPath()
        {
            var iso = EwsStructure.ForIso("NYISO");
            Assert.That(iso.LoadCurrent, Is.EqualTo("/Grid/NYISO/Load/Current"));
        }

        // -- Carbon paths -----------------------------------------------------

        [Test]
        public void IsoStructure_CarbonGridIntensity_CorrectPath()
        {
            var iso = EwsStructure.ForIso("ISONE");
            Assert.That(iso.CarbonGridIntensity, Is.EqualTo("/Grid/ISONE/Carbon/GridIntensity"));
        }

        [Test]
        public void IsoStructure_CarbonLevel_CorrectPath()
        {
            var iso = EwsStructure.ForIso("ISONE");
            Assert.That(iso.CarbonLevel, Is.EqualTo("/Grid/ISONE/Carbon/Level"));
        }

        // -- Trend log paths --------------------------------------------------

        [Test]
        public void IsoStructure_TrendCurrentPrice_EndsWithHistory()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.TrendCurrentPrice,
                Is.EqualTo("/Grid/CAISO/Price/CurrentPriceHistory"));
        }

        [Test]
        public void IsoStructure_TrendRealTimeLmp_EndsWithHistory()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.TrendRealTimeLmp,
                Is.EqualTo("/Grid/CAISO/Price/RealTimeLMPHistory"));
        }

        // -- Trend definitions array ------------------------------------------

        [Test]
        public void IsoStructure_TrendDefinitions_Has14Entries()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.TrendDefinitions.Length, Is.EqualTo(14));
        }

        [Test]
        public void IsoStructure_TrendDefinitions_AllHaveNonEmptyNames()
        {
            var iso = EwsStructure.ForIso("CAISO");
            foreach (var (trendId, valueId, name) in iso.TrendDefinitions)
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
        public void IsoStructure_TrendDefinitions_TrendIdsDifferFromValueIds()
        {
            var iso = EwsStructure.ForIso("CAISO");
            foreach (var (trendId, valueId, name) in iso.TrendDefinitions)
            {
                Assert.That(trendId, Is.Not.EqualTo(valueId),
                    $"TrendId and ValueId are identical for '{name}'");
            }
        }

        // -- Per-ISO metadata paths -------------------------------------------

        [Test]
        public void IsoStructure_MetaLastUpdate_CorrectPath()
        {
            var iso = EwsStructure.ForIso("SPP");
            Assert.That(iso.MetaLastUpdate, Is.EqualTo("/Grid/SPP/Meta/LastUpdate"));
        }

        [Test]
        public void IsoStructure_MetaDataHealthy_CorrectPath()
        {
            var iso = EwsStructure.ForIso("SPP");
            Assert.That(iso.MetaDataHealthy, Is.EqualTo("/Grid/SPP/Meta/DataHealthy"));
        }

        [Test]
        public void IsoStructure_MetaTimeZone_CorrectPath()
        {
            var iso = EwsStructure.ForIso("SPP");
            Assert.That(iso.MetaTimeZone, Is.EqualTo("/Grid/SPP/Meta/TimeZone"));
        }

        // -- Cross-extension path alignment -----------------------------------

        [Test]
        public void StandardInterface_PricePaths_MatchEntsoeConvention()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.PriceCurrentPrice, Does.EndWith("/Price/CurrentPrice"));
            Assert.That(iso.PriceNextHour, Does.EndWith("/Price/NextHour"));
            Assert.That(iso.PriceLevel, Does.EndWith("/Price/Level"));
            Assert.That(iso.PricePriceCategory, Does.EndWith("/Price/PriceCategory"));
            Assert.That(iso.PriceCheapThreshold, Does.EndWith("/Price/CheapThreshold"));
            Assert.That(iso.PriceExpensiveThreshold, Does.EndWith("/Price/ExpensiveThreshold"));
            Assert.That(iso.PriceCurrency, Does.EndWith("/Price/Currency"));
            Assert.That(iso.PriceUnit, Does.EndWith("/Price/Unit"));
        }

        [Test]
        public void StandardInterface_MetaPaths_MatchEntsoeConvention()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.MetaLastUpdate, Does.EndWith("/Meta/LastUpdate"));
            Assert.That(iso.MetaDataHealthy, Does.EndWith("/Meta/DataHealthy"));
            Assert.That(iso.MetaTimeZone, Does.EndWith("/Meta/TimeZone"));
        }

        [Test]
        public void StandardInterface_LoadShiftingPaths_MatchEntsoeConvention()
        {
            var iso = EwsStructure.ForIso("CAISO");
            Assert.That(iso.ShiftIsCurrentlyCheap,
                Does.EndWith("/LoadShifting/IsCurrentlyCheap"));
            Assert.That(iso.ShiftIsCurrentlyExpensive,
                Does.EndWith("/LoadShifting/IsCurrentlyExpensive"));
            Assert.That(iso.ShiftRenewablePercent,
                Does.EndWith("/LoadShifting/RenewablePercent"));
        }
    }
}
