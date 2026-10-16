import type { RefreshFinding } from "../../../models/SystemInfo/RefreshLog";

const SETTING_NAMES: Readonly<Record<string, string>> = {
	ParentOverrideField: "Parent Override Field",
	DependencyField: "Dependency Field",
	SizeEstimateField: "Size Estimate Field",
	FeatureOwnerField: "Feature Owner Field",
	EstimationField: "Estimation Field",
};

const twoDistinctEnds = (seen: readonly string[]) =>
	seen.length === 2 && seen[0].toLowerCase() !== seen[1].toLowerCase();

// A type whose two ends share one phrase has no end to name instead, so advising one would repeat
// the very name that failed.
const pointsBothWays = (settingName: string, finding: RefreshFinding) => {
	const statement = `The ${settingName} names "${finding.named}", which points both ways.`;
	if (!twoDistinctEnds(finding.seen)) {
		return statement;
	}
	return `${statement} Name one end: "${finding.seen[0]}" or "${finding.seen[1]}".`;
};

const SENTENCES: Readonly<
	Record<string, (settingName: string, finding: RefreshFinding) => string>
> = {
	NamedLinkPointsBothWays: pointsBothWays,
};

// Rows outlive the build that wrote them, so a kind or setting this browser does not know reads as
// nothing rather than as a sentence it cannot word.
export const sentenceFor = (finding: RefreshFinding): string | null => {
	const settingName = SETTING_NAMES[finding.setting];
	const sentence = SENTENCES[finding.kind];
	if (settingName === undefined || sentence === undefined) {
		return null;
	}
	return sentence(settingName, finding);
};
