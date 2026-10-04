import { Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import type { IStageRulesSetting } from "../../../models/Refinement/Refinement";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import {
	type IWorkItemRuleCondition,
	type IWorkItemRuleSchema,
	type IWorkItemRuleSet,
	RULE_SET_SCHEMA_VERSION,
} from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { DeliveryRuleBuilder } from "../DeliveryRuleBuilder/DeliveryRuleBuilder";
import {
	type DeliveryRuleGroupMode,
	isRuleConditionComplete,
} from "../DeliveryRuleBuilder/types";

export const NO_STAGE_RULES: IStageRulesSetting = {
	ready: null,
	beingRefined: null,
};

// A rule with no conditions left is a stage switched off, which the server stores as null.
export const stageRuleOf = (
	conditions: IWorkItemRuleCondition[],
	mode: DeliveryRuleGroupMode,
): IWorkItemRuleSet | null =>
	conditions.length === 0
		? null
		: { version: RULE_SET_SCHEMA_VERSION, mode, conditions };

// A condition still being written would match nothing, so the form holds the save until it is finished.
export const hasIncompleteStageRule = (
	stageRules: IStageRulesSetting | null | undefined,
): boolean =>
	[stageRules?.ready, stageRules?.beingRefined].some(
		(rule) => rule != null && !rule.conditions.every(isRuleConditionComplete),
	);

type Stage = keyof IStageRulesSetting;

const STAGES: { stage: Stage; title: string; outcome: string }[] = [
	{ stage: "ready", title: "Ready when", outcome: "Ready" },
	{
		stage: "beingRefined",
		title: "Being refined when",
		outcome: "being refined",
	},
];

// The fields a stage rule can look at are the Team's, so a Team that is not saved yet has none to offer.
const useWorkItemRuleSchema = (teamId: number) => {
	const { teamService } = useContext(ApiServiceContext);
	const [schema, setSchema] = useState<IWorkItemRuleSchema | null>(null);

	useEffect(() => {
		if (teamId <= 0) {
			return;
		}
		let cancelled = false;
		teamService
			.getForecastFilterSchema(teamId)
			.then((data) => {
				if (!cancelled) {
					setSchema(data);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setSchema(null);
				}
			});
		return () => {
			cancelled = true;
		};
	}, [teamId, teamService]);

	return teamId > 0 ? schema : null;
};

interface StageRuleGroupProps {
	title: string;
	emptyStateMessage: string;
	rule: IWorkItemRuleSet | null;
	schema: IWorkItemRuleSchema;
	onChange: (rule: IWorkItemRuleSet | null) => void;
}

const StageRuleGroup: React.FC<StageRuleGroupProps> = ({
	title,
	emptyStateMessage,
	rule,
	schema,
	onChange,
}) => {
	const mode = rule?.mode ?? "and";
	const conditions = rule?.conditions ?? [];
	return (
		<Grid size={{ xs: 12 }} role="group" aria-label={title}>
			<DeliveryRuleBuilder
				rules={conditions}
				onChange={(next) => onChange(stageRuleOf(next, mode))}
				mode={mode}
				onModeChange={(next) => onChange(stageRuleOf(conditions, next))}
				fields={schema.fields}
				operators={schema.operators}
				maxRules={schema.maxRules}
				maxValueLength={schema.maxValueLength}
				title={title}
				emptyStateMessage={emptyStateMessage}
			/>
		</Grid>
	);
};

interface StageRulesSettingsProps {
	teamId: number;
	stageRules: IStageRulesSetting;
	onChange: (stageRules: IStageRulesSetting) => void;
}

const StageRulesSettings: React.FC<StageRulesSettingsProps> = ({
	teamId,
	stageRules,
	onChange,
}) => {
	const { getTerm } = useTerminology();
	const ruleSchema = useWorkItemRuleSchema(teamId);
	if (ruleSchema === null) {
		return null;
	}

	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS).toLowerCase();
	return (
		<>
			<Grid size={{ xs: 12 }}>
				<Typography variant="subtitle1">Stages (optional)</Typography>
			</Grid>
			{STAGES.map(({ stage, title, outcome }) => (
				<StageRuleGroup
					key={stage}
					title={title}
					emptyStateMessage={`Add a rule to mark ${workItemsTerm} as ${outcome}.`}
					rule={stageRules[stage]}
					schema={ruleSchema}
					onChange={(rule) => onChange({ ...stageRules, [stage]: rule })}
				/>
			))}
		</>
	);
};

export default StageRulesSettings;
