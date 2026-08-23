import { describe, expect, it } from "vitest";
import { NOT_HONOURED_REASONS } from "../../models/FeatureDependency";
import {
	type DependencyTerms,
	positionedBelowSentence,
	reasonSentence,
	withheldName,
} from "./dependencySentences";

const terms: DependencyTerms = {
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
};

// Every renamed word is a word an instance may already use for something else, so the sentences are
// built from the instance's own vocabulary rather than written out.
const renamedTerms: DependencyTerms = {
	featureTerm: "Initiative",
	portfolioTerm: "Programme",
};

describe("reasonSentence", () => {
	// One sentence per reason. A reader meeting the same words for two different reasons has been told
	// nothing: the whole point of the reason is that it says which of them applies.
	it("says something different for every reason there is", () => {
		const sentences = NOT_HONOURED_REASONS.map((reason) =>
			reasonSentence(reason, "Warehouse sync", terms),
		);

		expect(new Set(sentences).size).toBe(NOT_HONOURED_REASONS.length);
	});

	it("says a Feature outside every shared Portfolio is left out of the forecast", () => {
		expect(
			reasonSentence("OutsideThisPortfolio", "Warehouse sync", terms),
		).toBe(
			"This Feature depends on Warehouse sync, which is in no Portfolio they share. That dependency is not included in the forecast.",
		);
	});

	it("says two Features waiting on each other are left out of the forecast", () => {
		expect(reasonSentence("InALoop", "Warehouse sync", terms)).toBe(
			"This Feature and Warehouse sync are waiting on each other. That dependency is not included in the forecast.",
		);
	});

	it("says a Feature with nothing measured behind it cannot be given a date", () => {
		expect(
			reasonSentence("BlockerCannotBeForecast", "Warehouse sync", terms),
		).toBe(
			"Warehouse sync has no measured delivery to forecast from, so the wait cannot be given a date. That dependency is not included in the forecast.",
		);
	});

	it("says what is missing is a premium licence, and what it would account for", () => {
		expect(reasonSentence("NotLicensed", "Warehouse sync", terms)).toBe(
			"This Feature depends on Warehouse sync, and that wait is not accounted for in the dates. A premium licence accounts for it.",
		);
	});

	it("names the Portfolio in the instance's own word for one", () => {
		expect(
			reasonSentence("OutsideThisPortfolio", "Warehouse sync", renamedTerms),
		).toBe(
			"This Initiative depends on Warehouse sync, which is in no Programme they share. That dependency is not included in the forecast.",
		);
	});

	// The one reason that is nobody's problem to fix, and the only one that does not name the Feature
	// waited on: the reader is looking at that name already, and the reason is the same for every entry
	// in the Portfolio.
	it("says a dependency set aside was set aside, and stops there", () => {
		expect(reasonSentence("IgnoredByPortfolio", "Warehouse sync", terms)).toBe(
			"Portfolio is set to ignore dependencies.",
		);
	});

	it("uses the words this instance calls things by", () => {
		const sentence = reasonSentence(
			"IgnoredByPortfolio",
			"Warehouse sync",
			renamedTerms,
		);

		expect(sentence).toContain("Programme");
		expect(sentence).not.toContain("Portfolio");
	});

	// The word this product reserves for an item held up right now is renameable, and both meanings
	// would follow one rename onto the same screen.
	it("never borrows the word an instance may already have renamed for board-blocked work", () => {
		for (const reason of NOT_HONOURED_REASONS) {
			expect(reasonSentence(reason, "Warehouse sync", terms)).not.toMatch(
				/block/i,
			);
		}
	});
});

describe("positionedBelowSentence", () => {
	it("says where the Feature waited on sits, and nothing about leaving it out", () => {
		const sentence = positionedBelowSentence("Warehouse sync", terms);

		expect(sentence).toBe(
			"This Feature depends on Warehouse sync, which sits below it in the order.",
		);
		expect(sentence).not.toContain("not included in the forecast");
	});
});

describe("withheldName", () => {
	it("names a Feature the reader may not see by what they can be told about it", () => {
		expect(withheldName(terms)).toBe("a Feature you do not have access to");
		expect(withheldName(renamedTerms)).toContain("Initiative");
	});
});
