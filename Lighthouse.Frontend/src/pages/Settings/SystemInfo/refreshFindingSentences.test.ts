import { describe, expect, it } from "vitest";
import type { RefreshFinding } from "../../../models/SystemInfo/RefreshLog";
import { sentenceFor } from "./refreshFindingSentences";

const bothWays = (named: string, seen: string[]): RefreshFinding => ({
	kind: "NamedLinkPointsBothWays",
	setting: "DependencyField",
	named,
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
				kind: "RelationTypeNotYetReadForDependencies",
			}),
		).toBeNull();
	});
});
