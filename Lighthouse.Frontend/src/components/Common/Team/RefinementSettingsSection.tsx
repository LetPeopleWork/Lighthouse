import {
	Box,
	Checkbox,
	FormHelperText,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useId, useRef, useState } from "react";
import type {
	IDiscussWhenSetting,
	IReadinessSetting,
	IRefinementStateSetting,
} from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import InputGroup from "../InputGroup/InputGroup";
import ItemListManager from "../ItemListManager/ItemListManager";

interface RefinementSettingsSectionProps {
	toDoStates: string[];
	doingStates: string[];
	chosenStates: IRefinementStateSetting[];
	onChange: (states: IRefinementStateSetting[]) => void;
	readiness: IReadinessSetting;
	onReadinessChange: (readiness: IReadinessSetting) => void;
}

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
	threshold === null || (Number.isInteger(threshold) && threshold >= 1)
		? null
		: DISCUSSION_RULE_ERROR;

// The same rule the server enforces, so a save it would refuse is never sent.
export const readinessErrors = (
	readiness: IReadinessSetting,
): {
	minYes: string | null;
	minVoters: string | null;
	discussWhenNo: string | null;
	discussWhenYesIf: string | null;
} => {
	const enoughYes = Number.isInteger(readiness.minYes) && readiness.minYes >= 1;
	const enoughVoters =
		Number.isInteger(readiness.minVoters) &&
		readiness.minVoters >= readiness.minYes;
	const discussWhen = discussWhenOf(readiness);
	return {
		minYes: enoughYes ? null : MIN_YES_ERROR,
		minVoters: enoughYes && !enoughVoters ? MIN_VOTERS_ERROR : null,
		discussWhenNo: discussionRuleError(discussWhen.no),
		discussWhenYesIf: discussionRuleError(discussWhen.yesIf),
	};
};

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
					value={Number.isNaN(shownThreshold) ? "" : shownThreshold}
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

const toSettings = (states: string[]): IRefinementStateSetting[] =>
	states.map((state) => ({ state }));

const RefinementSettingsSection: React.FC<RefinementSettingsSectionProps> = ({
	toDoStates,
	doingStates,
	chosenStates,
	onChange,
	readiness,
	onReadinessChange,
}) => {
	const { getTerm } = useTerminology();

	const suggestions = [...toDoStates, ...doingStates];
	const offered = new Set(suggestions.map((state) => state.toLowerCase()));
	const chosen = chosenStates
		.map((entry) => entry.state)
		.filter((state) => offered.has(state.toLowerCase()));

	const save = (states: string[]) => onChange(toSettings(states));

	// Only To Do and Doing states can mean refinement, so a chosen state leaves as soon as the admin stops
	// offering it, and the same save that changes the states carries the shorter list. Opening the form is
	// not an edit: a stored state that is not offered is only hidden, and the server drops it on any save.
	const offeredKey = [...offered].join("\n");
	const lastOfferedKey = useRef(offeredKey);
	const anyNoLongerOffered = chosen.length < chosenStates.length;
	useEffect(() => {
		if (lastOfferedKey.current === offeredKey) {
			return;
		}
		lastOfferedKey.current = offeredKey;
		if (anyNoLongerOffered) {
			onChange(toSettings(chosen));
		}
	}, [offeredKey, anyNoLongerOffered, chosen, onChange]);

	// The list accepts free text, but the server refuses anything that is not a To Do or Doing state,
	// so a typed value only counts when it names one of the suggestions.
	const add = (typed: string) => {
		const wanted = typed.trim().toLowerCase();
		const match = suggestions.find((state) => state.toLowerCase() === wanted);
		if (match && !chosen.includes(match)) {
			save([...chosen, match]);
		}
	};

	const remove = (state: string) =>
		save(chosen.filter((name) => name !== state));

	const refinementTerm = getTerm(TERMINOLOGY_KEYS.REFINEMENT);
	const errors = readinessErrors(readiness);

	const changeNumber =
		(key: "minYes" | "minVoters") =>
		(event: React.ChangeEvent<HTMLInputElement>) =>
			onReadinessChange({
				...readiness,
				[key]: Number.parseInt(event.target.value, 10),
			});

	const shown = (value: number) => (Number.isNaN(value) ? "" : value);

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
		onReadinessChange({
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
		<InputGroup title={refinementTerm}>
			<Grid size={{ xs: 12 }}>
				<ItemListManager
					title={`${refinementTerm} State`}
					items={chosen}
					onAddItem={add}
					onRemoveItem={remove}
					suggestions={suggestions}
					isLoading={false}
				/>
			</Grid>
			<Grid size={{ xs: 12 }}>
				<Typography variant="subtitle1">Readiness by votes</Typography>
			</Grid>
			<Grid size={{ xs: 12, sm: 6 }}>
				<TextField
					label="Yes votes needed"
					type="number"
					fullWidth
					value={shown(readiness.minYes)}
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
					value={shown(readiness.minVoters)}
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
		</InputGroup>
	);
};

export default RefinementSettingsSection;
