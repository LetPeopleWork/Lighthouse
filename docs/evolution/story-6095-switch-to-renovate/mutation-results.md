# Mutation testing — 6095 (Renovate is the one dependency bot)

The run was on 2026-09-29 against the three guard scripts this story ships:
`.github/scripts/action-pins.mjs`, `.github/scripts/dependency-bot.mjs` and the YAML reader they share,
`.github/scripts/scanned-yaml-files.mjs`. The gate is an 80 % kill rate, **on each file**, not only overall.

| run | commit (tree) | killed | timeout | survived | score | action-pins | dependency-bot | scanned-yaml-files |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| first | `c94a912e7` (`3da0f2ddf`) | 298 | 2 | 62 | 82.87 % | 83.70 % (15 survived) | 84.24 % (29 survived) | 79.07 % (18 survived) |
| after test remediation | `120284c4e` (`26df2a25a`) | 339 | 2 | 21 | **94.20 %** | **94.57 %** (5 survived) | **94.02 %** (11 survived) | **94.19 %** (5 survived) |

Both runs mutate the same 362 mutants. The two timeouts are the same in both, in `withoutComment`'s loop
(`at >= text.length`, `at--`): they never end, so Stryker counts them as detected.

## Setup

StrykerJS 9.6.1 from `Lighthouse.Frontend/node_modules`, with the **command runner** over
`node --test .github/scripts/action-pins.test.mjs .github/scripts/dependency-bot.test.mjs`. The project's
Stryker configs are built around Vitest, and these scripts live outside any package. Coverage analysis is
off, which the command runner requires. Config: `stryker.6095.json`, next to this file.

The sandbox holds only the tracked files the guards read, so the tests that check the real repository run
against the committed state:

```sh
SB=$(mktemp -d)
git archive HEAD .github renovate.json | tar -x -C "$SB"
cp docs/feature/story-6095-switch-to-renovate/mutation/stryker.6095.json "$SB"/
cd "$SB" && /path/to/Lighthouse/Lighthouse.Frontend/node_modules/.bin/stryker run stryker.6095.json > stryker.log 2>&1
```

A run takes under a minute.

## Closed by this pass

Commit `120284c4e`, tests only. Each test goes through `findActionPinViolations` or
`findDependencyBotViolations` on a temporary tree; nothing internal is exported.

`action-pins.test.mjs`:

| test | mutants it kills |
| --- | --- |
| accepts a double-quoted commit and release | `quote === '"'` → `false`, and `'"'` → `""` (a double-quoted pin was read as the unquoted kind and rejected) |
| rejects a hash longer than a commit, such as a SHA-256 digest | `FULL_COMMIT` without `^`, and without `$` |
| rejects a tag written with a space before the colon (`uses :`) | `USES_KEY` `\s*` → `\S*` |
| rejects a tag on a step aligned with extra spaces after its dash (`-   uses:`) | `USES_KEY` `(?:-\s+)?` → `(?:-\s)?` |
| rejects a quoted bare commit | `commentOf`'s `: ''` → `"Stryker was here!"` (a quoted pin with no comment counted as labelled) |
| tells how to pin the action it names | `actionOf` → `undefined`, and `split('@')` → `split('')` |
| rejects a job that calls a workflow from another repository by a tag | `USES_KEY` with the `- ` made mandatory (a job-level `uses:` has no dash) |
| does not read the text of a `run: \|2-` block | `BLOCK_INDICATOR` `[1-9][+-]?` → `[1-9][^+-]?` |
| does not read the text of a `run: >-   # a comment aligned with others` block | `BLOCK_INDICATOR` `\s+#` → `\s#` |
| does not read the text of a `run:   \| ` block (extra spaces, trailing space) | the `.trim()` before the indicator test dropped |
| keeps reading a block as text past an empty line / a line of stray spaces shallower than the key | `text.trim() === ''` → `false`, → `"Stryker was here!"`, and `.trim()` dropped |
| reads the steps under a key whose comment ends in `>` (`steps: # … <release>`) | `BLOCK_INDICATOR` without `^` (the comment's last `>` opened a block and hid every step) |
| reports files in name order, so every machine prints the same list | `.sort()` dropped. The workflows are written in reverse and an action is added, because tmpfs lists a folder in creation order and walks subfolders breadth-first |
| does not read files in the workflows folder that GitHub does not run (`nightly.yml.disabled`, `README.md`) | `isFile() && …` → `\|\|`, and `/\.ya?ml$/` without `$` |

`dependency-bot.test.mjs`:

| test | mutants it kills |
| --- | --- |
| a renovate.json that cannot be read fails the check instead of being reported missing | `error.code !== 'ENOENT'` → `false` |
| rejects a workflow that writes its if with a space before the colon (`if :`) | `IF_KEY` `\s*` → `\S*` |
| rejects a workflow that reads the actor after a quoted `#` (`'release #1'`) | `quote !== ''` → `false`; `char === quote` → `true` and → `!==`; `char === '"' \|\| char === "'"` → `false` and → `&&`; `char === "'"` → `false`; `"'"` → `""`; the quote-opening block emptied |
| rejects an actor condition that continues past an empty line / a line of stray spaces shallower than the key | `text.trim() !== ''` → `true`, → `"Stryker was here!"`, and `.trim()` dropped, in `conditionFrom` |
| rejects an actor condition on a step aligned with extra spaces after its dash (`-   if:`) | `IF_KEY` `(?:-\s+)?` → `(?:-\s)?` |
| does not read an actor condition that was commented out, key and all (`# if: github.actor …`) | `IF_KEY` without `^` |
| does not read a comment written with no space after its `#` (`#was: && github.actor …`) | `text[at - 1]` → `text[at + 1]` |
| reads a condition that follows a block in the same step | `KEY`'s first-character class negated, and `[^\s…]` → `[^\S…]` (both put the block's column one to the left); `indentOf(text) > blockKeyColumn` → `>=` |
| rejects a dependency ignored outside any rule, naming each one | `ignoreDeps.join(', ')` → `join('')` |
| does not ask for a reason when the policy has no packageRules at all | `: []` → `["Stryker was here"]` (a policy without `packageRules` threw) |

That is 41 mutants killed: 10 in `action-pins.mjs`, 18 in `dependency-bot.mjs`, 13 in `scanned-yaml-files.mjs`.

## Accepted survivors

The 21 left, each reviewed. "Equivalent" means no input the guard can receive changes the result;
"unreachable" means it can change the result, but only for a file nobody would commit.

`action-pins.mjs` (5, all equivalent):

1. `commentOf`: `startsWith('#')` → `startsWith('')`. After a quoted target, valid YAML allows only a
   comment or nothing, and for nothing both return `''`.
2. `commentOf`: `.slice(1).trim()` → `.slice(1)`. The label is only tested for being empty; `rest` is
   already trimmed, so the inner trim can never turn a non-empty label empty.
3. `ruleBroken`: `refStart === -1 ? '' : …` → `false`. With no `@`, the whole target is tested as the ref,
   and no `owner/repo` path is 40 hex characters.
4. The same, `-1` → `+1`. An `@` at index 1 needs a one-character target before it, which is not an
   action or workflow reference.
5. The same, `''` → `"Stryker was here!"`. Neither is a 40-character commit.

`dependency-bot.mjs` (11):

1. `exists`: `if (error.code === 'ENOENT') return false` → `true` — **unreachable**. Every other error
   `stat` raises on `.github/dependabot.yml` (a `.github` that is a file, a folder that cannot be
   searched) makes the scan of the same folder throw too, so the check still fails. Only a symlink loop
   at exactly that path would differ.
2. `exists`: the `catch` block emptied — **unreachable**, for the same reason.
3. `readFile(…, 'utf8')` → `readFile(…, '')`: equivalent. It returns a Buffer, and `JSON.parse` decodes a
   Buffer as UTF-8.
4. `lines[line - 1].slice(valueStart)` → `lines[line - 1]`: equivalent. The part cut off is only spaces,
   `- ` and `if:`, which never match the actor pattern and hold no quote or `#`.
5. `parts.join(' ')` → `join('')`: equivalent for real conditions. It only matters if `github.actor`,
   `user.login` or `[bot]` were split across two lines mid-token.
6. `at < text.length` → `<=`: equivalent. `text[text.length]` is `undefined`, which is no quote and no `#`.
7. `char === '"'` → `false`: equivalent. GitHub expressions quote strings with single quotes only, so a
   `#` inside a double-quoted YAML value is either inside a single-quoted literal too, or the expression
   is invalid.
8. `'"'` → `""`: equivalent, for the same reason.
9. `(at === 0 || text[at - 1] === ' ')` → `true`: equivalent. A `#` outside quotes with no space before it
   is not valid in a GitHub expression.
10. `at === 0` → `false`: equivalent. The first slice starts right after `if:`, so a `#` there would make
    `if:#` no key at all; a continuation line with `#` in column 0 has already ended the condition.
11. `at === 0` → `at !== 0`: equivalent, by the reasons of 9 and 10 together.

`scanned-yaml-files.mjs` (5):

1. `KEY` without `^` — **unreachable**. An unanchored match only lands on a block indicator for a
   comment at column 0 whose text is itself a key opening a block (`#    run: |`), and it then changes
   only lines indented deeper than that key that are not commented out. Commenting out the first line of
   a block but not its body is not valid YAML.
2. `KEY` `(?:\s|$)` → `(?:\s)`: equivalent. `[^:]*` stops at the first colon, so a key ending the line
   leaves nothing after it, and nothing is not a block indicator.
3. `BLOCK_INDICATOR` without `$`: equivalent. In valid YAML a value that starts with `|` or `>` is a
   block header; anything after the indicator other than a comment is a syntax error.
4. `kept = []` → `["Stryker was here"]`: equivalent. The extra entry has no `text`, and `undefined` never
   matches the `uses:` or `if:` key.
5. `blockKeyColumn = -1` → `+1` — **unreachable**. It only hides lines before the first line at indent 0
   or 1, and a workflow or action file starts with a top-level key in column 0.

## Defects found

None. No new test failed against the production code.

## Not mutated

The frontend changes in this story are a type annotation (`MarkElementProps`) and two icon import-path
renames. Neither carries runtime logic, so there is no frontend Stryker run.

## Final clear-text report

```
------------------------|------------------|----------|-----------|------------|----------|----------|
                        | % Mutation score |          |           |            |          |          |
File                    |  total | covered | # killed | # timeout | # survived | # no cov | # errors |
------------------------|--------|---------|----------|-----------|------------|----------|----------|
All files               |  94.20 |   94.20 |      339 |         2 |         21 |        0 |        0 |
 action-pins.mjs        |  94.57 |   94.57 |       87 |         0 |          5 |        0 |        0 |
 dependency-bot.mjs     |  94.02 |   94.02 |      171 |         2 |         11 |        0 |        0 |
 scanned-yaml-files.mjs |  94.19 |   94.19 |       81 |         0 |          5 |        0 |        0 |
------------------------|--------|---------|----------|-----------|------------|----------|----------|
```
