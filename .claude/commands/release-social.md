---
description: Draft social media posts (Slack #general and LinkedIn) from the latest release-notes block. Resolves Slack handles via the Slack Web API, optionally posts the Slack draft to #general using a bot token, and saves both drafts to disk for review.
allowed-tools: Bash, Read, Edit, Write, Glob, Grep, AskUserQuestion
---

# /release-social — draft Slack + LinkedIn posts for the latest release

You are drafting two social posts from the most recent release block at the top of `docs/releasenotes/releasenotes.md`:

- **Slack** — friendly internal-community update for the `letpeoplework` workspace, `#general` channel. After review, you can POST it directly via the Slack Web API using a bot token.
- **LinkedIn** — a more polished, public-facing post that leads with one spotlight feature and lists the rest as sidenotes. Saved to disk only — LinkedIn has no convenient bot-posting API for personal posts.

## Prerequisites — Slack bot configuration

Posting to Slack runs against the **Slack Web API**, not the MCP server. A Slack bot user must be configured with:

- **Bot token** in env var `SLACK_BOT_TOKEN` (format `xoxb-...`). Required for both identity lookup and posting.
- **Scopes**: `chat:write` (post messages), `users:read` (resolve contributor handles), `channels:read` (resolve `#general` to a channel ID). Add `chat:write.public` if you want the bot to post without being invited to the channel.
- **Bot invited to `#general`** once, unless `chat:write.public` is granted.

Optional: `SLACK_CHANNEL_ID` env var — if set, post there instead of resolving `#general`. Useful for testing in a private channel first.

**Token handling rules** (non-negotiable):

- Never echo, log, or interpolate `SLACK_BOT_TOKEN` into output the user can see. Use it only as an `$SLACK_BOT_TOKEN` shell variable inside `Authorization: Bearer ...` headers. The shell substitutes it; the command string you describe to the user must keep the literal `$SLACK_BOT_TOKEN` reference.
- Never commit it. Never include it in file contents.
- If `SLACK_BOT_TOKEN` is unset, fall back to **draft-only mode**: skip identity lookup and posting, save both files, and tell the user in the final report what to set to enable posting.

## Fixed context (do NOT ask)

- **Release notes source**: `docs/releasenotes/releasenotes.md` — the FIRST `# Lighthouse vX.Y.Z.W` block (top of file) is the release we draft for, unless `$ARGUMENTS` names a different version.
- **Output folder**: `social-posts/` at repo root (outside `docs/` so Jekyll won't publish it). Create it with `mkdir -p` if missing. Files per release:
  - `social-posts/<version>-slack.md`
  - `social-posts/<version>-linkedin.md`
- **Slack cache**: `social-posts/.slack-cache.json` — caches the resolved user list and `#general` channel ID for 24 hours so we don't hammer `users.list` on every run. Store ONLY `{id, real_name, display_name}` per user — no emails, no profile fields. This file MUST be gitignored before it's written; check `.gitignore` and append if needed.
- **Slack API base**: `https://slack.com/api/`. All calls use `curl` with `Authorization: Bearer $SLACK_BOT_TOKEN`. Every response must be checked for `ok: true`; surface `error` strings verbatim on failure.
- **LinkedIn**: no API path. The file is the artifact; user posts manually.
- **Docs URL** for the changelog link: prefer the GitHub compare link in the release block; if that's `compare/<old>...HEAD` (placeholder), fall back to `https://docs.lighthouse.letpeople.work/releasenotes.html`.

`$ARGUMENTS` may optionally be a version (e.g. `v26.5.14.2`) to draft for a specific past release. If empty, target the top block.

## Step 1 — locate and parse the release block

Read enough of `docs/releasenotes/releasenotes.md` to capture the target section. Extract:

1. **Version** — from `# Lighthouse vX.Y.Z.W`. If `vNext`, continue drafting against the placeholder.
2. **Headline features** — every `## <Name>` section that is NOT `## Bugfixes and Improvements` and NOT `## Contributions ❤️`. For each, capture title, prose summary, and any image URLs `![...](https://...png)`.
3. **Bugfixes list** — bullets under `## Bugfixes and Improvements`. Preserve `**title**` short labels.
4. **Contributors** — entries under `## Contributions ❤️`. Each is `- [Name](LinkedIn URL)` or `- Name`. Collect `{name, linkedin_url}` in order.
5. **Changelog link** — the `**Full Changelog**` URL.

If the entire release block is missing or empty, stop and tell the user.

## Step 2 — resolve Slack identities via the Slack Web API

Skip this entire step (and leave all contributors as bold plain text in the Slack draft) if `SLACK_BOT_TOKEN` is unset. Note this in the final report.

Otherwise:

### 2a — ensure `.slack-cache.json` is gitignored

```bash
grep -qxF 'social-posts/.slack-cache.json' .gitignore 2>/dev/null \
  || echo 'social-posts/.slack-cache.json' >> .gitignore
```

### 2b — load or refresh the cache

If `social-posts/.slack-cache.json` exists and its `fetched_at` is less than 24 hours old, read it. Otherwise, refresh:

```bash
# Paginate users.list. Bot needs users:read.
curl -sS -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
  'https://slack.com/api/users.list?limit=200' -o /tmp/slack-users.json

# List public channels to find #general. Bot needs channels:read.
curl -sS -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
  'https://slack.com/api/conversations.list?types=public_channel&limit=200' \
  -o /tmp/slack-channels.json
```

Check `ok` in each response. If false, surface the `error` and fall back to draft-only mode (don't crash the whole command).

Build the cache file with `jq`, keeping only minimal fields:

```bash
jq -n \
  --slurpfile u /tmp/slack-users.json \
  --slurpfile c /tmp/slack-channels.json \
  --arg ts "$(date -u +%FT%TZ)" \
  '{
    fetched_at: $ts,
    users: ($u[0].members | map({id, real_name, display_name: .profile.display_name})),
    channels: ($c[0].channels | map({id, name}))
  }' > social-posts/.slack-cache.json

rm -f /tmp/slack-users.json /tmp/slack-channels.json
```

If `users.list` returns a `next_cursor`, paginate (loop with `?cursor=…`) until exhausted. For most workspaces a single page covers it.

### 2c — resolve each contributor

For every contributor name, case-insensitive exact match against `real_name` OR `display_name` in the cache. Exactly one hit → record `<@USERID>`. Zero hits or multiple plausible hits → leave as bold plain text `*Name*`. Never fuzzy-match. Never guess.

### 2d — resolve the target channel

If `SLACK_CHANNEL_ID` is set, use it directly. Otherwise look up the channel named `general` in the cache. If neither resolves, fall back to draft-only mode for posting (handles still resolved fine).

## Step 3 — draft the Slack post

Slack uses **mrkdwn**: `*bold*` (single asterisks), `_italic_`, no `#`/`##` headings, links are `<https://url|label>`. **Channel mentions**: keep the human-readable `#general` in the draft file (so the .md stays readable), and let Step 7b substitute it for the API-required `<#CHANNELID>` syntax at post time. The bare text `#general` is auto-linked only when typed in the Slack client — `chat.postMessage` does NOT auto-resolve it.

Structure (adapt to actual content; don't pad if a section is empty):

```
:bulb: *Lighthouse <version> is out!* :rocket:

Hey #general — here's what shipped:

*<Headline 1>*
<1–2 sentence plain-language summary. End with <https://docs.lighthouse.letpeople.work/...|read more> if a docs link exists.>
<one image URL on its own line if there's a representative asset — Slack auto-unfurls>

*<Headline 2>*
<same shape>

*Fixes & improvements*
• <bullet title> — <one sentence>
• <bullet title> — <one sentence>

*Thanks to* <@U123>, <@U456>, and *Plain Name* for the feedback that shaped this release :heart:

Full changelog: <https://github.com/LetPeopleWork/Lighthouse/compare/<old>...<new>|here>
```

Rules:

- Max **3** headline sections. Extras fold into the fixes list with bold titles.
- Max **5** fixes bullets. Beyond that, collapse the tail to one "and a handful of smaller polish fixes" line.
- **Don't embed bare image URLs.** Slack only auto-unfurls 1–2 URLs per message and tends to pick the docs link (rich preview with title/hero/description) over a bare PNG URL — so the image URL renders as plain-text noise. Instead, rely on the headline's `<https://docs...|Read more>` link to unfurl, since the docs page's hero image is usually the same screenshot you'd have embedded. If you specifically need an inline image rendered (not as an unfurl card), use the Block Kit `image` block via `chat.postMessage`'s `blocks` field — not supported by the current draft format and out of scope unless asked.
- Max ~5 @-mentions. If contributors exceed that, tag the first 5 and append "and others".
- Emoji budget: max 4 total. Prefer `:rocket:`, `:heart:`, `:bug:`, `:sparkles:`, `:bulb:`, `:warning:`. Skip `:lighthouse:` unless you know the workspace has that custom emoji.
- Tone: pragmatic, mildly enthusiastic — match the release-notes voice. **Banned words**: "excited", "thrilled", "game-changing", "revolutionary".
- Drop the `*Thanks to*` line entirely if no contributors.

## Step 4 — draft the LinkedIn post

LinkedIn renders plain Unicode. No Markdown. Use line breaks, emoji, and bullet glyphs for structure.

```
🚀 Lighthouse <version> is live.

The headline this release: <one benefit-led sentence — what the user can now DO that they couldn't before>.

<2–3 sentence punchier rewrite of the spotlight feature. Lead with the user outcome.>

Also in this release:
• <Headline 2 — label + one-line benefit>
• <Headline 3 if relevant — same>
• <Notable user-facing bugfix — same>

A big thank you to <Name>, <Name> for the feedback that shaped this release ❤️

Full release notes → https://docs.lighthouse.letpeople.work/releasenotes.html

#Lighthouse #FlowMetrics #Forecasting #ProductDevelopment
```

Rules:

- **One** headline gets the spotlight. Pick the most outwardly-meaningful feature. Everything else becomes a 1-line bullet.
- Contributors by full name only — no inline URLs (LinkedIn auto-linkifies them and the post looks messy). User swaps to @-tags at posting.
- Hashtags: 3–5, chosen from `#Lighthouse #FlowMetrics #Forecasting #ProductDevelopment #LeanAgile #Kanban #Scrum #ProductManagement #OpenSource #ProductivityForTeams`. Match what the release actually touches.
- 200–300 words. Cut anything that doesn't earn its place.
- Emoji: lead with 🚀; at most one more in the body.
- No image embedding. If the release has a usable screenshot, note in the header comment which file to attach.

## Step 5 — show drafts and collect the review decision

Print both drafts inline so the user can read them without opening files. Then use `AskUserQuestion` with up to 3 questions in one call:

1. **Slack action** —
   - If `SLACK_BOT_TOKEN` IS set AND a channel resolved: options `Use as-is and post to #general (Recommended)`, `Use as-is, save only`, `Tweak it`, `Skip Slack`.
   - Otherwise: options `Use as-is, save only (Recommended)`, `Tweak it`, `Skip Slack`. Description explains why posting isn't available (missing token / unresolved channel).
2. **LinkedIn action** — options: `Use as-is`, `Tweak it`, `Skip LinkedIn`. (No "post" — there is no LinkedIn posting path.)
3. **Image to pair with LinkedIn post** — only if the release block has images. Options: up to 3 image filenames + `None`. The chosen filename becomes the `Attach:` value in the LinkedIn file's header comment.

If the user picks `Tweak it`, ask one follow-up free-text question for changes and re-draft **once**. Don't loop.

## Step 6 — save the drafts

```bash
mkdir -p social-posts
```

Write:

- `social-posts/<version>-slack.md` — pure Slack mrkdwn body, ready to paste / post. Top of file: `<!-- Paste into #general in the letpeoplework Slack, or post via /release-social. -->`. Nothing else outside the message body.
- `social-posts/<version>-linkedin.md` — plain text LinkedIn body. Top: `<!-- Paste into LinkedIn. Attach image: <filename or "none">. -->`.

Skip the file the user said to skip.

**Never overwrite** an existing `social-posts/<version>-<platform>.md` without asking — versions are stable, so an existing file usually means it was already posted.

## Step 7 — post to Slack (only when the user picked "Use as-is and post")

### 7a — refuse to double-post

Read the saved `social-posts/<version>-slack.md`. If it contains a `<!-- Posted: ... -->` line, stop and tell the user this release was already announced — show them the permalink from the comment. Don't re-post.

### 7b — build the payload

Strip the leading `<!-- ... -->` header comment(s), substitute channel mentions to API-clickable form, and build a JSON payload with `jq` (don't inline the body in the curl command — quoting hell):

```bash
# 1. Strip all leading HTML-comment-only lines (paper-trail may add several).
sed '/^<!--.*-->$/d' social-posts/<version>-slack.md > /tmp/slack-body.txt

# 2. Substitute every `#general` in the body with the API's <#CHANNELID> form,
#    so the message renders the channel as a clickable mention. Look up the
#    general channel ID from the Step 2 cache (separately from $CHANNEL_ID,
#    which may be #testing or another override).
#    NOTE: use a comma (or any non-`|`) sed delimiter — `|` clashes with the
#    alternation inside the regex and breaks with `unknown option to 's'`.
GENERAL_CH_ID=$(jq -r '.channels[] | select(.name=="general") | .id' social-posts/.slack-cache.json)
if [ -n "$GENERAL_CH_ID" ] && [ "$GENERAL_CH_ID" != "null" ]; then
    sed -i -E "s,#general([^A-Za-z0-9_]|$),<#${GENERAL_CH_ID}>\\1,g" /tmp/slack-body.txt
fi

# 3. Build payload.
payload=$(mktemp)
jq -n \
  --arg ch "$CHANNEL_ID" \
  --rawfile body /tmp/slack-body.txt \
  '{channel:$ch, text:$body, unfurl_links:true, unfurl_media:true}' \
  > "$payload"
```

`$CHANNEL_ID` is the resolved channel ID from Step 2d (target of the post — may be `#testing` or another override). `$GENERAL_CH_ID` is always the `#general` channel ID, used only for body-mention substitution; keep them separate so trial posts to `#testing` still render the `#general` reference as a real channel link (clickable, navigates the reader to where the live post will end up).

### 7c — POST `chat.postMessage`

```bash
curl -sS -X POST \
  -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
  -H 'Content-Type: application/json; charset=utf-8' \
  --data @"$payload" \
  https://slack.com/api/chat.postMessage > /tmp/slack-resp.json

rm -f "$payload" /tmp/slack-body.txt
```

Check `ok` in `/tmp/slack-resp.json`. On `ok: false`, map the `error` field:

- `not_in_channel` → tell user to invite the bot to `#general` (or grant `chat:write.public`). Saved draft stays. Stop.
- `invalid_auth` / `token_revoked` / `account_inactive` → tell user the token is bad. Saved draft stays. Stop.
- `channel_not_found` → tell user the resolved channel ID doesn't exist; suggest setting `SLACK_CHANNEL_ID` explicitly or refreshing the cache. Saved draft stays. Stop.
- Any other error → echo the `error` verbatim, leave saved draft intact, stop.

On `ok: true`:

```bash
TS=$(jq -r '.ts' /tmp/slack-resp.json)
curl -sS -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
  "https://slack.com/api/chat.getPermalink?channel=$CHANNEL_ID&message_ts=$TS" \
  > /tmp/slack-perma.json
PERMALINK=$(jq -r '.permalink' /tmp/slack-perma.json)
rm -f /tmp/slack-resp.json /tmp/slack-perma.json
```

### 7d — append the paper-trail to the saved draft

Insert a second HTML-comment line directly under the existing header, recording the permalink and an ISO timestamp:

```
<!-- Paste into #general in the letpeoplework Slack, or post via /release-social. -->
<!-- Posted: <permalink> at <ISO timestamp> -->
```

This is the marker Step 7a checks for to prevent double-posts.

## Step 8 — final report

Six-line summary:

1. Release version, headline count, bullet count, contributor count.
2. Slack identity resolution: handles resolved / unresolved (or "skipped — `SLACK_BOT_TOKEN` unset").
3. Slack draft path.
4. Slack post status: `posted` + permalink / `saved only` / `failed: <error>`.
5. LinkedIn draft path + image to attach (or "skipped").
6. Reminders: for LinkedIn, paste manually and attach the chosen image; for Slack, only remind if not posted ("paste `<path>` into `#general`").

## Guardrails

- Never echo or log `SLACK_BOT_TOKEN`. Always reference it as the literal `$SLACK_BOT_TOKEN` in command strings; let the shell substitute. Never `echo "$SLACK_BOT_TOKEN"`, never write it to a file, never include it in tool descriptions.
- Never post without an explicit user pick of `Use as-is and post` in Step 5. There is no auto-post mode and no `--yes` argument.
- Never double-post: if the saved Slack draft already has a `<!-- Posted: ... -->` line, refuse.
- Never delete the saved draft after a failed post — the user needs it to retry once they fix the config.
- Always gitignore `social-posts/.slack-cache.json` before writing it. The cache contains workspace user metadata.
- Never invent a Slack user ID. Unresolved names stay as bold plain text.
- Never invent a LinkedIn handle or URL. Plain names only.
- Never overwrite an existing draft file without asking.
- Never restate the entire release notes verbatim. Posts are short summaries.
- Never reference image URLs that aren't in the release block (no inventing assets).
- Drop the `Thanks to ...` line entirely if no contributors.
- Banned tone in both posts: "excited", "thrilled", "game-changing", "revolutionary", or other marketing fluff. Match the release-notes voice.
