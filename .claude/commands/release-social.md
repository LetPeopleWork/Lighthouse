---
description: Draft a Slack #general post from the latest release-notes block. Resolves Slack handles via the Slack Web API, optionally posts the draft to #general using a bot token, and saves the draft to disk for review.
allowed-tools: Bash, Read, Edit, Write, Glob, Grep, AskUserQuestion
---

# /release-social — draft the Slack post for the latest release

You are drafting a **Slack** post from the most recent release block at the top of `docs/releasenotes/releasenotes.md`: a friendly internal-community update for the `letpeoplework` workspace, `#general` channel. After review, you can POST it directly via the Slack Web API using a bot token.

## Prerequisites — Slack bot configuration

Posting to Slack runs against the **Slack Web API**, not the MCP server. A Slack bot user must be configured with:

- **Bot token** in env var `SLACK_BOT_TOKEN` (format `xoxb-...`). Required for both identity lookup and posting.
- **Scopes**: `chat:write` (post messages), `users:read` (resolve contributor handles), `channels:read` (resolve `#general` to a channel ID). Add `chat:write.public` if you want the bot to post without being invited to the channel.
- **Bot invited to `#general`** once, unless `chat:write.public` is granted.

Optional: `SLACK_CHANNEL_ID` env var — if set, post there instead of resolving `#general`. Useful for testing in a private channel first.

**Token handling rules** (non-negotiable):

- Never echo, log, or interpolate `SLACK_BOT_TOKEN` into output the user can see. Use it only as an `$SLACK_BOT_TOKEN` shell variable inside `Authorization: Bearer ...` headers. The shell substitutes it; the command string you describe to the user must keep the literal `$SLACK_BOT_TOKEN` reference.
- Never commit it. Never include it in file contents.
- If `SLACK_BOT_TOKEN` is unset, fall back to **draft-only mode**: skip identity lookup and posting, save the file, and tell the user in the final report what to set to enable posting.

## Fixed context (do NOT ask)

- **Release notes source**: `docs/releasenotes/releasenotes.md` — the FIRST `# Lighthouse vX.Y.Z.W` block (top of file) is the release we draft for, unless `$ARGUMENTS` names a different version.
- **Output folder**: `social-posts/` at repo root (outside `docs/` so Jekyll won't publish it). Create it with `mkdir -p` if missing. File per release:
  - `social-posts/<version>-slack.md`
- **Slack cache**: `social-posts/.slack-cache.json` — caches the resolved user list and `#general` channel ID for 24 hours so we don't hammer `users.list` on every run. Store ONLY `{id, real_name, display_name}` per user — no emails, no profile fields. This file MUST be gitignored before it's written; check `.gitignore` and append if needed.
- **Slack API base**: `https://slack.com/api/`. All calls use `curl` with `Authorization: Bearer $SLACK_BOT_TOKEN`. Every response must be checked for `ok: true`; surface `error` strings verbatim on failure.
- **Docs URL** for the changelog link: prefer the GitHub compare link in the release block; if that's `compare/<old>...HEAD` (placeholder), fall back to `https://docs.lighthouse.letpeople.work/releasenotes.html`.

`$ARGUMENTS` may optionally be a version (e.g. `v26.5.14.2`) to draft for a specific past release. If empty, target the top block.

## Step 1 — locate and parse the release block

Read enough of `docs/releasenotes/releasenotes.md` to capture the target section. Extract:

1. **Version** — from `# Lighthouse vX.Y.Z.W`. If `vNext`, continue drafting against the placeholder.
2. **Headline features** — every `## <Name>` section that is NOT `## Bugfixes and Improvements` and NOT `## Contributions ❤️`. For each, capture title, prose summary, and any image URLs `![...](https://...png)`.
3. **Bugfixes list** — bullets under `## Bugfixes and Improvements`. Preserve `**title**` short labels.
4. **Contributors** — entries under `## Contributions ❤️`. Each is `- [Name](URL)` or `- Name`. Collect the names in order.
5. **Changelog link** — the `**Full Changelog**` URL.

If the entire release block is missing or empty, stop and tell the user.

## Step 2 — resolve Slack identities via the Slack Web API

Skip this entire step (and leave all contributors as bold plain text in the draft) if `SLACK_BOT_TOKEN` is unset. Note this in the final report.

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

Slack uses **mrkdwn**: `*bold*` (single asterisks), `_italic_`, no `#`/`##` headings, links are `<https://url|label>`. **Channel mentions**: keep the human-readable `#general` in the draft file (so the .md stays readable), and let Step 6b substitute it for the API-required `<#CHANNELID>` syntax at post time. The bare text `#general` is auto-linked only when typed in the Slack client — `chat.postMessage` does NOT auto-resolve it.

Structure (adapt to actual content; don't pad if a section is empty):

```
:bulb: *Lighthouse <version> is out!* :rocket:

Hey #general — here's what shipped:

*<Headline 1>*
<1–2 sentence plain-language summary. End with <https://docs.lighthouse.letpeople.work/...|read more> if a docs link exists.>
<!-- image: <the headline's screenshot URL from the release block> | <alt text> -->

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
- **Show the headline screenshots, as image markers.** We want images in every release post. Under each headline whose release-notes section carries a screenshot `![...](https://...png)`, put one line `<!-- image: <that URL> | <short alt text> -->`. At post time Step 6b turns each marker into a Block Kit `image` block at that spot, which Slack renders inline at full width. Worked well first time in v26.10.10.1. Never paste a bare PNG URL into the text instead: Slack unfurls only 1–2 links per message, usually picks the docs link, and leaves the PNG URL as plain-text noise. Check each URL returns `200 image/png` (`curl -s -o /dev/null -w '%{http_code} %{content_type}' <url>`) before posting; Slack fetches it itself and rejects the whole message (`invalid_blocks`) if it can't. Use only screenshots that are in the release block. A headline without one simply gets no image. In a pasted (not API-posted) draft the markers are HTML comments, so delete them before pasting.
- Max ~5 @-mentions. If contributors exceed that, tag the first 5 and append "and others".
- Emoji budget: max 4 total. Prefer `:rocket:`, `:heart:`, `:bug:`, `:sparkles:`, `:bulb:`, `:warning:`. Skip `:lighthouse:` unless you know the workspace has that custom emoji.
- Tone: pragmatic, mildly enthusiastic — match the release-notes voice. **Banned words**: "excited", "thrilled", "game-changing", "revolutionary".
- Drop the `*Thanks to*` line entirely if no contributors.

## Step 4 — show the draft and collect the review decision

Print the draft inline so the user can read it without opening files. Then use `AskUserQuestion` (one question):

- **Slack action** —
  - If `SLACK_BOT_TOKEN` IS set AND a channel resolved: options `Use as-is and post to #general (Recommended)`, `Use as-is, save only`, `Tweak it`, `Skip Slack`.
  - Otherwise: options `Use as-is, save only (Recommended)`, `Tweak it`, `Skip Slack`. Description explains why posting isn't available (missing token / unresolved channel).

If the user picks `Tweak it`, ask one follow-up free-text question for changes and re-draft **once**. Don't loop.

## Step 5 — save the draft

```bash
mkdir -p social-posts
```

Write `social-posts/<version>-slack.md` — pure Slack mrkdwn body, ready to post. Top of file: `<!-- Paste into #general in the letpeoplework Slack, or post via /release-social. -->`. Nothing else outside the message body except the `<!-- image: ... -->` markers from Step 3.

Skip the file if the user said to skip Slack.

**Never overwrite** an existing `social-posts/<version>-slack.md` without asking — versions are stable, so an existing file usually means it was already posted.

## Step 6 — post to Slack (only when the user picked "Use as-is and post")

### 6a — refuse to double-post

Read the saved `social-posts/<version>-slack.md`. If it contains a `<!-- Posted: ... -->` line, stop and tell the user this release was already announced — show them the permalink from the comment. Don't re-post.

### 6b — build the payload

Build it with the checked-in script `.claude/scripts/slack_release_payload.py`, never by hand or with an inline heredoc (the shell guard blocks those, and quoting the body breaks):

```bash
GENERAL_CH_ID=$(jq -r '.channels[] | select(.name=="general") | .id' social-posts/.slack-cache.json)
payload=$(mktemp)
python3 -I .claude/scripts/slack_release_payload.py \
  social-posts/<version>-slack.md "$CHANNEL_ID" "$GENERAL_CH_ID" > "$payload"
jq -r '.blocks[] | .type' "$payload"   # sanity check: section / image / section ...
```

What the script does:
- drops the `<!-- ... -->` header and paper-trail lines;
- turns each `<!-- image: URL | alt -->` marker into a Block Kit `{"type":"image","image_url":…,"alt_text":…}` block at that spot, and the text between markers into `mrkdwn` `section` blocks (it stops if a section goes over Slack's 3000-character limit; shorten that part);
- swaps `#general` for the clickable `<#CHANNELID>` form;
- keeps the whole text as the `text` fallback, which notifications and old clients show;
- sets `unfurl_links`/`unfurl_media` to `false`, because the screenshots are already inline and a link preview would only add a second card below them.

`$CHANNEL_ID` is the resolved channel ID from Step 2d (target of the post — may be `#testing` or another override). `$GENERAL_CH_ID` is always the `#general` channel ID, used only for body-mention substitution; keep them separate so trial posts to `#testing` still render the `#general` reference as a real channel link (clickable, navigates the reader to where the live post will end up).

### 6c — POST `chat.postMessage`

```bash
curl -sS -X POST \
  -H "Authorization: Bearer $SLACK_BOT_TOKEN" \
  -H 'Content-Type: application/json; charset=utf-8' \
  --data @"$payload" \
  https://slack.com/api/chat.postMessage > /tmp/slack-resp.json

rm -f "$payload"
```

Check `ok` in `/tmp/slack-resp.json`. On `ok: false`, map the `error` field:

- `not_in_channel` → tell user to invite the bot to `#general` (or grant `chat:write.public`). Saved draft stays. Stop.
- `invalid_auth` / `token_revoked` / `account_inactive` → tell user the token is bad. Saved draft stays. Stop.
- `channel_not_found` → tell user the resolved channel ID doesn't exist; suggest setting `SLACK_CHANNEL_ID` explicitly or refreshing the cache. Saved draft stays. Stop.
- `invalid_blocks` (often with `downloading image failed`) → an `image_url` is not publicly fetchable as an image. Re-check every marker URL with curl, fix or drop the marker, rebuild the payload. Saved draft stays. Stop.
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

### 6d — append the paper-trail to the saved draft

Insert a second HTML-comment line directly under the existing header, recording the permalink and an ISO timestamp:

```
<!-- Paste into #general in the letpeoplework Slack, or post via /release-social. -->
<!-- Posted: <permalink> at <ISO timestamp> -->
```

This is the marker Step 6a checks for to prevent double-posts.

## Step 7 — final report

Four-line summary:

1. Release version, headline count, bullet count, contributor count.
2. Slack identity resolution: handles resolved / unresolved (or "skipped — `SLACK_BOT_TOKEN` unset").
3. Slack draft path.
4. Slack post status: `posted` + permalink / `saved only` / `failed: <error>`. Only remind to paste manually if not posted ("paste `<path>` into `#general`").

## Guardrails

- Never echo or log `SLACK_BOT_TOKEN`. Always reference it as the literal `$SLACK_BOT_TOKEN` in command strings; let the shell substitute. Never `echo "$SLACK_BOT_TOKEN"`, never write it to a file, never include it in tool descriptions.
- Never post without an explicit user pick of `Use as-is and post` in Step 4. There is no auto-post mode and no `--yes` argument.
- Never double-post: if the saved Slack draft already has a `<!-- Posted: ... -->` line, refuse.
- Never delete the saved draft after a failed post — the user needs it to retry once they fix the config.
- Always gitignore `social-posts/.slack-cache.json` before writing it. The cache contains workspace user metadata.
- Never invent a Slack user ID. Unresolved names stay as bold plain text.
- Never overwrite an existing draft file without asking.
- Never restate the entire release notes verbatim. The post is a short summary.
- Never reference image URLs that aren't in the release block (no inventing assets).
- Drop the `Thanks to ...` line entirely if no contributors.
- Banned tone: "excited", "thrilled", "game-changing", "revolutionary", or other marketing fluff. Match the release-notes voice.
