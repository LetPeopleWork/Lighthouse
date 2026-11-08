
## 7. No rewrite or move is planned (scenario A-7)

Search of the roadmap file in the paths this slice changes (`docs/feature/story-6538-t11-test-writing-guidance/deliver/roadmap.json`; no other roadmap is in the slice's paths) for the words
`rewrite` and `move`, `grep -o -i -E '.{70}(rewrite|move).{60}' deliver/roadmap.json`:

```
earch of the roadmap files in the slice's changed paths for the words rewrite and move prints no step to rewrite or move a test (output p
 never edited after a number is seen (STOPPED block if they were). No rewrite is planned under any band. The audit record is written to d
on in the Wave section) with T5's bullet cut to the rule and its body moved into backend.md, and the ledger pointer in docs/ci-learnings
```

Three hits, none a step that rewrites or moves a test: the first is the text of this search's own criterion, the second says "No rewrite is planned under any band", the third is the cut of a
`CLAUDE.md` bullet into a document. This slice's roadmap was not edited by the audit.

