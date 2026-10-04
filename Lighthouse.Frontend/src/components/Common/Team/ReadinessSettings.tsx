import {
	Box,
	Checkbox,
	FormHelperText,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useId, useState } from "react";
import type {
	IDiscussWhenSetting,
	IReadinessSetting,
} from "../../../models/Refinement/Refinement";
import {
	isAtLeastOne,
	isAWholeNumber,
	shownNumber,
} from "../../../utils/numberField";

type DiscussionRule = keyof IDiscussWhenSetting;

const DEFAULT_DISCUSS_WHEN: Record<DiscussionRule, number> = {
	no: 1,
	yesIf: 2,
};

export const DEFAULT_READINESS: IReadinessSetting = {
	minYes: 3,
	minVoters: 3,
	discussWhen: DEFAULT_DISCUSS_WHEN,
};

const DISCUSSION_RULES: {
	rule: DiscussionRule;
	votes: string;
}[] = [
	{ rule: "no", votes: "No votes" },
	{ rule: "yesIf", votes: "“Yes, if…” votes" },
];

export const MIN_YES_ERROR = "At least one Yes vote is needed";
export const MIN_VOTERS_ERROR =
	"Voters needed cannot be fewer than Yes votes needed";
export const DISCUSSION_RULE_ERROR = "A discussion rule needs at least 1 vote";

const discussWhenOf = (readiness: IReadinessSetting): IDiscussWhenSetting =>
	readiness.discussWhen ?? DEFAULT_DISCUSS_WHEN;

const discussionRuleError = (threshold: number | null) =>
	threshold === null || isAtLeastOne(threshold) ? null : DISCUSSION_RULE_ERROR;

interface ReadinessErrors {
	minYes: string | null;
	minVoters: string | null;
	discussWhenNo: string | null;
	discussWhenYesIf: string | null;
}

// The same rule the server enforces, so a save it would refuse is never sent.
export const readinessErrors = (
	readiness: IReadinessSetting,
): ReadinessErrors => {
	const enoughYes = isAtLeastOne(readiness.minYes);
	const enoughVoters =
		isAWholeNumber(readiness.minVoters) &&
		readiness.minVoters >= readiness.minYes;
	const discussWhen = discussWhenOf(readiness);
	return {
		minYes: enoughYes ? null : MIN_YES_ERROR,
		minVoters: enoughYes && !enoughVoters ? MIN_VOTERS_ERROR : null,
		discussWhenNo: discussionRuleError(discussWhen.no),
		discussWhenYesIf: discussionRuleError(discussWhen.yesIf),
	};
};

export const hasReadinessErrors = (readiness: IReadinessSetting): boolean =>
	Object.values(readinessErrors(readiness)).some((error) => error !== null);

interface DiscussionRuleRowProps {
	votes: string;
	threshold: number | null;
	remembered: number;
	error: string | null;
	onToggle: (on: boolean) => void;
	onThresholdChange: (threshold: number) => void;
}

const DiscussionRuleRow: React.FC<DiscussionRuleRowProps> = ({
	votes,
	threshold,
	remembered,
	error,
	onToggle,
	onThresholdChange,
}) => {
	const errorId = useId();
	const on = threshold !== null;
	const shownThreshold = on ? threshold : remembered;
	return (
		<Grid size={{ xs: 12 }}>
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<Checkbox
					checked={on}
					onChange={(event) => onToggle(event.target.checked)}
					slotProps={{
						input: { "aria-label": `Send to discussion on ${votes}` },
					}}
				/>
				<TextField
					type="number"
					size="small"
					sx={{ width: 80 }}
					disabled={!on}
					value={shownNumber(shownThreshold)}
					onChange={(event) =>
						onThresholdChange(Number.parseInt(event.target.value, 10))
					}
					error={error !== null}
					slotProps={{
						htmlInput: {
							min: 1,
							step: 1,
							"aria-label": `${votes} that send to discussion`,
							"aria-describedby": error === null ? undefined : errorId,
						},
					}}
				/>
				<Typography>or more {votes}</Typography>
			</Box>
			{error !== null && (
				<FormHelperText id={errorId} error>
					{error}
				</FormHelperText>
			)}
		</Grid>
	);
};

interface ReadinessSettingsProps {
	readiness: IReadinessSetting;
	onChange: (readiness: IReadinessSetting) => void;
}

/** How many votes make a Work Item Ready, and how many send it to discussion instead. */
const ReadinessSettings: React.FC<ReadinessSettingsProps> = ({
	readiness,
	onChange,
}) => {
	const errors = readinessErrors(readiness);

	const changeNumber =
		(key: "minYes" | "minVoters") =>
		(event: React.ChangeEvent<HTMLInputElement>) =>
			onChange({
				...readiness,
				[key]: Number.parseInt(event.target.value, 10),
			});

	const discussWhen = discussWhenOf(readiness);
	const discussionErrors: Record<DiscussionRule, string | null> = {
		no: errors.discussWhenNo,
		yesIf: errors.discussWhenYesIf,
	};

	// A rule switched off and on again comes back at the threshold it had, not at the default.
	const [rememberedThresholds, setRememberedThresholds] = useState(() => ({
		no: discussWhen.no ?? DEFAULT_DISCUSS_WHEN.no,
		yesIf: discussWhen.yesIf ?? DEFAULT_DISCUSS_WHEN.yesIf,
	}));

	const changeDiscussWhen = (rule: DiscussionRule, threshold: number | null) =>
		onChange({
			...readiness,
			discussWhen: { ...discussWhen, [rule]: threshold },
		});

	const toggleRule = (rule: DiscussionRule) => (on: boolean) => {
		const current = discussWhen[rule];
		if (!on && current !== null && discussionRuleError(current) === null) {
			setRememberedThresholds({ ...rememberedThresholds, [rule]: current });
		}
		changeDiscussWhen(rule, on ? rememberedThresholds[rule] : null);
	};

	return (
		<>
			<Grid size={{ xs: 12 }}>
				<Typography variant="subtitle1">Readiness by votes</Typography>
			</Grid>
			<Grid size={{ xs: 12, sm: 6 }}>
				<TextField
					label="Yes votes needed"
					type="number"
					fullWidth
					value={shownNumber(readiness.minYes)}
					onChange={changeNumber("minYes")}
					error={errors.minYes !== null}
					helperText={errors.minYes}
					slotProps={{ htmlInput: { min: 1, step: 1 } }}
				/>
			</Grid>
			<Grid size={{ xs: 12, sm: 6 }}>
				<TextField
					label="Voters needed"
					type="number"
					fullWidth
					value={shownNumber(readiness.minVoters)}
					onChange={changeNumber("minVoters")}
					error={errors.minVoters !== null}
					helperText={errors.minVoters}
					slotProps={{ htmlInput: { min: 1, step: 1 } }}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<Typography variant="subtitle2">Send to discussion when</Typography>
			</Grid>
			{DISCUSSION_RULES.map(({ rule, votes }) => (
				<DiscussionRuleRow
					key={rule}
					votes={votes}
					threshold={discussWhen[rule]}
					remembered={rememberedThresholds[rule]}
					error={discussionErrors[rule]}
					onToggle={toggleRule(rule)}
					onThresholdChange={(threshold) => changeDiscussWhen(rule, threshold)}
				/>
			))}
		</>
	);
};

export default ReadinessSettings;
