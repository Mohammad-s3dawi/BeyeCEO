using BeyeCEO.Infrastructure.NewsAggregation;
using System.Collections.Generic;

namespace BeyeCEO.Tests
{
    public class BankingKeywordFilterTests
    {
        private static readonly List<(string NameEN, string NameAR)> FabricatedLocalBanks = new()
        {
            ("Zenith Capital Co", "زينث كابيتال")
        };

        [Fact]
        public void NormalizeArabic_StripsDiacriticsAndTatweelAndNormalizesLetterVariants()
        {
            var input = "البَنْكُ المركزيّ الأردنـــي يحذّر مِن قُروضٍ وَهميّة";

            var normalized = BankingKeywordFilter.NormalizeArabic(input);

            Assert.DoesNotContain('ً', normalized);
            Assert.DoesNotContain('ّ', normalized);
            Assert.DoesNotContain('ـ', normalized);
            Assert.Contains("البنك المركزي الاردني", normalized);
            Assert.Contains("قروض", normalized);
        }

        [Fact]
        public void NormalizeArabic_AlefMaqsuraAndTaMarbuta_AreNormalized()
        {
            Assert.Equal("ي", BankingKeywordFilter.NormalizeArabic("ى"));
            Assert.Equal("ه", BankingKeywordFilter.NormalizeArabic("ة"));
            Assert.Equal("ا", BankingKeywordFilter.NormalizeArabic("أ"));
        }

        // ── Negative phrases ────────────────────────────────────────────

        [Theory]
        [InlineData("Settlers attack farmers in the West Bank")]
        [InlineData("Local food bank sees record donations this winter")]
        [InlineData("From penguins to polar bears, Jordan Banks captures nature")]
        [InlineData("Banks announce a bank holiday for the upcoming festival")]
        public void Check_NegativePhrase_Rejects(string title)
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(title, "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        [Fact]
        public void Check_ArabicNegativePhrase_WestBank_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("مواجهات في الضفة الغربية", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        // ── New English negative/context phrases (Step 9b) ───────────────
        // Real rows from the old-rows dataset — sports "on loan" transfer
        // stories and a food-bank welfare story were previously false
        // positives via the bare "loan"/"banks" strong keywords.

        [Fact]
        public void Check_LoanMoveNegativePhrase_RealTitle_JorgensenStrasbourg_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Keeper Jorgensen set for loan move to Strasbourg",
                "Chelsea goalkeeper Filip Jorgensen is set to seal a season-long loan to Strasbourg, " +
                "part of the same ownership group as the London club.",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        [Fact]
        public void Check_OnLoanNegativePhrase_RealTitle_CelticGossip_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Celtic wait for midfielder Sigur - gossip",
                "Celtic advance in search of a midfielder and goalkeeper as Dundee aim to secure striker on loan...",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        [Fact]
        public void Check_OnLoanNegativePhrase_RealTitle_GarnachoVilla_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Is Garnacho already at a crossroads at Villa?",
                "Alejandro Garnacho joined Aston Villa on loan from Chelsea with a view to kick-starting " +
                "his career, but his early weeks in the Midlands have been a struggle.",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        [Fact]
        public void Check_FoodBanksNegativePhrase_RealTitle_HousingBenefit_RejectsDespiteGenuineBanksKeyword()
        {
            // "banks" (strong keyword) is also present here, via "food
            // banks" — the negative phrase correctly wins: this is a
            // social-welfare story, not banking news. Documented trade-off,
            // see the Step 9b report for the explicit note on this row.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Pay rent, eat or keep warm? Growing numbers face hard choices as housing benefit gap grows",
                "Wirral mother among renters turning to food banks as gap between a two-bed and housing " +
                "benefit hits £158 a month – rising to as much as £324 in London",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        // ── Word-boundary handling ──────────────────────────────────────

        [Fact]
        public void Check_Bankruptcy_DoesNotMatchBank_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Bankruptcy filed by the regional retailer", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        // ── "banker"/"bankers" as strong keywords (Step 9b) ──────────────

        [Fact]
        public void Check_BankersStrongKeyword_RealTitle_InvestmentBankers_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "London's investment bankers and lawyers make more than £1bn in takeover frenzy", "",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_Bankruptcy_StillDoesNotMatch_AfterAddingBanker()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Bankruptcy filed by the regional retailer", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_BankersAssociation_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Bankers Association issues new guidance", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Theory]
        [InlineData("The banks announced new rules")]
        [InlineData("New banking regulations take effect today")]
        public void Check_BanksAndBanking_DoMatchAsWholeWords(string title)
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(title, "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        // ── Arabic token-boundary matching (Step 9b) ─────────────────────
        // Plain substring Contains() let "بنك" match inside unrelated
        // longer words (no spaces around Arabic clitic prefixes/suffixes).
        // Fixed with a regex requiring a non-letter boundary, except for a
        // small set of allowed prefixes/suffixes attached to the keyword.

        [Fact]
        public void Check_ArabicBank_DoesNotMatchInsidePancreas()
        {
            // "بنكرياس" (pancreas) — synthetic example illustrating the
            // general boundary fix (see also the real-row correction test
            // below: the actual Step 9a false positive had a different,
            // non-bug cause).
            var filter = new BankingKeywordFilter();

            var result = filter.Check("التهاب البنكرياس يسبب أعراضا خطيرة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_ArabicBank_WithBaPrefix_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("ببنك الإسكان افتتاح فرع جديد", "", isOfficialFeed: false, countryCode: "JO");

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_ArabicBanksPlural_WithWaAlPrefix_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("والبنوك تستعد لإجراءات جديدة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicMasrafBank_DoesNotMatchInsideMasroofat()
        {
            // "مصروفات" (expenses) does not contain "مصرف" as a contiguous
            // substring at all (extra و), so this never matched — kept as a
            // regression guard.
            var filter = new BankingKeywordFilter();

            var result = filter.Check("جدول المصروفات الشهرية للأسرة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_ArabicMasarifPlural_WithAlPrefix_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("المصارف تعلن عن نتائجها السنوية", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicMasrafi_WithAlPrefixNoSuffix_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("العمل المصرفي يشهد نموا ملحوظا", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicWeakWord_StillMatchesWithAlPrefix()
        {
            // "تمويل" (weak) must still be recognized once wrapped with the
            // "ال" prefix — combined with another distinct weak word so the
            // >= 2 weak-keyword rule can fire and prove the match occurred.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "مشروع صغير يبحث عن التمويل", "ويحتاج إلى الائتمان لتغطية السيولة", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedWeak, result.Verdict);
        }

        [Fact]
        public void Check_RealRow_CentralBankOfJordanPhrase_Passes()
        {
            // Real jo24.net row (2026-09-07) — confirms "البنك المركزي
            // الأردني" still matches correctly after the boundary rewrite.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "المركزي يحذر من مسابقات وهمية تستهدف بيانات المواطنين",
                "حذّر البنك المركزي الأردني من محاولات احتيال إلكتروني قد تبدأ برسائل تدّعي فوز المستلم بجائزة، " +
                "وتستهدف الحصول على بياناته الشخصية أو المالية.",
                isOfficialFeed: false, countryCode: "JO");

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
            Assert.Contains("البنك المركزي الأردني", result.MatchedOn!);
        }

        [Fact]
        public void Check_RealRow_UkBiobank_NowRejected_ViaStep9dNegativePhrase()
        {
            // Step 9b left this one as a correctly-bounded "بنك" match (via
            // "البنك الحيوي" = "the biobank") since it's not a tokenization
            // bug. Step 9d adds "البنك الحيوي"/"biobank" as an explicit
            // negative phrase instead, since a biobank genuinely isn't
            // banking news. Real jo24.net row.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "دراسة تحذر: كل ساعة إضافية من الجلوس المتواصل قد تزيد خطر السرطان",
                "وحلل الباحثون بيانات أكثر من 91 ألف شخص شاركوا في دراسة البنك الحيوي البريطاني",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        [Fact]
        public void Check_UkBiobank_English_NegativePhrase_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "UK Biobank study links sitting time to cancer risk", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.True(result.IsNegativePhrase);
        }

        // ── Arabic tanwin-alef suffix for strong banking roots (Step 9d) ──

        [Fact]
        public void Check_ArabicBanksPlural_WithTanwinAlef_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("بنوكاً مركزية حول العالم", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicBank_WithTanwinAlef_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("افتتح بنكاً جديدا في العاصمة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicLoan_WithTanwinAlef_Matches()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("حصل على قرضاً لشراء منزل", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_RealRow_UsdRateHike_CentralBanksTanwinAlef_NowPasses()
        {
            // The Step 9b regression row: its only banking signal is
            // "بنوكا مركزية" (بنوك + bare tanwin alef). Now fixed.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "USD Under Pressure Despite Increasing US Rate Hike Expectations",
                "ظل الدولار تحت الضغط، الاثنين، رغم زيادة توقعات رفع الفائدة الأميركية إذ عزز التوتر في الشرق " +
                "الأوسط احتمالات اتساع الضغوط التضخمية، مما قد يدفع بنوكا مركزية حول العالم إلى تشديد سياساتها النقدية.",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_ArabicBank_DoesNotMatchInsidePancreas_EvenWithTanwinAlefRule()
        {
            // Regression guard: adding the "ا" suffix for "بنك" must not
            // reopen the "بنكرياس" false positive — the "ر" right after
            // "بنك" still blocks the match regardless of what suffix set
            // is tried.
            var filter = new BankingKeywordFilter();

            var result = filter.Check("التهاب البنكرياس يسبب أعراضا خطيرة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_ArabicMasraf_DoesNotMatchInsideMasroofat_EvenWithTanwinAlefRule()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("جدول المصروفات الشهرية للأسرة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        // ── Strong / weak keyword rule ──────────────────────────────────

        [Fact]
        public void Check_KnownLimitation_TeachingPaymentsToBanks_PassesViaBanksWholeWord()
        {
            // "Ministry transfers supplementary teaching payments to banks" is not
            // really banking news — it's a government payroll story that happens to
            // end in the word "banks". This is an accepted precision trade-off of a
            // simple keyword filter; documenting it here rather than special-casing it.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Ministry transfers supplementary teaching payments to banks", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
            // "payments" (weak) is also present in this title — Step 7.1
            // reports every matched term, strong first.
            Assert.Equal("banks + payments", result.MatchedOn);
        }

        [Fact]
        public void Check_ArabicWeakWordAlone_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("مشروع إقليمي للسيليكا", "يحتاج إلى تمويل إضافي", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_ArabicTwoDistinctWeakWords_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("مشروع يحتاج تمويل", "ويعتمد على الائتمان لتغطية السيولة", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedWeak, result.Verdict);
        }

        [Fact]
        public void Check_ArabicCentralBankPhrase_PassesStrong()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("البنك المركزي يرفع سعر الفائدة", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_PersonioFintechOnly_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Personio acquires spend management platform Circula",
                "The fintech deal expands Personio's HR offering.",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_FiservPaymentsOnly_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Why Fiserv pivoted on FIUSD",
                "The move reshapes Fiserv's payments strategy.",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_PlaidCreditAndPayment_PassesWeak()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Plaid launches credit, fraud and payment risk AI models", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedWeak, result.Verdict);
        }

        // ── Bank names as strong positives ───────────────────────────────

        [Fact]
        public void Check_GlobalBankName_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Anchorage Digital expands global services with Standard Chartered", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_GlobalBankName_MorganStanley_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Morgan Stanley sets up digital asset lab", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_OccTrustCharter_PassesStrong()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Rain, Modern Treasury seek OCC trust charters", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
            Assert.Equal("occ", result.MatchedOn);
        }

        [Fact]
        public void Check_GlobalBankName_NotAppliedWhenNotInternational()
        {
            // Same headline, but the feed isn't flagged international — the
            // global bank-name list should not kick in, and there's no other
            // banking keyword here, so it should be rejected.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Morgan Stanley sets up digital asset lab", "",
                isOfficialFeed: false, isInternational: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_FabricatedLocalBankName_IsStrongPositiveSignal()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Zenith Capital Co reports record quarterly profits", "",
                isOfficialFeed: false, localBankNames: FabricatedLocalBanks);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Theory]
        [InlineData("Arab Bank reports strong Q3 results")]
        [InlineData("Housing Bank launches new digital app")]
        [InlineData("Bank al Etihad signs cooperation deal")]
        public void Check_JoBankShortForm_PassesForJoCountryCode(string title)
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(title, "", isOfficialFeed: false, countryCode: "JO");

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_JoBankShortForm_ArabicForm_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("بنك الاتحاد يفتتح فرعا جديدا", "", isOfficialFeed: false, countryCode: "JO");

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        // ── Official feeds ────────────────────────────────────────────

        [Fact]
        public void Check_Official_BankRateMonetaryPolicySummary_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Bank rate maintained at 3.75% - September 2026 Monetary Policy Summary and Minutes", "",
                isOfficialFeed: true);

            Assert.True(result.Verdict is KeywordVerdict.PassedStrong or KeywordVerdict.PassedOfficialTopic);
            Assert.True(result.IsOfficial);
        }

        [Fact]
        public void Check_Official_AppointmentNoise_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Appointment of members of the Enforcement Decision Making Committee (EDMC)", "",
                isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
            Assert.False(result.IsOfficial);
        }

        [Fact]
        public void Check_Official_ForeignCurrencyReservesMarketNotice_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Foreign Currency Reserves 2026 - Market Notice 22 September 2026", "",
                isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_Official_FomcStatement_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("FOMC statement", "", isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
            Assert.True(result.IsOfficial);
        }

        [Fact]
        public void Check_Official_UkMoneyMarketsSubCommitteeMinutes_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Minutes of the UK Money Markets Code Sub-Committee – September 2026", "",
                isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_Official_NoiseOverridesAnOtherwisePassingTopicMatch()
        {
            // Contains "liquidity" (an official topic word) but is still an
            // administrative "Market Notice" — noise must win.
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Foreign Currency Reserves 2026 - Market Notice: liquidity update", "",
                isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        // ── Official-topic stem matching (Step 7.1) ──────────────────────

        [Fact]
        public void Check_Official_RegulatoryStem_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Regulatory thresholds set to shift to automatic increases", "", isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.PassedOfficialTopic, result.Verdict);
            Assert.True(result.IsOfficial);
        }

        [Fact]
        public void Check_Official_StemDoesNotMatchDeregulation()
        {
            // "regulat" must not match inside "deregulation" — no word
            // boundary immediately before that substring there.
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Deregulation sweeps across the sector", "", isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_Official_FederalOpenMarketCommitteePhrase_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Minutes of the Federal Open Market Committee, July 28–29, 2026", "", isOfficialFeed: true);

            Assert.True(result.Verdict is KeywordVerdict.PassedStrong or KeywordVerdict.PassedOfficialTopic);
            Assert.True(result.IsOfficial);
        }

        [Fact]
        public void Check_Official_DiscountRateMeetingMinutes_StillRejected()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Minutes of the Board's discount rate meetings on July 20 and July 29, 2026", "",
                isOfficialFeed: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        // ── Central bank names, international only (Step 7.1) ───────────

        [Fact]
        public void Check_PowellWithFederalReserveContext_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "DOJ won't reopen Powell probe",
                "The inquiry into Federal Reserve Chair Jerome Powell has been closed.",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_PowellWithoutFedContext_International_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "DOJ won't reopen Powell probe",
                "The investigation into the contractor has been closed.",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_EcbShortForm_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "ECB holds rates steady amid inflation concerns", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        [Fact]
        public void Check_FederalReserveMention_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Federal Reserve weighs policy options", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
        }

        // ── Title-preferred decisive rule + "all terms, strong first" ────

        [Fact]
        public void Check_StrongInTitleBankNameInSummary_TitleDecides()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Banks prioritize responsible AI, governance as use soars",
                "JPMorgan and others are leading the push.",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
            Assert.NotNull(result.MatchedOn);
            // Strong term listed before the bank name found in the summary.
            Assert.StartsWith("bank", result.MatchedOn!);
            Assert.Contains("JPMorgan", result.MatchedOn!);
        }

        [Fact]
        public void Check_BankNameOnlyInSummary_NoTitleMatch_FallsBackToSummary()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Industry event draws large crowd",
                "JPMorgan announced new hires at the conference.",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
            Assert.Equal("JPMorgan", result.MatchedOn);
        }

        // ── World Bank / IMF neutral (Step 7.2) ──────────────────────────

        [Fact]
        public void Check_ImfAndWorldBankSharedAdjective_Arabic_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "حرب الشرق الأوسط والديون تهيمنان على اجتماعات صندوق النقد والبنك الدوليين", "",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_WorldBankAlone_Arabic_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "البنك الدولي يجري محادثات مع عشرات الدول بشأن مساعدات الأزمات", "",
                isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_WorldBankPlusCentralBankOfJordan_Arabic_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "البنك الدولي والبنك المركزي الأردني يوقعان اتفاقية", "",
                isOfficialFeed: false, countryCode: "JO");

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
            Assert.Contains("البنك المركزي الأردني", result.MatchedOn!);
        }

        [Fact]
        public void Check_WorldBankPlusBnpParibas_English_StillPassesViaBankName()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "World Bank picks BNP Paribas exec for CFO", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedBankName, result.Verdict);
            Assert.Contains("BNP Paribas", result.MatchedOn!);
        }

        // ── Official feed own-name exclusion (Step 7.2) ──────────────────

        private static readonly string[] BoEOwnName = ["Bank of England"];
        private static readonly string[] FedOwnName = ["Federal Reserve Board", "Federal Reserve", "the Board"];

        [Fact]
        public void Check_Official_BoE_OwnNamePlusBanknotes_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Bank of England announces the four animals that will feature on our next series of banknotes",
                "", isOfficialFeed: true, ownNameExclusions: BoEOwnName);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_Official_FxjscNoise_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Minutes of the London FXJSC Main Committee Meeting – 24 June 2026",
                "", isOfficialFeed: true, ownNameExclusions: BoEOwnName);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_Official_FinancialPolicyCommitteeRecord_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Financial Policy Committee Record – September 2026",
                "The Bank of England's Financial Policy Committee assesses risks to financial stability.",
                isOfficialFeed: true, ownNameExclusions: BoEOwnName);

            Assert.Equal(KeywordVerdict.PassedOfficialTopic, result.Verdict);
        }

        [Fact]
        public void Check_Official_BankRateSummary_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Bank rate maintained at 3.75% - September 2026 Monetary Policy Summary and Minutes",
                "", isOfficialFeed: true, ownNameExclusions: BoEOwnName);

            Assert.True(result.Verdict is KeywordVerdict.PassedStrong or KeywordVerdict.PassedOfficialTopic);
        }

        [Fact]
        public void Check_Official_RegulatoryThresholds_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Regulatory thresholds set to shift to automatic increases",
                "", isOfficialFeed: true, ownNameExclusions: BoEOwnName);

            Assert.Equal(KeywordVerdict.PassedOfficialTopic, result.Verdict);
        }

        [Fact]
        public void Check_Official_Fed_FomcMinutes_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Minutes of the Federal Open Market Committee, September 15-16, 2026",
                "", isOfficialFeed: true, isInternational: true, ownNameExclusions: FedOwnName);

            Assert.True(result.Verdict is KeywordVerdict.PassedBankName or KeywordVerdict.PassedOfficialTopic);
        }

        [Fact]
        public void Check_Official_Fed_RegulationOTopic_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Federal Reserve Board announces it will extend, until November 4, the comment period " +
                "on its proposal to modernize Regulation O",
                "", isOfficialFeed: true, isInternational: true, ownNameExclusions: FedOwnName);

            Assert.Equal(KeywordVerdict.PassedOfficialTopic, result.Verdict);
            Assert.Equal("regulat", result.MatchedOn);
        }

        [Fact]
        public void Check_Official_Fed_FomcStatement_StillPasses()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Federal Reserve issues FOMC statement",
                "", isOfficialFeed: true, isInternational: true, ownNameExclusions: FedOwnName);

            Assert.True(result.Verdict is KeywordVerdict.PassedStrong or KeywordVerdict.PassedBankName);
            Assert.True(result.IsOfficial);
        }

        // ── "trust charter" stem, International only (Step 7.2) ──────────

        [Fact]
        public void Check_TrustChartersPlural_International_Passes()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "The elder statesman of trust charters makes its move", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.PassedStrong, result.Verdict);
        }

        [Fact]
        public void Check_BareCharterFlights_International_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check(
                "Airline adds new charter flights to the Caribbean", "",
                isOfficialFeed: false, isInternational: true);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        [Fact]
        public void Check_BareCharter_NotInternational_Rejects()
        {
            var filter = new BankingKeywordFilter();

            var result = filter.Check("Trust charters discussed at the summit", "", isOfficialFeed: false);

            Assert.Equal(KeywordVerdict.Rejected, result.Verdict);
        }

        // ── Diagnostics ─────────────────────────────────────────────────

        [Fact]
        public void RejectedHistory_IsCappedAt200()
        {
            var filter = new BankingKeywordFilter();

            for (var i = 0; i < 250; i++)
                filter.Check($"Irrelevant headline number {i}", "", isOfficialFeed: false);

            Assert.Equal(200, filter.GetRecentRejectedTitles().Count);
            Assert.Contains("Irrelevant headline number 249", filter.GetRecentRejectedTitles());
            Assert.DoesNotContain("Irrelevant headline number 0", filter.GetRecentRejectedTitles());
        }

        [Fact]
        public void RuleLabel_ReflectsVerdict()
        {
            var filter = new BankingKeywordFilter();

            Assert.Equal("strong keyword", filter.Check("Central bank raises rates", "", false).RuleLabel);
            Assert.Equal("no match", filter.Check("Weather turns colder", "", false).RuleLabel);
            Assert.Equal("negative phrase", filter.Check("West Bank settlers", "", false).RuleLabel);
        }
    }
}
