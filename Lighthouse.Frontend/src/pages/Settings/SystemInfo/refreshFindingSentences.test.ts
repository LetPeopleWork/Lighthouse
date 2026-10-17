import { describe, expect, it } from "vitest";
import type {
	RefreshFinding,
	RefreshFindingKind,
	RefreshFindingSetting,
	RefreshLog,
} from "../../../models/SystemInfo/RefreshLog";
import {
	asOfLabelFor,
	findingsShownFor,
	sentenceFor,
} from "./refreshFindingSentences";

const bothWays = (named: string, seen: string[]): RefreshFinding => ({
	kind: "NamedLinkPointsBothWays",
	setting: "DependencyField",
	named,
	seen,
});

const matchedNothing = (seen: string[]): RefreshFinding => ({
	kind: "NamedLinkMatchedNothing",
	setting: "DependencyField",
	named: "depends on",
	seen,
});

describe("the sentence a refresh finding reads as", () => {
	it("a link type named by its name asks which of its two ends is meant", () => {
		expect(sentenceFor(bothWays("Blocks", ["blocks", "is blocked by"]))).toBe(
			'The Dependency Field names "Blocks", which points both ways. Name one end: "blocks" or "is blocked by".',
		);
	});

	it("a link type whose two ends share one phrase does not advise the name that failed", () => {
		expect(
			sentenceFor(bothWays("relates to", ["relates to", "Relates To"])),
		).toBe('The Dependency Field names "relates to", which points both ways.');
	});

	it("a kind this browser does not know reads as nothing", () => {
		expect(
			sentenceFor({
				...bothWays("Blocks", []),
				kind: "SomeLaterKind" as RefreshFindingKind,
			}),
		).toBeNull();
	});

	it("a setting this browser does not know reads as nothing", () => {
		expect(
			sentenceFor({
				...bothWays("Blocks", []),
				setting: "SomeLaterField" as RefreshFindingSetting,
			}),
		).toBeNull();
	});

	it.each<[RefreshFindingSetting, string]>([
		["ParentOverrideField", "Parent Override Field"],
		["DependencyField", "Dependency Field"],
		["SizeEstimateField", "Size Estimate Field"],
		["FeatureOwnerField", "Feature Owner Field"],
		["EstimationField", "Estimation Field"],
	])("the %s is named as the screen labels it", (setting, label) => {
		expect(sentenceFor({ ...bothWays("Blocks", []), setting })).toBe(
			`The ${label} names "Blocks", which points both ways.`,
		);
	});

	it.each([
		["one phrase", ["blocks"]],
		["three phrases", ["blocks", "is blocked by", "gates"]],
	])("a link type offering %s is not advised as two ends", (_, seen) => {
		expect(sentenceFor(bothWays("Blocks", seen))).toBe(
			'The Dependency Field names "Blocks", which points both ways.',
		);
	});

	it("a named link nothing carries is worded in the default term when none is given", () => {
		expect(sentenceFor(matchedNothing(["relates to"]))).toBe(
			'The Dependency Field names "depends on", and no Feature carries that link. Links seen: relates to.',
		);
	});

	it("a named link nothing carries, on Features carrying no links at all, lists none", () => {
		expect(sentenceFor(matchedNothing([]), "Epic")).toBe(
			'The Dependency Field names "depends on", and no Epic carries that link.',
		);
	});

	it("a relation type with no direction is advised a type with two ends", () => {
		expect(
			sentenceFor({
				kind: "RelationTypeHasNoDirection",
				setting: "DependencyField",
				named: "System.LinkTypes.Related",
				seen: [],
			}),
		).toBe(
			'The Dependency Field names "System.LinkTypes.Related", which has no direction. Name a relation type with two ends, such as "System.LinkTypes.Dependency-Reverse".',
		);
	});

	it("a relation type named without its end is advised both of its ends, built from what was typed", () => {
		expect(
			sentenceFor({
				kind: "RelationTypeNamedWithoutItsEnd",
				setting: "DependencyField",
				named: "Contoso.DependsOn",
				seen: ["contoso.dependson-forward", "contoso.dependson-reverse"],
			}),
		).toBe(
			'The Dependency Field names "Contoso.DependsOn" without its end. Name one end: "Contoso.DependsOn-Forward" or "Contoso.DependsOn-Reverse".',
		);
	});
});

describe("the date a refresh finding is shown as of", () => {
	it.each([
		["01", "Jan"],
		["02", "Feb"],
		["03", "Mar"],
		["04", "Apr"],
		["05", "May"],
		["06", "Jun"],
		["07", "Jul"],
		["08", "Aug"],
		["09", "Sep"],
		["10", "Oct"],
		["11", "Nov"],
		["12", "Dec"],
	])("a full download in month %s is named %s", (month, name) => {
		expect(asOfLabelFor(`2026-${month}-06T07:05:00`)).toBe(
			`as of the full download on 6 ${name}, 07:05`,
		);
	});

	it("a time that cannot be read gives no label", () => {
		expect(asOfLabelFor("not a time")).toBeNull();
	});
});

describe("the findings a Portfolio's refresh history shows", () => {
	const aRow = (
		id: number,
		findings: RefreshFinding[] | undefined,
	): RefreshLog => ({
		id,
		type: "Portfolio",
		entityId: 5,
		entityName: "Payments Platform",
		itemCount: 3,
		durationMs: 400,
		executedAt: "2026-10-16T10:00:00",
		success: true,
		cancelled: false,
		...(findings === undefined
			? {}
			: {
					latestFullRunFindings: findings,
					latestFullRunAt: "2026-10-16T09:12:00",
				}),
	});

	it("are read from the first row that carries any, passing over rows that carry none", () => {
		expect(
			findingsShownFor(
				[aRow(1, undefined), aRow(2, [matchedNothing([])])],
				"Epic",
			),
		).toEqual({
			sentences: [
				'The Dependency Field names "depends on", and no Epic carries that link.',
			],
			asOfLabel: "as of the full download on 16 Oct, 09:12",
		});
	});

	it("leave out a finding this browser cannot word", () => {
		const unknown: RefreshFinding = {
			...matchedNothing([]),
			kind: "SomeLaterKind" as RefreshFindingKind,
		};

		expect(
			findingsShownFor([aRow(1, [unknown, matchedNothing([])])], "Epic")
				.sentences,
		).toEqual([
			'The Dependency Field names "depends on", and no Epic carries that link.',
		]);
	});

	it("are none, with no label, when only findings this browser cannot word were recorded", () => {
		const unknown: RefreshFinding = {
			...matchedNothing([]),
			kind: "SomeLaterKind" as RefreshFindingKind,
		};

		expect(findingsShownFor([aRow(1, [unknown])], "Epic")).toEqual({
			sentences: [],
			asOfLabel: null,
		});
	});
});
