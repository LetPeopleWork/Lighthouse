
Band: the lower end of High (33.9 %) is below N (83.8 %), so **band 4**: moving logic cannot reach 90 %.

Three sensitivity runs, printed by the same script (`bootstrap.py`), none changes the band:

- With the second rater's labels in place of the first rater's for the ten files they read (`labels-second-rater-on-its-files.tsv`): Low 9.8 % (lower end 0.0 %), Middle 12.6 % (lower end
  2.5 %), High 51.6 % (lower end 37.9 %, upper end 62.5 %). Band 4.
- With N = 0.852 (above), or 0.8373 (the current CSV total of 9,530 rows): lower ends unchanged, band 4.
- With the labels as first written, before the tightened wording (`labels-round1.tsv`, `bootstrap-output-round1.txt`): Low 9.8 % (0.0 % to 21.7 %), Middle 17.9 % (6.4 % to 29.7 %), High 47.8 % (37.0 % to 57.3 %). Band 4.

**The verdict rule and the band wording were not edited after a number was seen.** The verdict rule's text (the Statistic, the N definition and the four-band table, from "Statistic" to
the line before "Which model does what") is compared with the DESIGN commit `3ad54cfa0`, and the band wording table with the DISTILL commit `f249408df` (`rule_diff.sh`); each diff is
empty, and the line counts printed show that the text was found (18 and 7):

