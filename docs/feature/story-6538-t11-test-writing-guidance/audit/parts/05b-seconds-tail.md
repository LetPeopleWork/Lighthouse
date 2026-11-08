
**None of the ten enters the estimate unless the pick rule chose it.** One was chosen by the rule: `components/Common/Team/ModifyTeamSettings.test.tsx` (pick 18, group `components/Common`
without `Charts`, position 109). It is in the sample because the rule chose it, not because it is slow. The other nine are not in the sample. They say where a rewrite would pay first and
decide nothing here.

**Seconds in the sample's tests**, from the same CSV, matched to the sampled tests by their full title (666 of the 682 matched; the other 16 are parametrised titles that the CSV
names per case). Seconds are reported beside the shares because a share moved is not seconds saved:

| Tests | Seconds | Share of the matched seconds |
|---|---|---|
| Low (logic in E0 files) | 1.13 | 1.9 % |
| Middle (logic in E0 and E1 files) | 1.15 | 1.9 % |
| High (logic plus mixed) | 34.97 | 57.1 % |
| All matched sample tests | 61.27 | 100 % |

Per test, on the matched tests, the logic tests took about 13 ms on average (1.15 s over 85), the mixed tests about 157 ms (33.82 s over 215) and the UI tests about 72 ms (26.31 s over
366). So the logic tests are the cheap ones: moving them saves little time even where the share is large. Time stays the rule.
