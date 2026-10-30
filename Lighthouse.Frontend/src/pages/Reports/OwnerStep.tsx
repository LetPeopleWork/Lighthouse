import {
	Box,
	Button,
	Checkbox,
	CircularProgress,
	FormControlLabel,
	FormGroup,
	Link,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { Link as RouterLink } from "react-router";
import {
	type IReportCreationOwner,
	MOST_PER_BULK_RUN,
} from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";

export const ownerKeyOf = (
	owner: Pick<IReportCreationOwner, "ownerKind" | "ownerId">,
): string => `${owner.ownerKind}:${owner.ownerId}`;

/** Id of the message the disabled Create button points at while too many owners are picked. */
export const OVER_THE_CEILING_ID = "owners-over-the-ceiling";

export const isOverTheCeiling = (pickedCount: number): boolean =>
	pickedCount > MOST_PER_BULK_RUN;

/** Why an owner cannot hold the window: when its kept history starts and when Then would start, both as shown. */
export interface OwnerLimit {
	keptSince: string;
	thenStarts: string;
}

export type OwnerLimitOf = (owner: IReportCreationOwner) => OwnerLimit | null;

const settingsPathOf = (owner: IReportCreationOwner): string =>
	`/${owner.ownerKind === "team" ? "teams" : "portfolios"}/${owner.ownerId}/settings`;

/** Whether the owners to pick from are known: still on their way, could not be loaded, or here. */
export type OwnersStatus = "loading" | "failed" | "ready";

interface OwnerStepProps {
	ownersStatus: OwnersStatus;
	onRetry: () => void;
	/** The owners that may be picked, in the order they are listed and sent. */
	teams: readonly IReportCreationOwner[];
	/** Listed after the Teams, in the order they are sent. */
	portfolios: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	limitOf: OwnerLimitOf;
	onToggle: (owner: IReportCreationOwner) => void;
	onSelectAll: (owners: readonly IReportCreationOwner[]) => void;
	stepNumber: number;
	stepCount: number;
	disabled: boolean;
}

interface OwnerGroupProps {
	heading: string;
	owners: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	limitOf: OwnerLimitOf;
	onToggle: (owner: IReportCreationOwner) => void;
	onSelectAll: (owners: readonly IReportCreationOwner[]) => void;
	disabled: boolean;
}

/** How the two kinds of owner are named together, in the reader's own terms. */
const useOwnersName = (): string => {
	const { getTerm } = useTerminology();
	return `${getTerm(TERMINOLOGY_KEYS.TEAMS)} and ${getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}`;
};

const OwnerGroup: React.FC<Readonly<OwnerGroupProps>> = ({
	heading,
	owners,
	picked,
	limitOf,
	onToggle,
	onSelectAll,
	disabled,
}) => (
	<FormGroup role="group" aria-label={heading}>
		<Box
			sx={{
				display: "flex",
				justifyContent: "space-between",
				alignItems: "center",
			}}
		>
			<Typography variant="subtitle1">{heading}</Typography>
			<Button
				size="small"
				disabled={disabled}
				aria-label={`Select all ${heading}`}
				onClick={() => onSelectAll(owners)}
			>
				Select all
			</Button>
		</Box>
		{owners.map((owner) => {
			const limit = limitOf(owner);
			return (
				<Box key={ownerKeyOf(owner)}>
					<FormControlLabel
						label={owner.ownerName}
						control={
							<Checkbox
								checked={picked.has(ownerKeyOf(owner))}
								disabled={disabled || limit !== null}
								onChange={() => onToggle(owner)}
								slotProps={{ input: { "aria-label": owner.ownerName } }}
							/>
						}
					/>
					{limit !== null && (
						<Typography variant="body2" color="text.secondary" sx={{ pl: 4 }}>
							{`Kept history starts ${limit.keptSince}; Then starts ${limit.thenStarts}.`}{" "}
							<Link component={RouterLink} to={settingsPathOf(owner)}>
								{`Open ${owner.ownerName} settings`}
							</Link>
						</Typography>
					)}
				</Box>
			);
		})}
	</FormGroup>
);

type OwnerListProps = Omit<
	OwnerStepProps,
	"ownersStatus" | "onRetry" | "stepNumber" | "stepCount"
>;

const OwnerList: React.FC<Readonly<OwnerListProps>> = ({
	teams,
	portfolios,
	...group
}) => {
	const { getTerm } = useTerminology();
	const ownersName = useOwnersName();
	const pickedCount = [...teams, ...portfolios].filter((owner) =>
		group.picked.has(ownerKeyOf(owner)),
	).length;

	return (
		<>
			{teams.length > 0 && (
				<OwnerGroup
					heading={getTerm(TERMINOLOGY_KEYS.TEAMS)}
					owners={teams}
					{...group}
				/>
			)}
			{portfolios.length > 0 && (
				<OwnerGroup
					heading={getTerm(TERMINOLOGY_KEYS.PORTFOLIOS)}
					owners={portfolios}
					{...group}
				/>
			)}
			<Typography variant="body2" color="text.secondary">
				{`Only ${ownersName} you are Admin of are listed.`}
			</Typography>
			{isOverTheCeiling(pickedCount) && (
				<Typography
					id={OVER_THE_CEILING_ID}
					role="alert"
					color="error"
					align="right"
				>
					{`Pick at most ${MOST_PER_BULK_RUN} ${ownersName}.`}
				</Typography>
			)}
		</>
	);
};

const OwnerStep: React.FC<Readonly<OwnerStepProps>> = ({
	ownersStatus,
	onRetry,
	stepNumber,
	stepCount,
	...list
}) => {
	const { getTerm } = useTerminology();
	const noOwners = list.teams.length + list.portfolios.length === 0;
	const ownersName = useOwnersName();

	return (
		<Stack spacing={2}>
			<Typography variant="h6" component="h3">
				{`Step ${stepNumber} of ${stepCount} — Who gets this report?`}
			</Typography>
			{ownersStatus === "loading" && (
				<CircularProgress aria-label={`Loading your ${ownersName}`} />
			)}
			{ownersStatus === "failed" && (
				<Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
					<Typography role="alert">{`Your ${ownersName} couldn't be loaded.`}</Typography>
					<Button onClick={onRetry}>Retry</Button>
				</Box>
			)}
			{ownersStatus === "ready" && noOwners && (
				<Typography>
					{`You are not Admin of any ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}.`}
				</Typography>
			)}
			{ownersStatus === "ready" && !noOwners && <OwnerList {...list} />}
		</Stack>
	);
};

export default OwnerStep;
