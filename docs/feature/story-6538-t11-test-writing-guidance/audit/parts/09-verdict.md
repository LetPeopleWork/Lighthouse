## Verdict block (goes to the maintainer; the last section of this record)

**LOW CONFIDENCE.** Agreement between the two raters stayed below 0.80 after the one round (0.719008, kappa 0.5272), so the headline carries a low-confidence mark. The mark concerns the
line between UI and Mixed labels. The raters agree on every Logic test (26 of 26), and the Logic share is what Low and Middle are built from.

| Field | Content |
|---|---|
| Commit, file and test counts | Listing run at `ced6e04acb72b5adda3fa87fe45e749e584e9f80`; 410 `.test.tsx` files by `git ls-files` and by the Vitest listing, no difference to name; 5,392 tests in the listing (the CSV's 5,857 counts parametrised cases one by one). Sample: 50 files, 682 tests |
| N used and where it came from | 0.8378 (4,907 of 5,857 `.tsx` tests must move for the fast tier to reach 90 %), the design's figure from the brief's counts; frontend alone, because the backend has no labels yet. Sensitivity: 0.852 from today's listing, same band |
| Low, Middle, High | Test-count weighted, percentile bootstrap, files resampled within group, 10,000 resamples, seed 6538, 2.5th and 97.5th percentiles. **Low** 9.8 %, interval 0.0 % to 21.7 %. **Middle (headline)** 12.6 %, interval 2.5 % to 23.9 %. **High** 44.9 %, interval 33.6 % to 54.5 % (51.3 %, 37.6 % to 62.3 % on the second rater's labels for its ten files) |
| Seconds | Run 42457232387 (green `main`, 2026-11-08), 666 of 682 sample tests matched: the Low tests took 1.13 s, the Middle tests 1.15 s, the High tests 34.52 s, out of 61.27 s. The logic tests are the cheap ones (about 13 ms each against about 157 ms for a mixed test), so a large share moved saves little time |
| Agreement and confidence | Round 1: 179 of 242 = 0.739669 (kappa 0.5816). Round 2, after the tightened wording: 174 of 242 = 0.719008 (kappa 0.5272). Low confidence, marked above |
| Band and verdict | **Band 4**: the lower end of High (33.6 %) is below N (83.8 %). The verdict rule and the band wording are unchanged since the DESIGN and DISTILL commits (empty diffs, section 5) |
| What the maintainer decides | Whether to plan any rewrite. No rewrite is planned in this Story under any band |

Wording sent to the maintainer for band 4: "Moving logic cannot reach 90 %. Keep the shares as a direction and let time stay the rule."

Nothing was edited in `Lighthouse.Frontend`; the call column of every row reads *keep*.
