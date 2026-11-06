import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import CloseIcon from "@mui/icons-material/Close";
import {
	Box,
	IconButton,
	List,
	ListItem,
	Tooltip,
	Typography,
} from "@mui/material";
import type React from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";
import FormSelectField from "../FormSelectField/FormSelectField";
import {
	entriesStillToAdd,
	MaximumParentSources,
	nameOfParentSource,
	namesAnUnknownField,
	type ParentSourceEntry,
	type ParentSourcesAction,
	reduceParentSources,
	theTrackersOwn,
} from "./parentSources";

export interface ParentSourcesListProps {
	value: readonly ParentSourceEntry[];
	tracker: WorkTrackingSystemType;
	additionalFieldDefinitions: readonly IAdditionalFieldDefinition[];
	onChange: (list: readonly ParentSourceEntry[]) => void;
}

interface ParentSourceRowProps {
	position: number;
	name: string;
	isUnknown: boolean;
	isRefused: (action: ParentSourcesAction) => boolean;
	onEdit: (action: ParentSourcesAction) => void;
}

const ParentSourceRow: React.FC<Readonly<ParentSourceRowProps>> = ({
	position,
	name,
	isUnknown,
	isRefused,
	onEdit,
}) => {
	const moveUp: ParentSourcesAction = { type: "moveUp", index: position };
	const moveDown: ParentSourcesAction = { type: "moveDown", index: position };
	const remove: ParentSourcesAction = { type: "remove", index: position };
	const cannotBeRemoved = isRefused(remove);

	return (
		<ListItem disableGutters sx={{ gap: 1 }}>
			<Typography color="text.secondary">{position + 1}</Typography>
			<Typography
				color={isUnknown ? "warning.main" : "text.primary"}
				sx={{ flex: 1 }}
			>
				{name}
			</Typography>
			<IconButton
				size="small"
				aria-label={`Move ${name} up`}
				disabled={isRefused(moveUp)}
				onClick={() => onEdit(moveUp)}
			>
				<ArrowUpwardIcon fontSize="small" />
			</IconButton>
			<IconButton
				size="small"
				aria-label={`Move ${name} down`}
				disabled={isRefused(moveDown)}
				onClick={() => onEdit(moveDown)}
			>
				<ArrowDownwardIcon fontSize="small" />
			</IconButton>
			<Tooltip
				title={cannotBeRemoved ? "A list needs at least one source" : ""}
				describeChild
			>
				{/* A disabled button is not an event target, so the tooltip hangs on a wrapper instead. */}
				<span>
					<IconButton
						size="small"
						aria-label={`Remove ${name}`}
						disabled={cannotBeRemoved}
						onClick={() => onEdit(remove)}
					>
						<CloseIcon fontSize="small" />
					</IconButton>
				</span>
			</Tooltip>
		</ListItem>
	);
};

/** The ordered list of parent sources: a controlled value, handed on whole after every edit. */
const ParentSourcesList: React.FC<Readonly<ParentSourcesListProps>> = ({
	value,
	tracker,
	additionalFieldDefinitions,
	onChange,
}) => {
	const { getTerm } = useTerminology();
	const workItem = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const feature = getTerm(TERMINOLOGY_KEYS.FEATURE);

	const nameOf = (entry: ParentSourceEntry) =>
		nameOfParentSource(entry, tracker, additionalFieldDefinitions);
	const ownParentName = nameOf({ kind: "TrackerParent" });
	const stillToAdd = entriesStillToAdd(value, additionalFieldDefinitions);
	const isFull = value.length >= MaximumParentSources;
	const listsOwnParent = value.some((entry) => entry.kind === "TrackerParent");

	const isRefused = (action: ParentSourcesAction) =>
		reduceParentSources(value, action) === value;
	const edit = (action: ParentSourcesAction) =>
		onChange(reduceParentSources(value, action));

	const handleAdd = (position: number | null) => {
		if (position !== null) {
			edit({ type: "add", entry: stillToAdd[position] });
		}
	};

	return (
		<Box>
			<Typography variant="subtitle2" component="h3">
				Parent sources
			</Typography>
			<Typography variant="body2" color="text.secondary">
				The first source that has a value gives the parent; the rest are ignored
				for that {workItem}.
			</Typography>
			<List component="ol" dense>
				{value.map((entry, position) => (
					<ParentSourceRow
						key={`${entry.kind}-${entry.additionalFieldDefinitionId ?? ""}`}
						position={position}
						name={nameOf(entry)}
						isUnknown={namesAnUnknownField(entry, additionalFieldDefinitions)}
						isRefused={isRefused}
						onEdit={edit}
					/>
				))}
			</List>
			<FormSelectField<number>
				label="Add a source"
				value=""
				onChange={handleAdd}
				allowNone={false}
				disabled={isFull}
				options={stillToAdd.map((entry, position) => ({
					id: position,
					label: nameOf(entry),
				}))}
				helperText={
					isFull
						? `A list holds at most ${MaximumParentSources} sources`
						: `Adds ${ownParentName} or an Additional Field not yet in the list.`
				}
			/>
			{!listsOwnParent && (
				<Typography variant="body2" color="warning.main">
					Not in the list: {ownParentName}. {workItems} whose only parent is{" "}
					{theTrackersOwn(tracker)} parent get no {feature}.
				</Typography>
			)}
		</Box>
	);
};

export default ParentSourcesList;
