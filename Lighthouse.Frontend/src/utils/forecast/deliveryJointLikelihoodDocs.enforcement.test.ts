import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

/**
 * The release notes and the concept page explain why the delivery number moved. The change is
 * explained in the docs only, with no in-app messaging.
 *
 * WHAT THIS IS, stated plainly so nobody mistakes it for more than it is. This is a prose change.
 * No test can judge whether an explanation explains. These are DRIFT GUARDS: they fail if the section
 * is never written, if it is later deleted, or if it makes the one claim the explanation must never make.
 * They cannot tell good prose from bad, and they are not the quality gate. That gate is the
 * maintainer walking the worked example end to end against the running demo instance and arriving at
 * the displayed rounded percentage (the first gate), plus the DIVIO/Diataxis review of the prose.
 *
 * The keyword choices are deliberately the ones the explanation makes unavoidable — "backfill" because the notes
 * must say the recorded trend cannot be backfilled, and the literal indicator text "not enough
 * data" because the notes must call out that indicator. If the docs say the same thing in different words it
 * may move a keyword, provided the assertion stays falsifiable: a check that passes on an empty
 * section is worse than no check.
 *
 * The second block is the "must not" half: it fails if the docs ever make the forbidden claim.
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
const deliveryGridFolder = resolve(
	here,
	"../../pages/Portfolios/Detail/Components/DeliveryGrid",
);
const deliverySectionFiles = [
	"DeliverySection.tsx",
	"UnbindConfirmationDialog.tsx",
	"WorkItemsTab.tsx",
	"deliveryFeatureRow.tsx",
	"deliveryHeaderCopy.tsx",
].map((file) => resolve(deliveryGridFolder, file));

function read(path: string): string {
	return readFileSync(path, "utf8");
}

/**
 * The release notes, whole file.
 *
 * This was scoped to the `# Lighthouse vNext` section until 2026-08-02, which made the guard
 * self-destructing: vNext becomes `# Lighthouse v<version>` the moment a release is cut, so the
 * section it asserted against ceased to exist and every check went red on an unrelated push. The
 * shipped wording is what has to survive, and it survives wherever in the notes it ends up.
 */
function releaseNotesText(): string {
	const notes = read(releaseNotes);
	expect(notes, "release notes are empty").not.toHaveLength(0);
	return notes;
}

/**
 * The concept page's DELIVERY-level section — the one this change adds, sliced from its heading to the
 * next heading of the same or higher level.
 *
 * Every content assertion below is scoped to it rather than to the whole page, and that is the point:
 * the page ALREADY teaches independence, the coin analogy and a 72 % worked example at FEATURE grain,
 * so a page-wide keyword check passes today and can never go red. Three of these checks were written
 * page-wide first and observed passing against the unchanged page, so none of them could fail before this change. That is why
 * they are scoped here: a check that cannot fail is worse than no check.
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

describe("delivery joint likelihood is explained in the docs", () => {
	it("names all three visible consequences in the release notes", () => {
		const notes = releaseNotesText();

		// 1. The number drops, because it used to reflect only the governing feature. Shipped as
		//    "one Feature governs it entirely", so match the stem rather than the first draft's keyword.
		expect(notes).toMatch(/govern/i);
		// 2. The percentile dates move outward — the most under-communicated consequence.
		expect(notes).toMatch(/percentile|forecast date/i);
		// 3. The recorded trend steps once and CANNOT be backfilled (the trend is forward-only).
		//    Shipped as "past points cannot be recomputed".
		expect(notes).toMatch(/backfill|cannot be recomputed/i);
	});

	it("calls the sufficiency change out separately", () => {
		const notes = releaseNotesText();

		// The indicator shipped reading "Cannot forecast", not the first draft's "not enough data".
		expect(notes).toMatch(/not enough data|cannot forecast/i);
		expect(notes).toMatch(/every|all/i);
	});

	it("adds a delivery-level worked example to the concept page", () => {
		const page = read(conceptPage);

		// Alongside the existing "2 Teams - 1 Feature" / "Doing it by hand" walkthrough, one grain up.
		expect(page).toMatch(/^#{2,4} .*deliver/im);
		expect(page).toMatch(/Doing it by hand/);
	});

	it("teaches the per-team-per-feature grain", () => {
		// A feature worked by two teams contributes one row to EACH team's bucket, which is why a
		// shared feature is not penalised twice.
		expect(deliveryGrainSection()).toMatch(/twice|double|each team/i);
	});

	it("restates the independence assumption at delivery grain", () => {
		// The page already says this at FEATURE grain in exactly these terms — extend, do not re-argue.
		// Scoped to the new section precisely because the feature-grain statement already exists.
		expect(deliveryGrainSection()).toMatch(/independen|shared people/i);
	});

	it("shows the equality case", () => {
		// The three-way fixture: A/F1 = 0.90, B/F1 = 0.80, B/F2 = 0.95 => delivery 0.720, with rows
		// 0.72 and 0.95. The delivery EQUALS F1's row, and the prose has to say so — the numbers alone
		// leave the reader to notice it.
		const section = deliveryGrainSection();

		expect(section).toMatch(/0\.72|72\s?%/);
		expect(section).toMatch(/0\.95|95\s?%/);
		expect(section).toMatch(/equal|the same as/i);
	});
});

describe("delivery joint likelihood docs must not overclaim", () => {
	it("never claims the delivery is always lower than every feature", () => {
		// Vacuous until the delivery-level section lands, and labelled as such — it sits outside the
		// skipped block so it is already running when the first draft is written. Equality
		// is legitimate and is exactly what the three-way fixture renders, so "always lower than
		// every Feature" would be a false statement published live on letpeople.work the moment it
		// merges (docs/ is hot-linked from @main via jsDelivr).
		const page = read(conceptPage);

		expect(page).not.toMatch(/always lower than (every|each|any)/i);
		expect(page).not.toMatch(/lower than (every|each|any) (feature|epic)/i);
	});

	it("adds no in-app banner or dismissible notice to the delivery surface", () => {
		// The change is explained in the docs only, so no in-app message may appear. This catches a
		// well-meant "why did my number change?" banner: no dismissible-notice mechanism exists, and
		// building one for a single-release message is disproportionate. The check covers the
		// Delivery section and the files split out of it. The trend-annotation part lives in the
		// chart components and is checked by review, not by this test.
		for (const file of deliverySectionFiles) {
			const source = read(file);

			expect(source, file).not.toMatch(/\bAlert\b/);
			expect(source, file).not.toMatch(/\bSnackbar\b/);
			expect(source, file).not.toMatch(/dismiss/i);
		}
	});
});
