import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

/**
 * Story #5587 (ADR-113), slice-04 — the release notes and the concept page explain why the number
 * moved. D3: docs only, no in-app messaging.
 *
 * WHAT THIS IS, stated plainly so nobody mistakes it for more than it is. Slice-04 is a prose slice.
 * No test can judge whether an explanation explains. These are DRIFT GUARDS: they fail if the section
 * is never written, if it is later deleted, or if it makes the one claim D1 constraint B forbids.
 * They cannot tell good prose from bad, and they are not the slice's quality gate. That gate is the
 * maintainer walking the worked example end to end against the running demo instance and arriving at
 * the displayed rounded percentage (slice-04 gate 1), plus the DIVIO/Diataxis review of the prose.
 *
 * The keyword choices are deliberately the ones the ACs make unavoidable — "backfill" because AC-04.1
 * requires saying the recorded trend cannot be backfilled, and the literal indicator text "not enough
 * data" because AC-04.2 is about that indicator. If DELIVER says the same thing in different words it
 * may move a keyword, provided the assertion stays falsifiable: a check that passes on an empty
 * section is worse than no check.
 *
 * `describe.skip` = RED scaffold; DELIVER enables it (ADR-025). The second block is NOT skipped — it
 * is the "must not" half, which can fail today and must stay green through the change.
 *
 * Placement: beside formatLikelihood.enforcement.test.ts, which is this repo's only precedent for a
 * readFileSync-plus-regex enforcement test. Not under src/docs — a `src/docs` path in this project has
 * a history of Biome reformatting the entire docs tree.
 */

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(here, "../../../..");

const conceptPage = resolve(
	repoRoot,
	"docs/concepts/howlighthouseforecasts.md",
);
const releaseNotes = resolve(repoRoot, "docs/releasenotes/releasenotes.md");
const deliverySection = resolve(
	here,
	"../../pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx",
);

function read(path: string): string {
	return readFileSync(path, "utf8");
}

/** The unreleased section of the notes — everything above the first shipped version heading. */
function vNextSection(): string {
	const notes = read(releaseNotes);
	const start = notes.indexOf("# Lighthouse vNext");
	expect(start, "release notes have no vNext section").toBeGreaterThanOrEqual(
		0,
	);

	const next = notes.indexOf("\n# Lighthouse v", start + 1);
	return next === -1 ? notes.slice(start) : notes.slice(start, next);
}

/**
 * The concept page's DELIVERY-level section — the one slice-04 adds, sliced from its heading to the
 * next heading of the same or higher level.
 *
 * Every content assertion below is scoped to it rather than to the whole page, and that is the point:
 * the page ALREADY teaches independence, the coin analogy and a 72 % worked example at FEATURE grain,
 * so a page-wide keyword check passes today and can never go red. Three of these checks were written
 * page-wide first and observed passing against the unchanged page — recorded in
 * distill-red-classification.md, because a check that cannot fail is worse than no check.
 */
function deliveryGrainSection(): string {
	const page = read(conceptPage);
	const heading = /^(#{2,4}) .*deliver.*$/im.exec(page);

	if (heading === null) {
		return "";
	}

	const rest = page.slice(heading.index + heading[0].length);
	const nextHeading = new RegExp(`^#{1,${heading[1].length}} `, "m").exec(rest);

	return nextHeading === null ? rest : rest.slice(0, nextHeading.index);
}

describe.skip("delivery joint likelihood is explained in the docs (Story #5587 slice-04)", () => {
	it("names all three visible consequences in the release notes (AC-04.1)", () => {
		const section = vNextSection();

		// 1. The number drops, because it used to reflect only the governing feature.
		expect(section).toMatch(/governing/i);
		// 2. The percentile dates move outward — the most under-communicated consequence.
		expect(section).toMatch(/percentile|forecast date/i);
		// 3. The recorded trend steps once and CANNOT be backfilled (forward-only, ADR-048/049).
		expect(section).toMatch(/backfill/i);
	});

	it("calls the sufficiency change out separately (AC-04.2)", () => {
		const section = vNextSection();

		expect(section).toMatch(/not enough data/i);
		expect(section).toMatch(/every|all/i);
	});

	it("adds a delivery-level worked example to the concept page (AC-04.3)", () => {
		const page = read(conceptPage);

		// Alongside the existing "2 Teams - 1 Feature" / "Doing it by hand" walkthrough, one grain up.
		expect(page).toMatch(/^#{2,4} .*deliver/im);
		expect(page).toMatch(/Doing it by hand/);
	});

	it("teaches the per-team-per-feature grain (AC-04.4)", () => {
		// D5: a feature worked by two teams contributes one row to EACH team's bucket, which is why a
		// shared feature is not penalised twice.
		expect(deliveryGrainSection()).toMatch(/twice|double|each team/i);
	});

	it("restates the independence assumption at delivery grain (AC-04.5)", () => {
		// The page already says this at FEATURE grain in exactly these terms — extend, do not re-argue.
		// Scoped to the new section precisely because the feature-grain statement already exists.
		expect(deliveryGrainSection()).toMatch(/independen|shared people/i);
	});

	it("shows the equality case (AC-04.6)", () => {
		// The three-way fixture: A/F1 = 0.90, B/F1 = 0.80, B/F2 = 0.95 => delivery 0.720, with rows
		// 0.72 and 0.95. The delivery EQUALS F1's row, and the prose has to say so — the numbers alone
		// leave the reader to notice it.
		const section = deliveryGrainSection();

		expect(section).toMatch(/0\.72|72\s?%/);
		expect(section).toMatch(/0\.95|95\s?%/);
		expect(section).toMatch(/equal|the same as/i);
	});
});

describe("delivery joint likelihood docs must not overclaim (Story #5587 slice-04)", () => {
	it("never claims the delivery is always lower than every feature (AC-04.6, D1 constraint B)", () => {
		// Vacuous until the delivery-level section lands, and labelled as such — it sits outside the
		// skipped block so it is already running when DELIVER writes the first draft. Equality is
		// legitimate (D5) and is exactly what the three-way fixture renders, so "always lower than
		// every Feature" would be a false statement published live on letpeople.work the moment it
		// merges (docs/ is hot-linked from @main via jsDelivr).
		const page = read(conceptPage);

		expect(page).not.toMatch(/always lower than (every|each|any)/i);
		expect(page).not.toMatch(/lower than (every|each|any) (feature|epic)/i);
	});

	it("adds no in-app banner or dismissible notice to the delivery surface (AC-04.7, D3)", () => {
		// D3 chose docs-only. The failure this catches is a well-meant "why did my number change?"
		// banner appearing in DELIVER, which is the one thing the decision ruled out — no dismissible-
		// notice mechanism exists and building one for a single-release message is disproportionate.
		// Scoped to DeliverySection: the trend-annotation half of AC-04.7 lives in the chart components
		// and is a diff-review item, not a greppable one.
		const source = read(deliverySection);

		expect(source).not.toMatch(/\bAlert\b/);
		expect(source).not.toMatch(/\bSnackbar\b/);
		expect(source).not.toMatch(/dismiss/i);
	});
});
