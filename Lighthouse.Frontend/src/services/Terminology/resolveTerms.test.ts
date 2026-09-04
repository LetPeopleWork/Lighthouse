import { describe, expect, it } from "vitest";
import { resolveTerms } from "./resolveTerms";

const anInstanceThatCallsThem = (words: Record<string, string>) => {
	return (key: string) => words[key] ?? key;
};

const theInstance = anInstanceThatCallsThem({
	feature: "Deliverable",
	features: "Deliverables",
	team: "Squad",
});

describe("resolveTerms", () => {
	it("reads a token the product has a word for in the instance's own word", () => {
		expect(
			resolveTerms(
				"Let Lighthouse own the order of your {{features}}",
				theInstance,
			),
		).toBe("Let Lighthouse own the order of your Deliverables");
	});

	it("leaves a token the product has no word for exactly as written", () => {
		expect(
			resolveTerms("Arrange your {{fetaures}} yourself.", theInstance),
		).toBe("Arrange your {{fetaures}} yourself.");
	});

	it("reads every token in a sentence that carries several", () => {
		expect(
			resolveTerms(
				"A {{team}} owns one {{feature}} among many {{features}}",
				theInstance,
			),
		).toBe("A Squad owns one Deliverable among many Deliverables");
	});

	it("returns copy that carries no token byte for byte", () => {
		expect(
			resolveTerms(
				"Turning this off restores the previous behaviour immediately.",
				theInstance,
			),
		).toBe("Turning this off restores the previous behaviour immediately.");
	});

	it("returns an empty string for an empty string", () => {
		expect(resolveTerms("", theInstance)).toBe("");
	});
});
