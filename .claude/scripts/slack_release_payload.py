"""Turn a social-posts/<version>-slack.md draft into a chat.postMessage payload.

Usage: python3 -I slack_release_payload.py <draft.md> <target-channel-id> <general-channel-id>

A line `<!-- image: <public png url> | <alt text> -->` becomes a Block Kit image block
at that spot, and the text around it becomes mrkdwn section blocks. Other `<!-- ... -->`
lines are dropped. Link unfurling is off: an unfurl card would sit below the image and
Slack, not the draft, would choose which link it previews.
"""
import json, re, sys

draft, channel, general_id = sys.argv[1], sys.argv[2], sys.argv[3]
lines = open(draft, encoding="utf-8").read().splitlines()

blocks, chunk, fallback = [], [], []
def flush():
    text = "\n".join(chunk).strip()
    if text:
        assert len(text) <= 3000, f"section too long: {len(text)}"
        blocks.append({"type": "section", "text": {"type": "mrkdwn", "text": text}})
    chunk.clear()

for line in lines:
    m = re.match(r"^<!-- image: (\S+) \| (.+?) -->$", line)
    if m:
        flush()
        blocks.append({"type": "image", "image_url": m.group(1), "alt_text": m.group(2)})
        continue
    if re.match(r"^<!--.*-->$", line):
        continue
    line = re.sub(r"#general(?![A-Za-z0-9_])", f"<#{general_id}>", line)
    chunk.append(line)
    fallback.append(line)
flush()

print(json.dumps({
    "channel": channel,
    "text": "\n".join(fallback).strip(),
    "blocks": blocks,
    "unfurl_links": False,
    "unfurl_media": False,
}, ensure_ascii=False))
