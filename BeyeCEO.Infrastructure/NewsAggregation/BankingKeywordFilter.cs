using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BeyeCEO.Infrastructure.NewsAggregation
{
    public enum KeywordVerdict
    {
        PassedStrong,
        PassedWeak,
        PassedBankName,
        PassedOfficialTopic,
        Rejected
    }

    public class KeywordCheckResult
    {
        public KeywordVerdict Verdict { get; set; }
        public bool IsOfficial { get; set; }
        public string? MatchedOn { get; set; }

        // True only when a negative phrase (West Bank, blood bank, ...)
        // explicitly cancelled the match — distinct from an ordinary
        // "nothing matched" or "only one weak keyword" rejection, even
        // though both leave MatchedOn non-null for diagnostics.
        public bool IsNegativePhrase { get; set; }

        // True only when an official-feed item was disqualified by an
        // administrative-noise title pattern ("Appointment of", "Market
        // Notice", ...) rather than simply not matching anything.
        public bool IsAdminNoise { get; set; }

        // Short label for the "rule" column in diagnostics/reports.
        public string RuleLabel => Verdict switch
        {
            KeywordVerdict.PassedStrong => "strong keyword",
            KeywordVerdict.PassedWeak => "weak keyword x2",
            KeywordVerdict.PassedBankName => "bank name",
            KeywordVerdict.PassedOfficialTopic => "official topic",
            _ when IsNegativePhrase => "negative phrase",
            _ when IsAdminNoise => "administrative noise",
            _ => "no match"
        };
    }

    // Cheap pre-filter run before the (future) AI judging step. Keeps the last
    // 200 rejected titles around so a human can sanity-check what's being thrown away.
    public class BankingKeywordFilter
    {
        private const int MaxRejectedHistory = 200;
        private readonly ConcurrentQueue<string> _rejectedTitles = new();

        // One strong keyword is enough. Multi-word phrases are matched as a
        // whole phrase. "bank" alone is deliberately included even though it
        // makes an article like "Ministry transfers teaching payments to
        // banks" pass despite not really being banking news — see
        // BankingKeywordFilterTests for that documented, accepted trade-off.
        private static readonly string[] StrongEnglish =
        [
            "bank", "banks", "banking", "banker", "bankers", "bancassurance", "central bank",
            "lender", "lenders", "loan", "loans", "mortgage", "deposit", "deposits",
            "interest rate", "interest rates", "monetary policy", "bank rate", "basel",
            "capital adequacy", "non-performing", "npl", "stress test", "resolution plan", "fomc",
            "federal funds rate", "quantitative easing", "deposit insurance", "fdic", "occ",
            "stablecoin issuer", "aml", "anti-money laundering"
        ];

        // International only, stem-matched (so plural "charters" counts too)
        // — deliberately NOT a bare "charter": "charter flights" must never
        // match, only when preceded by trust/bank/occ.
        private static readonly string[] CharterStemsInternational =
            ["trust charter", "bank charter", "occ charter"];

        private static readonly string[] StrongArabicRaw =
        [
            "بنك", "بنوك", "مصرف", "مصارف", "مصرفي", "البنك المركزي", "قرض", "قروض",
            "ودائع", "وديعة", "سعر الفائدة", "أسعار الفائدة", "السياسة النقدية", "بازل",
            "الجهاز المصرفي", "القطاع المصرفي", "رأس المال المصرفي", "البنوك الإسلامية"
        ];

        // Never enough alone — need >= 2 DISTINCT weak keywords, or one
        // combined with something stronger.
        private static readonly string[] WeakEnglish =
            ["payments", "payment", "fintech", "credit", "funding", "liquidity"];

        private static readonly string[] WeakArabicRaw =
            ["تمويل", "سيولة", "ائتمان", "دفع", "دفعات"];

        private static readonly string[] NegativePhrasesEn =
        [
            "west bank", "blood bank", "food bank", "river bank", "bank holiday",
            "jordan banks", "memory bank",
            "on loan", "loan move", "loan deal", "loan spell", "loaned out",
            "food banks", "foodbanks", "sperm bank", "piggy bank",
            "biobank", "bio bank", "uk biobank"
        ];

        private static readonly string[] NegativePhrasesArRaw =
        [
            "الضفة الغربية", "بنك الدم", "بنك الطعام", "بنوك الطعام", "بنوك الغذاء",
            "البنك الحيوي"
        ];

        // Stripped from the text before any strong/weak/bank-name matching
        // (but NOT before negative-phrase checking) — "World Bank" and "IMF"
        // mentions must not trigger the generic "bank" keyword by themselves.
        private static readonly string[] NeutralInstitutionsEn =
            ["world bank", "international monetary fund", "imf"];

        private static readonly string[] NeutralInstitutionsArRaw =
            ["صندوق النقد الدولي", "صندوق النقد", "بنك الدولي"];

        private static readonly string[] NeutralInstitutionsAr = NormalizeAll(NeutralInstitutionsArRaw);

        // Catches "البنك الدولي" and grammatical variants like the dual
        // "البنك الدوليين" (e.g. "صندوق النقد والبنك الدوليين" = "the IMF and
        // the [two] international [funds]", a shared-adjective construction
        // where "الدولي" is inflected to agree with both nouns).
        private static readonly Regex WorldBankArRegex = new(@"البنك\s*الدولي\S*", RegexOptions.Compiled);

        // Scope = International only (see BankingKeywordFilter.Check isInternational param).
        private static readonly string[] GlobalBankNames =
        [
            "JPMorgan", "JP Morgan", "Goldman Sachs", "Morgan Stanley", "Citigroup", "Citibank",
            "Bank of America", "BofA", "Wells Fargo", "HSBC", "Barclays", "Standard Chartered",
            "UBS", "Credit Suisse", "Deutsche Bank", "BNP Paribas", "Societe Generale", "Santander",
            "BBVA", "ING", "Lloyds", "NatWest", "Mitsubishi UFJ", "Mizuho", "Sumitomo Mitsui",
            "ICBC", "Revolut", "Monzo", "Wise"
        ];

        // Central banks — International scope only, same as GlobalBankNames.
        // "Powell" is handled separately: it only counts when "Fed"/"Federal
        // Reserve" also appears (see MatchesPowellWithFedContext).
        private static readonly string[] CentralBankNames =
        [
            "Federal Reserve", "Fed chair", "Federal Open Market Committee",
            "European Central Bank", "ECB", "Bank of England", "Bank of Japan",
            "People's Bank of China"
        ];

        // Scope = Local/JO only, supplementing config.Banks lookups with the
        // short/brand forms actually used in headlines.
        private static readonly (string En, string Ar)[] JoBankShortForms =
        [
            ("Arab Bank", "البنك العربي"),
            ("Housing Bank", "بنك الإسكان"),
            ("Jordan Kuwait Bank", "البنك الأردني الكويتي"),
            ("Cairo Amman Bank", "بنك القاهرة عمان"),
            ("Capital Bank", "كابيتال بنك"),
            ("Bank al Etihad", "بنك الاتحاد"),
            ("Jordan Ahli Bank", "البنك الأهلي الأردني"),
            ("ABC Bank", "بنك المؤسسة العربية المصرفية"),
            ("Safwa Islamic Bank", "بنك صفوة الإسلامي"),
            ("Jordan Islamic Bank", "البنك الإسلامي الأردني"),
            ("Islamic International Arab Bank", "البنك العربي الإسلامي الدولي"),
            ("CBJ", "البنك المركزي الأردني"),
            ("Central Bank of Jordan", "البنك المركزي الأردني"),
            ("Association of Banks in Jordan", "جمعية البنوك في الأردن")
        ];

        // Official (BoE/BIS/Fed/ECB) items that don't already pass the normal
        // strong/weak/bank-name rule can still pass on topic alone. These are
        // STEMS matched at word-start (e.g. "regulat" matches "regulation",
        // "regulatory", "regulators" but not "deregulation") — see
        // ContainsStemEn. The two full phrases are matched the same way,
        // which is equivalent to a plain substring check for them.
        private static readonly string[] OfficialTopicStems =
        [
            "regulat", "supervis", "financial stabil", "monetary", "inflation", "stress test",
            "resolution", "stablecoin", "payment system", "economic projection",
            "financial market infrastructure", "federal open market committee",
            "open market committee", "exchange of letters"
        ];

        // Administrative noise that disqualifies an official item even if it
        // would otherwise pass on topic.
        private static readonly string[] NoisePatterns =
        [
            "appointment of", "market notice", "minutes of the uk money markets",
            "minutes of the market participants", "minutes of the transaction and post-trade",
            "court of directors", "foreign currency reserves", "transcript of", "dates for 20",
            "new office", "discount rate meeting minutes", "enforcement decision making",
            "taskforce", "sub-committee", "fxjsc", "banknote", "banknotes",
            "foreign exchange joint standing committee"
        ];

        private static readonly string[] StrongArabic = NormalizeAll(StrongArabicRaw);
        private static readonly string[] WeakArabic = NormalizeAll(WeakArabicRaw);
        private static readonly string[] NegativePhrasesAr = NormalizeAll(NegativePhrasesArRaw);

        private static string[] NormalizeAll(string[] values) =>
            values.Select(NormalizeArabic).ToArray();

        public static string NormalizeArabic(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                // Strip tashkeel (combining diacritics) and the superscript alef.
                if (ch is >= 'ً' and <= 'ٟ') continue;
                if (ch == 'ٰ') continue;
                // Strip tatweel (kashida).
                if (ch == 'ـ') continue;

                sb.Append(ch switch
                {
                    'أ' or 'إ' or 'آ' or 'ٱ' => 'ا', // alef variants
                    'ى' => 'ي',                      // alef maqsura -> ya
                    'ة' => 'ه',                      // ta marbuta -> ha
                    _ => ch
                });
            }

            return sb.ToString();
        }

        private static bool ContainsWordEn(string lowerText, string keyword) =>
            Regex.IsMatch(lowerText, $@"\b{Regex.Escape(keyword.ToLowerInvariant())}\b");

        // Word-START match only (no trailing \b) — matches a stem as the
        // beginning of a word, so "regulat" matches "regulation",
        // "regulatory", "regulators" but not "deregulation" (no boundary
        // right before "regulat" in the middle of that word).
        private static bool ContainsStemEn(string lowerText, string stem) =>
            Regex.IsMatch(lowerText, $@"\b{Regex.Escape(stem.ToLowerInvariant())}");

        private static string? FirstMatchEn(string lowerText, IEnumerable<string> keywords) =>
            keywords.FirstOrDefault(k => ContainsWordEn(lowerText, k));

        private static string? FirstStemMatchEn(string lowerText, IEnumerable<string> stems) =>
            stems.FirstOrDefault(s => ContainsStemEn(lowerText, s));

        private static List<string> AllMatchesEn(string lowerText, IEnumerable<string> keywords) =>
            keywords.Where(k => ContainsWordEn(lowerText, k)).ToList();

        // ── Arabic token-boundary matching ───────────────────────────────
        // Arabic has no spaces around clitic prefixes/suffixes (و، ال، ها،
        // ...), so a plain substring Contains() check lets a keyword match
        // inside an unrelated longer word — e.g. "بنك" ("bank") matching
        // inside "بنكرياس" ("pancreas"). Instead, match on token boundaries
        // over the NORMALIZED text: a keyword matches only when it isn't
        // preceded/followed by another Arabic letter, except for a small
        // set of allowed clitic prefixes/suffixes attached directly to it.
        private static readonly string[] ArabicPrefixes =
            ["وال", "بال", "فال", "كال", "لل", "ال", "و", "ف", "ب", "ل", "ك"];

        // Suffixes in NORMALIZED form — NormalizeArabic maps ة→ه and ى→ي,
        // so a keyword ending in either must list the normalized form here.
        private static readonly string[] ArabicSuffixes =
            ["ها", "هم", "ات", "ان", "ين", "ون", "ي", "ه"];

        // Bare "ا" (accusative/indefinite tanwin, left behind once the
        // combining tanwin mark itself is stripped by NormalizeArabic) is
        // ONLY allowed as a suffix for these specific strong banking roots
        // (3+ letters) — e.g. "بنوكاً" -> normalized "بنوكا" -> بنوك+ا. Not
        // extended to weak keywords or short tokens, where "ا" would be far
        // too permissive (Step 9b/9d).
        private static readonly HashSet<string> TanwinAlefEligibleAr = new(StringComparer.Ordinal)
        {
            "بنك", "بنوك", "مصرف", "مصارف", "قرض", "قروض"
        };

        private static readonly string ArabicPrefixAlternation =
            string.Join("|", ArabicPrefixes.Select(Regex.Escape));

        private static readonly string ArabicSuffixAlternation =
            string.Join("|", ArabicSuffixes.Select(Regex.Escape));

        private static readonly ConcurrentDictionary<string, Regex> ArabicTokenRegexCache = new();

        private static Regex GetArabicTokenRegex(string keyword) =>
            ArabicTokenRegexCache.GetOrAdd(keyword, k =>
            {
                var suffixAlternation = TanwinAlefEligibleAr.Contains(k)
                    ? ArabicSuffixAlternation + "|" + Regex.Escape("ا")
                    : ArabicSuffixAlternation;

                return new Regex(
                    $@"(?<!\p{{L}})(?:{ArabicPrefixAlternation})?{Regex.Escape(k)}(?:{suffixAlternation})?(?!\p{{L}})",
                    RegexOptions.Compiled);
            });

        private static bool ContainsArabicToken(string normalizedText, string keyword) =>
            GetArabicTokenRegex(keyword).IsMatch(normalizedText);

        private static string? FirstMatchAr(string normalizedText, IEnumerable<string> keywords) =>
            keywords.FirstOrDefault(k => ContainsArabicToken(normalizedText, k));

        private static List<string> AllMatchesAr(string normalizedText, IEnumerable<string> keywords) =>
            keywords.Where(k => ContainsArabicToken(normalizedText, k)).ToList();

        private static bool MatchesPowellWithFedContext(string lowerText) =>
            ContainsWordEn(lowerText, "powell") &&
            (ContainsWordEn(lowerText, "fed") || lowerText.Contains("federal reserve", StringComparison.Ordinal));

        // Removes "World Bank"/"IMF" mentions (always) and, for official
        // feeds, the feed's own institution name (so e.g. the Bank of
        // England feed doesn't trivially pass every item just by saying
        // "Bank of England") — applied before strong/weak/bank-name
        // matching only; negative-phrase checking still sees the original text.
        private static string StripNeutralAndOwnNameEn(string lowerEn, IEnumerable<string>? ownNameExclusions)
        {
            var result = lowerEn;
            foreach (var phrase in NeutralInstitutionsEn.OrderByDescending(p => p.Length))
                result = result.Replace(phrase, " ", StringComparison.Ordinal);

            if (ownNameExclusions != null)
                foreach (var phrase in ownNameExclusions.OrderByDescending(p => p.Length))
                    result = result.Replace(phrase.ToLowerInvariant(), " ", StringComparison.Ordinal);

            return result;
        }

        private static string StripNeutralAr(string normalizedAr)
        {
            var result = WorldBankArRegex.Replace(normalizedAr, " ");
            foreach (var phrase in NeutralInstitutionsAr)
                result = result.Replace(phrase, " ", StringComparison.Ordinal);
            return result;
        }

        public KeywordCheckResult Check(
            string? title,
            string? summary,
            bool isOfficialFeed,
            IEnumerable<(string NameEN, string NameAR)>? localBankNames = null,
            bool isInternational = false,
            string? countryCode = null,
            IEnumerable<string>? ownNameExclusions = null)
        {
            title ??= string.Empty;
            summary ??= string.Empty;
            var combined = $"{title} {summary}";
            var lowerEn = combined.ToLowerInvariant();
            var normalizedAr = NormalizeArabic(combined);

            // Negative phrases cancel everything, regardless of feed type —
            // checked against the ORIGINAL text, before any stripping.
            var negEn = FirstMatchEn(lowerEn, NegativePhrasesEn);
            if (negEn != null)
                return Reject(title, negEn);

            var negAr = FirstMatchAr(normalizedAr, NegativePhrasesAr);
            if (negAr != null)
                return Reject(title, negAr);

            var core = CheckCoreRule(title, summary, localBankNames, isInternational, countryCode, ownNameExclusions);

            if (!isOfficialFeed)
            {
                if (core.Verdict != KeywordVerdict.Rejected) return core;
                RecordRejected(title);
                return core;
            }

            // Official feed: strong/weak/bank-name OR an official topic — but
            // administrative noise disqualifies either path.
            var isNoisy = NoisePatterns.Any(p => lowerEn.Contains(p, StringComparison.Ordinal));

            if (core.Verdict != KeywordVerdict.Rejected)
            {
                if (isNoisy)
                {
                    RecordRejected(title);
                    return new KeywordCheckResult
                    {
                        Verdict = KeywordVerdict.Rejected,
                        MatchedOn = core.MatchedOn,
                        IsAdminNoise = true
                    };
                }
                core.IsOfficial = true;
                return core;
            }

            var topic = FirstStemMatchEn(lowerEn, OfficialTopicStems);
            if (topic != null && !isNoisy)
            {
                return new KeywordCheckResult
                {
                    Verdict = KeywordVerdict.PassedOfficialTopic,
                    IsOfficial = true,
                    MatchedOn = topic
                };
            }

            RecordRejected(title);
            return new KeywordCheckResult
            {
                Verdict = KeywordVerdict.Rejected,
                MatchedOn = core.MatchedOn,
                IsAdminNoise = topic != null && isNoisy
            };
        }

        // Category matches for one piece of text (title or summary alone).
        private readonly record struct CategoryMatches(
            List<string> BankNames, List<string> Strong, List<string> Weak)
        {
            public bool HasDecisive => BankNames.Count > 0 || Strong.Count > 0 || Weak.Count >= 2;
        }

        private static CategoryMatches CollectMatches(
            string text,
            IEnumerable<(string NameEN, string NameAR)>? localBankNames,
            bool isInternational,
            string? countryCode,
            IEnumerable<string>? ownNameExclusions = null)
        {
            var lowerEn = StripNeutralAndOwnNameEn(text.ToLowerInvariant(), ownNameExclusions);
            var normalizedAr = StripNeutralAr(NormalizeArabic(text));

            var bankNames = new List<string>();

            if (localBankNames != null)
            {
                foreach (var (nameEn, nameAr) in localBankNames)
                {
                    if (!string.IsNullOrWhiteSpace(nameEn) && ContainsWordEn(lowerEn, nameEn))
                        bankNames.Add(nameEn);
                    if (!string.IsNullOrWhiteSpace(nameAr) &&
                        ContainsArabicToken(normalizedAr, NormalizeArabic(nameAr)))
                        bankNames.Add(nameAr);
                }
            }

            if (string.Equals(countryCode, "JO", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var (en, ar) in JoBankShortForms)
                {
                    if (ContainsWordEn(lowerEn, en)) bankNames.Add(en);
                    if (ContainsArabicToken(normalizedAr, NormalizeArabic(ar))) bankNames.Add(ar);
                }
            }

            if (isInternational)
            {
                bankNames.AddRange(GlobalBankNames.Where(n => ContainsWordEn(lowerEn, n)));
                bankNames.AddRange(CentralBankNames.Where(n => ContainsWordEn(lowerEn, n)));
                if (MatchesPowellWithFedContext(lowerEn)) bankNames.Add("Powell");
            }

            var strong = AllMatchesEn(lowerEn, StrongEnglish)
                .Concat(AllMatchesAr(normalizedAr, StrongArabic))
                .ToList();

            if (isInternational)
                strong.AddRange(CharterStemsInternational.Where(s => ContainsStemEn(lowerEn, s)));

            strong = strong.Distinct().ToList();

            var weak = AllMatchesEn(lowerEn, WeakEnglish)
                .Concat(AllMatchesAr(normalizedAr, WeakArabic))
                .Distinct()
                .ToList();

            return new CategoryMatches(bankNames.Distinct().ToList(), strong, weak);
        }

        // Title is checked first and wins if it has any decisive match on its
        // own (bank name, strong keyword, or 2+ distinct weak keywords) —
        // "Banks prioritize responsible AI..." (strong, in the title) should
        // report as a strong-keyword match even if "JPMorgan" (a bank name)
        // only appears in the summary. MatchedOn always reports every term
        // found across the full title+summary text, strong keywords first.
        private KeywordCheckResult CheckCoreRule(
            string title, string summary,
            IEnumerable<(string NameEN, string NameAR)>? localBankNames,
            bool isInternational, string? countryCode,
            IEnumerable<string>? ownNameExclusions)
        {
            var titleMatches = CollectMatches(title, localBankNames, isInternational, countryCode, ownNameExclusions);
            var combinedMatches = CollectMatches(
                $"{title} {summary}", localBankNames, isInternational, countryCode, ownNameExclusions);

            string JoinAll(CategoryMatches m) =>
                string.Join(" + ", m.Strong.Concat(m.BankNames).Concat(m.Weak).Distinct());

            if (titleMatches.BankNames.Count > 0 || titleMatches.Strong.Count > 0)
            {
                // Decide the category from the title alone (bank name is the
                // strongest per-field signal), but report every term found
                // anywhere in title+summary, strong first.
                var verdict = titleMatches.BankNames.Count > 0
                    ? KeywordVerdict.PassedBankName
                    : KeywordVerdict.PassedStrong;

                return new KeywordCheckResult { Verdict = verdict, MatchedOn = JoinAll(combinedMatches) };
            }

            if (titleMatches.Weak.Count >= 2)
                return new KeywordCheckResult
                {
                    Verdict = KeywordVerdict.PassedWeak,
                    MatchedOn = string.Join(" + ", titleMatches.Weak)
                };

            // Title alone had nothing decisive — fall back to the combined
            // text (catches summary-only matches and weak pairs split across
            // title and summary).
            if (combinedMatches.BankNames.Count > 0)
                return new KeywordCheckResult { Verdict = KeywordVerdict.PassedBankName, MatchedOn = JoinAll(combinedMatches) };

            if (combinedMatches.Strong.Count > 0)
                return new KeywordCheckResult { Verdict = KeywordVerdict.PassedStrong, MatchedOn = JoinAll(combinedMatches) };

            if (combinedMatches.Weak.Count >= 2)
                return new KeywordCheckResult
                {
                    Verdict = KeywordVerdict.PassedWeak,
                    MatchedOn = string.Join(" + ", combinedMatches.Weak)
                };

            return new KeywordCheckResult
            {
                Verdict = KeywordVerdict.Rejected,
                MatchedOn = combinedMatches.Weak.FirstOrDefault()
            };
        }

        private KeywordCheckResult Reject(string? title, string matchedPhrase)
        {
            RecordRejected(title);
            return new KeywordCheckResult
            {
                Verdict = KeywordVerdict.Rejected,
                MatchedOn = matchedPhrase,
                IsNegativePhrase = true
            };
        }

        private void RecordRejected(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;

            _rejectedTitles.Enqueue(title);
            while (_rejectedTitles.Count > MaxRejectedHistory)
                _rejectedTitles.TryDequeue(out _);
        }

        public IReadOnlyList<string> GetRecentRejectedTitles() => _rejectedTitles.ToArray();
    }
}
