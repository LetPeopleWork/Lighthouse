import ArchiveIcon from "@mui/icons-material/Archive";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import LinkOffIcon from "@mui/icons-material/LinkOff";
import type TouchAppIcon from "@mui/icons-material/TouchApp";
import { Box, IconButton, Tooltip, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useState } from "react";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import {
	createForecastedStartColumn,
	createForecastsColumn,
	createStateColumn,
} from "../../../../../components/Common/FeatureListDataGrid/columns";
import FeatureProgressIndicator from "../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import { FeatureLikelihoodChip } from "../../../../../components/Common/Forecasts/FeatureLikelihoodChip";
import StyledLink from "../../../../../components/Common/StyledLink/StyledLink";
import type { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import { getWorkItemName } from "../../../../../utils/featureName";
import { PREMIUM_UPGRADE_TOOLTIP } from "../../../../../utils/premiumUpgradeTooltip";

// A Feature's own chance of landing, not the Delivery's — the Delivery's asks whether they ALL land,
// so it sits below any single row's, and the heading must not invite the two to be read as one number.
const MARGINAL_LIKELIHOOD_HEADER = "Likelihood";

const ACTION_BUTTON_SX = {
	bgcolor: "background.paper",
	"&:hover": { bgcolor: "primary.light" },
} as const;

/** Archive, edit and delete, for a reader who may do them. */
export const DeliveryRowActions: React.FC<{
	delivery: Delivery;
	deliveryTerm: string;
	canArchive: boolean;
	onArchive?: (delivery: Delivery) => void;
	onEdit: (delivery: Delivery) => void;
	onDelete: (delivery: Delivery) => void;
}> = ({ delivery, deliveryTerm, canArchive, onArchive, onEdit, onDelete }) => {
	const stopAnd = (act: () => void) => (event: React.MouseEvent) => {
		event.stopPropagation();
		act();
	};

	return (
		<Box
			sx={{
				position: "absolute",
				top: "50%",
				transform: "translateY(-50%)",
				right: 8,
				zIndex: 1,
				display: "flex",
				alignItems: "center",
				gap: 0.5,
			}}
		>
			<Tooltip
				title={canArchive ? `Archive ${deliveryTerm}` : PREMIUM_UPGRADE_TOOLTIP}
			>
				{/* The button is disabled without a licence, and a disabled button fires no events, so
				    the tooltip needs an enabled element of its own to sit on. */}
				<span>
					<IconButton
						size="small"
						disabled={!canArchive}
						onClick={stopAnd(() => onArchive?.(delivery))}
						aria-label="archive"
						sx={ACTION_BUTTON_SX}
					>
						<ArchiveIcon />
					</IconButton>
				</span>
			</Tooltip>
			<IconButton
				size="small"
				onClick={stopAnd(() => onEdit(delivery))}
				aria-label="edit"
				sx={ACTION_BUTTON_SX}
			>
				<EditIcon />
			</IconButton>
			<IconButton
				size="small"
				onClick={stopAnd(() => onDelete(delivery))}
				aria-label="delete"
				sx={{
					bgcolor: "background.paper",
					"&:hover": { bgcolor: "error.light" },
				}}
			>
				<DeleteIcon />
			</IconButton>
		</Box>
	);
};

/**
 * The badge beside a Delivery's name saying how its Features were chosen — and, for a reader who may
 * let go of the source, the button that starts doing so.
 *
 * A whole link reads as a badge rather than as something to press, so the marker only breaks the
 * moment the pointer or the keyboard arrives: that shows what pressing will do without a second glyph
 * sitting beside every name for the rest of the time. The hover state lives here because nothing
 * outside this badge has any use for it.
 */
export const SelectionModeMarker: React.FC<{
	hint: string;
	Icon: typeof TouchAppIcon;
	canUnbind: boolean;
	onAskToUnbind: () => void;
}> = ({ hint, Icon, canUnbind, onAskToUnbind }) => {
	const [isAboutToUnbind, setIsAboutToUnbind] = useState(false);

	if (!canUnbind) {
		return (
			<Tooltip title={hint}>
				<Box sx={{ display: "flex", alignItems: "center" }}>
					<Icon fontSize="small" sx={{ color: "text.secondary" }} />
				</Box>
			</Tooltip>
		);
	}

	return (
		<Tooltip title={hint}>
			<IconButton
				size="small"
				aria-label={hint}
				onClick={(e) => {
					e.stopPropagation();
					onAskToUnbind();
				}}
				onMouseEnter={() => setIsAboutToUnbind(true)}
				onMouseLeave={() => setIsAboutToUnbind(false)}
				onFocus={() => setIsAboutToUnbind(true)}
				onBlur={() => setIsAboutToUnbind(false)}
				sx={{ color: "text.secondary", p: 0 }}
			>
				{isAboutToUnbind ? (
					<LinkOffIcon fontSize="small" />
				) : (
					<Icon fontSize="small" />
				)}
			</IconButton>
		</Tooltip>
	);
};

/** The teams carrying work on one row, or a word saying nobody is. */
export const OwningTeams: React.FC<{
	row: IFeature;
	teams: IEntityReference[];
}> = ({ row, teams }) => {
	const teamsWithWork = teams.filter(
		(team) => row.getTotalWorkForTeam(team.id) > 0,
	);

	if (teamsWithWork.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				Unassigned
			</Typography>
		);
	}

	return (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
			{teamsWithWork.map((team) => (
				<StyledLink key={team.id} to={`/teams/${team.id}`}>
					<Typography variant="body2">{team.name}</Typography>
				</StyledLink>
			))}
		</Box>
	);
};

/**
 * The grid's column definitions. They are a description of the table rather than part of this
 * component's own logic, and each cell renderer is a small function of its own — so they live out
 * here, where they do not count against the component that happens to mount the grid.
 */
export function featureColumns({
	featureTerm,
	delivery,
	teams,
	onShowDetails,
}: {
	featureTerm: string;
	delivery: Delivery;
	teams: IEntityReference[];
	onShowDetails: (feature: IFeature) => void;
}): DataGridColumn<IFeature & GridValidRowModel>[] {
	return [
		{
			field: "name",
			headerName: `${featureTerm} Name`,
			hideable: false,
			minWidth: 120,
			flex: 1,
			renderCell: ({ row }) => (
				<FeatureName
					name={getWorkItemName(row.name, row.referenceId)}
					url={row.url ?? ""}
				/>
			),
		},
		{
			field: "owningTeam",
			headerName: "Team",
			minWidth: 100,
			flex: 0.5,
			renderCell: ({ row }) => <OwningTeams row={row} teams={teams} />,
		},
		{
			field: "progress",
			headerName: "Progress",
			minWidth: 200,
			flex: 1,
			sortable: false,
			renderCell: ({ row }) => (
				<FeatureProgressIndicator
					feature={row}
					teams={teams}
					onShowDetails={() => Promise.resolve(onShowDetails(row))}
				/>
			),
		},
		{
			...createForecastedStartColumn("Forecasted Start"),
			minWidth: 100,
			flex: 0.5,
			width: undefined,
		},
		{
			...createForecastsColumn("Forecasted Completion"),
			minWidth: 100,
			flex: 0.5,
			width: undefined,
		},
		{
			field: "likelihood",
			headerName: MARGINAL_LIKELIHOOD_HEADER,
			minWidth: 110,
			flex: 0.3,
			sortable: false,
			renderCell: ({ row }) =>
				delivery.featureLikelihoods
					.filter((fl) => fl.featureId === row.id)
					.map((fl) => (
						<FeatureLikelihoodChip
							key={fl.featureId}
							featureLikelihood={fl}
							hasRemainingWork={row.getRemainingWorkForFeature() > 0}
						/>
					)),
		},
		createStateColumn(),
	];
}
