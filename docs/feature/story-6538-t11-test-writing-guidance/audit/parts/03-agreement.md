
## 4. Second rater and agreement (scenario A-4)

**Who.** The first rater is this crafter agent (model `sonnet`, the profile's `agent_model`). The second rater is a separate dispatch of a general-purpose agent on model
`haiku` (the profile's `reviewer_model`), given only the labelling rule, the ten files' paths and their test titles (`rater2-input.md`), and told not to open any label or
audit file. The two models differ. The second rater read the same 10 files: the 5th, 10th, ... 50th picks, 242 tests.

**Agreement** is the unrounded share of tests given the same label. **80 % means at least 0.80.** Cohen's kappa is reported and does not gate.

### Round 1, labelling rule as written in the design

`agreement-round1.txt` (script `agreement.py`, the second rater's labels in `rater2-labels-round1.tsv`):

```
agreement 179/242 = 0.739669; kappa 0.5816
```

0.739669 is below 0.80, so the first rater's labels did not stand and the redo path ran. The 63 disagreeing tests are listed with their titles in
`disagreements-round1.md` (file, test number, first rater, second rater, title). They fall in three kinds: the first rater's Logic against the second rater's Mixed
(26 tests), Mixed against UI (23) and UI against Mixed (11), with 3 Logic against UI.

### The wording change

Only the labelling rule's wording (the section "What is decided per test" in `feature-delta.md`) was changed, in one paragraph headed "Tightened after the first agreement
round", with disagreeing tests as its examples. The verdict rule and the band wording were not touched: `rule_diff.sh` (section 5) prints nothing.

### The redo, one round

Both raters redid the 10 files with the tightened wording; the second rater was again blind to all earlier labels. The first rater also re-read, in the other 40 files, every
test of the kinds that had disagreed: every Logic test that mounts a component (now Mixed) and every Mixed test that states only presence (now UI). The rows changed are in
`labels_round2.tsv`.

`agreement-round2.txt`, with the 68 disagreeing tests listed with titles in `disagreements-round2.md`:

```
agreement 174/242 = 0.719008; kappa 0.5272
confusion (first, second): {('U','U'): 55, ('U','M'): 56, ('M','U'): 12, ('M','M'): 93, ('L','L'): 26}
```

**0.719008 is below 0.80 after the one round. No second round is run. The headline is marked "low confidence".**

- **What the second rater saw.** Of the 86 Logic tests in the sample, **26 were rated by both raters and all 26 agree**. The other 60 were never seen by the second rater, among them 58 of the
  67 tests that make up Low (picks 24 and 48, the two hook files; the other 9 are in pick 50). **Low rests mostly on tests only the first rater labelled.**
- **The tightening narrowed what counts as Logic.** The added wording says a test that mounts a component is never Logic. That is a categorical exclusion, made within the one agreement
  round the design allows. On the labels as first written (`labels-round1.tsv`) Middle was 17.9 % (interval 6.4 % to 29.7 %), on the final labels it is 12.6 % (2.5 % to 23.9 %); Low is 9.8 % on both,
  High 47.8 % against 45.2 % (`bootstrap-output-round1.txt` against `bootstrap-output.txt`). The band is 4 on both, because even the larger Middle is far below N.
- **The largest remaining kind was not re-read.** 56 of the 68 round-2 disagreements are first rater UI against second rater Mixed. The design asks the first rater to re-read, in the other 40
  files, every test of a kind that disagreed; that was done for the Logic-against-Mixed kind and for Mixed tests that state only presence, and **not for the UI tests that state a value a rule decided**.
  High (which takes the Mixed tests) is therefore likely understated. It is unlikely to lift the band: High's lower end is 33.9 % and N is 83.8 %, so a re-read would have to turn most of the UI tests (over half of all sampled tests) into Mixed.
  Low and Middle do not depend on this kind.
