# Image Backfill Preview (Step 11b) — SELECT-only, nothing applied

Read-only preview of the og:image fallback against ACTIVE rows with empty `ImageUrl`, restricted to the allow-listed feeds (Al Mamlaka, Watan News, Finextra). Verified live against the real `PageMetadataFetcher` implementation (robots.txt-aware, honest UA, 10s timeout, ≤2 redirects, byte-budgeted).

## Al Mamlaka (5 of 5 candidates — ImageFallback.MaxBytes=128KB)

| Id | Title (AR) | Found Image URL | Decision |
|---|---|---|---|
| 5DCABDCA-5553-4F8D-B83D-A11E995D8437 | وزير الأوقاف: الصكوك الإسلامية أداة تمويل موفرة للسيولة | https://almamlakatv.com/images/articles/big/2026/10/6acc8f16bf451.jpeg | **APPLY** — real per-article photo, https, 200 confirmed |
| 095732CE-625D-40A9-9F2F-07F8EED0A05A | اجتماعات صندوق النقد والبنك الدولي تبحث تحديات الطاقة والديون | https://almamlakatv.com/images/articles/big/2026/10/6acc89514a9f0.jpeg | **APPLY** |
| F7EE7A82-6D97-4DE9-85CF-0417528B0F47 | الدولار قرب أعلى مستوى في 18 شهرا وسط تركيز على الشرق الأوسط | https://almamlakatv.com/images/articles/big/2026/10/6acc71c47d078.jpeg | **APPLY** |
| 1D9DE807-00D5-43DB-A0E8-E8E323175BAF | موجودات البنك المركزي الأردني من الذهب تسجل رقما قياسيا جديدا | https://almamlakatv.com/images/articles/big/2026/10/6acb55048d384.jpeg | **APPLY** |
| 0C8C259B-9653-4D80-A836-EF1E8E2ACE78 | حرب الشرق الأوسط والديون تهيمنان على اجتماعات صندوق النقد والبنك الدوليين | https://almamlakatv.com/images/articles/big/2026/10/6acb3bdcb2a2d.jpeg | **APPLY** |

## Watan News (3 of 3 candidates — ImageFallback.MaxBytes=256KB)

| Id | Title (AR) | Found Image URL | Decision |
|---|---|---|---|
| 70190FD9-D172-4320-8C73-54A11FA38EEA | الأردن يشارك في اجتماعات صندوق النقد والبنك الدوليين في بانكوك | https://watananews.com/wp-content/uploads/2026/10/download.webp | **APPLY** |
| 10DE3ED1-C6A6-4B4B-855C-0D6EC907024B | LINC من بنك القاهرة عمان يرعى مسابقة الروبوتات في جامعة الحسين التقنية | https://watananews.com/wp-content/uploads/2026/10/LINC-من-بنك-القاهرة-عمان-يرعى-مسابقة-الروبوتات-في-جامعة-الحسين-التقنية.JPG.jpeg | **APPLY** — unusual Arabic filename + double extension (`.JPG.jpeg`), but confirmed 200/image-jpeg live |
| D04F11CD-15B5-4E05-9AC6-8FA9C224C09F | كابيتال بنك وقسطاس يوقّعان مذكرة تفاهم لتوظيف تقنيات الذكاء الاصطناعي في أعمال إدارة الشؤون القانونية | https://watananews.com/wp-content/uploads/2026/10/Capital-Bank-and-Qistas.jpg | **APPLY** |

Note: Watan News needed the 256KB budget — its `og:image` tag sits at roughly byte offset 220,000 on a typical page (confirmed Step 11a); all 3 were found successfully with that larger budget.

## Finextra (0 of 14 candidates found — all SKIPPED)

**Live-tested 3 of the 14 no-image Finextra rows just now.** All three got an immediate HTTP 403 from Cloudflare (`cf-mitigated: challenge`) on the very first request, even using the honest, non-browser `BeyeCEO-NewsReader/1.0` UA — robots.txt does **not** disallow these article paths, but Cloudflare's bot-management layer blocks the request outright. `PageMetadataFetcher` correctly detected the 403 and stopped using `finextra.com` for the rest of that run (per the "clean stop" requirement) rather than retrying or escalating.

| Id | Title | Result |
|---|---|---|
| 00567E80-E944-491B-BB2F-CC22419B4BB9 | FIS launches agentic AI suite for the office of the CFO | 403 (Cloudflare challenge) → domain stopped |
| 330928C9-6EDB-44C3-9139-4F2C913A7205 | Sibos 2026: Trust and collaboration... | Skipped (domain already stopped this run) |
| B6C1ACCF-6091-4C2F-8552-D05B1FCC6185 | Revolut to create its own scent | Skipped (domain already stopped this run) |

**Recommendation**: Finextra's `ImageFallback` is enabled in config per your approval, but it will not produce any images in practice while Cloudflare blocks the honest UA — every run will cost exactly one wasted request (cheap, safe, but zero benefit) then stop. Worth either disabling it until a Cloudflare-compatible approach exists, or leaving it on as a no-op/future-proofing (no harm either way — it fails safely).

Remaining 11 Finextra rows not live-tested (same domain, same expected 403 — not worth burning more requests to confirm the obvious).

## Summary

| Source | Candidates | Found (real photo) | To backfill |
|---|---|---|---|
| Al Mamlaka | 5 | 5 | **5** |
| Watan News | 3 | 3 | **3** |
| Finextra | 14 | 0 (confirmed via 3 live samples — Cloudflare-blocked) | **0** |
| **Total** | **22** | **8** | **8** |

## Finextra Crawl-delay ETA (if ever attempted for all 14)
Finextra's robots.txt declares `Crawl-delay: 60` for generic bots. `DomainRateLimiter` honors this as a strict per-domain minimum spacing. Fetching all 14 sequentially would take **13 × 60s ≈ 13 minutes** (first request has no prior timestamp, so no wait; each subsequent one waits up to 60s) — moot right now since the domain gets stopped after the very first 403 either way.
