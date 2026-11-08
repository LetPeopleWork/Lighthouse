
## 5. The estimate and the band (scenario A-5)

**Statistic** (the design's, unchanged): each share is weighted by test count and pooled over the sample. **Low** = logic tests in E0 files / all sampled tests. **Middle** = logic
tests in E0 and E1 files, the headline. **High** = logic plus mixed tests in files of every class. Low <= Middle <= High by construction. The comparison uses the **lower end** of
each 95 % interval; the point and the upper end are reported beside it.

**N, the needed share, and its source.** Frontend alone, N = 4,907 of 5,857 = **0.8378** (about 84 %), the design's figure from the brief's counts: 90 % of 9,505 tests is 8,554.5, of
which 3,648 are already fast, leaving 4,907 of the 5,857 `.tsx` tests to move. The 5,857 is also the number of `.test.tsx` rows in the CSV of the run named in section 6, so the brief's count
is still the current one by that measure. No backend labels exist yet (T6 has not labelled them), so N is for the frontend alone and the whole-suite N is not used. As a sensitivity, N computed the
same way from today's listing (fast tier = the 2,578 non-`.tsx` tests of 7,970, 90 % = 7,173, so 4,595 of 5,392 must move) is 0.852; the band below is 4 under that N too.

**Method line.** Percentile bootstrap: files resampled with replacement within their group, 10,000 resamples, seed 6538, 2.5th and 97.5th percentiles (linear interpolation between order
statistics), test-count weighted pooled ratio (`bootstrap.py`, output as printed):

