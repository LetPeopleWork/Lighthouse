import { Box, Typography } from "@mui/material";
import type React from "react";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import {
	type ConnectionDefaultName,
	type EstimationDefaultName,
	type FieldDefaultName,
	type IConnectionDefaults,
	type IEstimationDefault,
	isEstimationDefaultName,
	isParentSourcesDefaultName,
	type ParentSourcesDefaultName,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	directionTip,
	directionTipTermsFrom,
	parentSourcesTip,
} from "../ConnectionDefault/fieldDirectionTips";
import ParentSourcesList from "../ConnectionDefault/ParentSourcesList";
import { ownerOfParentSourcesDefault } from "../ConnectionDefault/parentSources";
import FormSelectField from "../FormSelectField";
import InputGroup from "../InputGroup/InputGroup";
import EstimationDefaultEditor from "./EstimationDefaultEditor";
import {
	type DefaultsGroup,
	fieldOfDefault,
	parentSourcesOfDefault,
	WhereEachDefaultSits,
} from "./whatASaveAsks";

interface ConnectionDefaultsSectionProps {
	supportedDefaults: ConnectionDefaultName[];
	defaults: IConnectionDefaults;
	additionalFields: IAdditionalFieldDefinition[];
	onChange: (name: FieldDefaultName, fieldId: number | null) => void;
	onEstimationChange: (
		name: EstimationDefaultName,
		bundle: IEstimationDefault | null,
	) => void;
	onParentSourcesChange: (
		name: ParentSourcesDefaultName,
		list: readonly ParentSourceEntry[],
	) => void;
	/** Only a saved field can be named by a default, so a field added in this edit is not offered yet. */
	savedFieldIds: ReadonlySet<number>;
	tracker: WorkTrackingSystemType | undefined;
}

const ConnectionDefaultsSection: React.FC<ConnectionDefaultsSectionProps> = ({
	supportedDefaults,
	defaults,
	additionalFields,
	onChange,
	onEstimationChange,
	onParentSourcesChange,
	savedFieldIds,
	tracker,
}) => {
	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const tipTerms = directionTipTermsFrom(getTerm);
	const title = `Defaults for ${teamsTerm} and ${portfoliosTerm}`;

	const fieldOptions = additionalFields.map((field) => ({
		id: field.id,
		label: field.displayName,
	}));
	const fieldIds = new Set(additionalFields.map((field) => field.id));
	// A field removed from the list above has no entry to show, so its default reads as None.
	const chosenFieldOf = (name: ConnectionDefaultName): number | "" => {
		const fieldId = fieldOfDefault(name, defaults);
		return fieldId != null && fieldIds.has(fieldId) ? fieldId : "";
	};

	const chosenFieldIdOf = (name: ConnectionDefaultName): number | null => {
		const chosen = chosenFieldOf(name);
		return chosen === "" ? null : chosen;
	};

	const savedFields = additionalFields.filter((field) =>
		savedFieldIds.has(field.id),
	);

	const groups: { group: DefaultsGroup; heading: string }[] = [
		{ group: "teams", heading: teamsTerm },
		{ group: "portfolios", heading: portfoliosTerm },
	];
	const sections = groups
		.map(({ group, heading }) => ({
			heading,
			rows: supportedDefaults.filter(
				(name) => WhereEachDefaultSits[name].group === group,
			),
		}))
		.filter(({ rows }) => rows.length > 0);

	const rowOf = (name: ConnectionDefaultName) => {
		if (isEstimationDefaultName(name)) {
			return (
				<EstimationDefaultEditor
					key={name}
					bundle={defaults[name]}
					chosenField={chosenFieldIdOf(name)}
					additionalFields={additionalFields}
					onChange={(bundle) => onEstimationChange(name, bundle)}
				/>
			);
		}
		if (isParentSourcesDefaultName(name) && tracker) {
			return (
				<ParentSourcesList
					key={name}
					owner={ownerOfParentSourcesDefault(name)}
					value={parentSourcesOfDefault(name, defaults)}
					tracker={tracker}
					additionalFieldDefinitions={savedFields}
					onChange={(list) => onParentSourcesChange(name, list)}
					info={parentSourcesTip(name, tracker, tipTerms)}
				/>
			);
		}
		return (
			<FormSelectField<number>
				key={name}
				label={WhereEachDefaultSits[name].label(featureTerm)}
				value={chosenFieldOf(name)}
				options={fieldOptions}
				onChange={(fieldId) => onChange(name, fieldId)}
				info={directionTip(name, tracker, tipTerms)}
			/>
		);
	};

	return (
		<Box component="section" aria-label={title}>
			<InputGroup title={title}>
				<Typography variant="body2" color="text.secondary">
					{`${teamsTerm} and ${portfoliosTerm} on this connection use these unless they set their own.`}
				</Typography>
				{sections.map(({ heading, rows }) => (
					<Box
						key={heading}
						component="fieldset"
						sx={{ border: 0, m: 0, p: 0, minWidth: 0 }}
					>
						<Typography
							component="legend"
							variant="subtitle2"
							color="text.secondary"
						>
							{heading}
						</Typography>
						{rows.map(rowOf)}
					</Box>
				))}
			</InputGroup>
		</Box>
	);
};

export default ConnectionDefaultsSection;
