import type {
	RefreshFinding,
	RefreshLog,
} from "../../../models/SystemInfo/RefreshLog";
import { isValidDate } from "../../../utils/date/isValidDate";

// Named as each setting's own label reads, which for the owner field carries this instance's word for a Feature.
const SETTING_NAMES: Readonly<Record<string, (featureTerm: string) => string>> =
	{
		ParentOverrideField: () => "Parent Override Field",
		DependencyField: () => "Dependency Field",
		SizeEstimateField: () => "Size Estimate Field",
		FeatureOwnerField: (featureTerm) => `${featureTerm} Owner Field`,
		EstimationField: () => "Estimation Field",
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

const matchedNothing = (
	settingName: string,
	finding: RefreshFinding,
	featureTerm: string,
) => {
	const statement = `The ${settingName} names "${finding.named}", and no ${featureTerm} carries that link.`;
	if (finding.seen.length === 0) {
		return statement;
	}
	return `${statement} Links seen: ${finding.seen.join(", ")}.`;
};

type FindingSentence = (
	settingName: string,
	finding: RefreshFinding,
	featureTerm: string,
) => string;

const cannotBeReadHere = (settingName: string) =>
	`The ${settingName} points at an Azure DevOps relation type, which only the Dependency Field reads.`;

const SENTENCES: Readonly<Record<string, FindingSentence>> = {
	NamedLinkPointsBothWays: pointsBothWays,
	NamedLinkMatchedNothing: matchedNothing,
	RelationTypeCannotBeReadHere: cannotBeReadHere,
};

// Rows outlive the build that wrote them, so a kind or setting this browser does not know reads as
// nothing rather than as a sentence it cannot word.
export const sentenceFor = (
	finding: RefreshFinding,
	featureTerm = "Feature",
): string | null => {
	const settingName = SETTING_NAMES[finding.setting];
	const sentence = SENTENCES[finding.kind];
	if (settingName === undefined || sentence === undefined) {
		return null;
	}
	return sentence(settingName(featureTerm), finding, featureTerm);
};

const MONTHS = [
	"Jan",
	"Feb",
	"Mar",
	"Apr",
	"May",
	"Jun",
	"Jul",
	"Aug",
	"Sep",
	"Oct",
	"Nov",
	"Dec",
];

const twoDigits = (value: number) => String(value).padStart(2, "0");

// Read on the viewer's own calendar and clock, so the label names the download as they saw it run.
export const asOfLabelFor = (fullRunAt: string | undefined): string | null => {
	const at = fullRunAt === undefined ? null : new Date(fullRunAt);
	if (!isValidDate(at)) {
		return null;
	}
	return `as of the full download on ${at.getDate()} ${MONTHS[at.getMonth()]}, ${twoDigits(at.getHours())}:${twoDigits(at.getMinutes())}`;
};

// Every row of an entity carries the same latest full download, so the first that carries one speaks
// for all of them. The date goes only beside a sentence: on its own it would date an answer nobody
// was shown.
export const findingsShownFor = (
	rows: readonly RefreshLog[],
	featureTerm: string,
): { sentences: string[]; asOfLabel: string | null } => {
	const latestFullRun = rows.find(
		(row) => row.latestFullRunFindings !== undefined,
	);
	const sentences = (latestFullRun?.latestFullRunFindings ?? [])
		.map((finding) => sentenceFor(finding, featureTerm))
		.filter((sentence) => sentence !== null);
	if (sentences.length === 0) {
		return { sentences, asOfLabel: null };
	}
	return { sentences, asOfLabel: asOfLabelFor(latestFullRun?.latestFullRunAt) };
};
