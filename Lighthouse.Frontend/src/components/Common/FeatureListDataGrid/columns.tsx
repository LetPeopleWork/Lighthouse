import { Box, Link, Tooltip, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import {
	hasNothingWrongWithIt,
	type IFeatureDependency,
} from "../../../models/FeatureDependency";
import { getWorkItemName } from "../../../utils/featureName";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import type { DataGridColumn } from "../DataGrid/types";
import FeatureName from "../FeatureName/FeatureName";
import ForecastInfoList from "../Forecasts/ForecastInfoList";
import ParentWorkItemCell from "../ParentWorkItemCell/ParentWorkItemCell";
import ActiveWorkIndicator from "./ActiveWorkIndicator";
import FeatureMoveMenu from "./FeatureMoveMenu";
import type { FeatureOrderingBinding } from "./types";
import WarningsIndicator from "./WarningsIndicator";

// FeatureListDataGrid pins this column first, so every feature list renders the name the same way.
export const createNameColumn = (
	featureTerm: string,
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "name",
	headerName: `${featureTerm} Name`,
	hideable: false,
	width: 300,
	flex: 1,
	renderCell: ({ row }) => (
		<FeatureName
			name={getWorkItemName(row.name, row.referenceId)}
			url={row.url ?? ""}
		/>
	),
});

export const createForecastsColumn = (
	headerName = "Forecasts",
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "forecasts",
	headerName,
	width: 200,
	sortable: false,
	renderCell: ({ row }) => (
		<Box data-testid="feature-forecast-cell">
			{cannotBeForecast({ teamsWithoutForecast: row.teamsWithoutForecast }) ? (
				<Tooltip title={cannotForecastReason(row.teamsWithoutForecast ?? [])}>
					<Typography variant="body2" color="text.secondary">
						{CANNOT_FORECAST_SHORT}
					</Typography>
				</Tooltip>
			) : (
				<ForecastInfoList title={""} forecasts={row.forecasts} />
			)}
		</Box>
	),
});

// The value is the backend-supplied global position, never the row index - the grid may be filtered.
export const createPositionColumn = (
	headerLabel: string,
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "position",
	headerName: headerLabel,
	width: 70,
	sortable: true,
	valueGetter: (_, row) => row.position,
	renderCell: ({ row }) => <span>{row.position ?? ""}</span>,
});

// The four gestures live behind one menu so a row gains one control, not four.
export const createFeatureOrderingActionsColumn = (
	binding: FeatureOrderingBinding,
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "featureOrderingActions",
	headerName: "",
	width: 60,
	sortable: false,
	filterable: false,
	renderCell: ({ row }) => (
		<FeatureMoveMenu
			feature={row}
			gate={binding.resolveGate(row)}
			onMove={(target) => binding.onMove(row.id, target)}
			visibleNeighbours={binding.neighboursFor(row)}
		/>
	),
});

export const createStateColumn = (): DataGridColumn<
	IFeature & GridValidRowModel
> => ({
	field: "state",
	headerName: "State",
	width: 150,
	sortable: true,
	renderCell: ({ row }) => <span>{row.state}</span>,
});

export const createWarningsColumn = (): DataGridColumn<
	IFeature & GridValidRowModel
> => ({
	field: "warnings",
	headerName: "Warnings",
	type: "boolean",
	width: 90,
	sortable: true,
	// The sort has to see everything the icon shows, or a row whose only warning is about a dependency
	// sorts as though it were clean and the column quietly stops being a way to find them.
	valueGetter: (_, row) =>
		(row.stateCategory === "Done" && row.getRemainingWorkForFeature() > 0) ||
		row.isUsingDefaultFeatureSize ||
		(row.dependsOn ?? []).some(
			(dependency) => !hasNothingWrongWithIt(dependency),
		),
	renderCell: ({ row }) => (
		<WarningsIndicator
			isDoneWithRemainingWork={
				row.stateCategory === "Done" && row.getRemainingWorkForFeature() > 0
			}
			isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
			dependencies={row.dependsOn}
		/>
	),
});

export const createActiveWorkColumn = (
	getTeams: (row: IFeature) => IEntityReference[],
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "activeWork",
	headerName: "Active Work",
	type: "boolean",
	width: 110,
	sortable: true,
	valueGetter: (_, row) => getTeams(row).length > 0,
	renderCell: ({ row }) => <ActiveWorkIndicator teams={getTeams(row)} />,
});

// Most Features wait on nothing, so the cell stays blank in that case, the same way a missing position
// is left blank. Each one it does wait on gets its own line: a reader scanning the column is looking
// for which Features are involved, and a run-on line makes them read it twice.
const renderDependsOn = (row: IFeature) => (
	<Box sx={{ py: 0.5 }}>
		{(row.dependsOn ?? []).map((dependency, index) => (
			<Typography
				// A withheld entry carries no id of its own to be listed under - that is what withholding it
				// means - so it is the one case keyed by where it sits.
				key={
					dependency.isWithheld ? `withheld-${index}` : dependency.referenceId
				}
				variant="body2"
				component="div"
				data-testid={`depends-on-${row.referenceId}`}
			>
				{renderDependency(dependency)}
			</Typography>
		))}
	</Box>
);

const renderDependency = (dependency: IFeatureDependency) => {
	if (dependency.isWithheld) {
		return <em>No access</em>;
	}

	const named = `${dependency.referenceId}: ${dependency.name}`;

	if (!dependency.url) {
		return <span>{named}</span>;
	}

	return (
		<Link href={dependency.url} target="_blank" rel="noopener noreferrer">
			{named}
		</Link>
	);
};

export const createDependsOnColumn = (): DataGridColumn<
	IFeature & GridValidRowModel
> => ({
	field: "dependsOn",
	headerName: "Dependencies",
	width: 260,
	sortable: true,
	// Sorted by how many a Feature waits on: the list itself has no order a reader would sort by, and
	// the question the column answers first is which rows are entangled at all.
	valueGetter: (_, row) => (row.dependsOn ?? []).length,
	renderCell: ({ row }) => renderDependsOn(row),
});

export const createParentColumn = (
	parentMap: Map<string, ParentWorkItem>,
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "parent",
	headerName: "Parent",
	width: 300,
	sortable: false,
	renderCell: ({ row }) => (
		<Box>
			<ParentWorkItemCell
				parentReference={row.parentWorkItemReference}
				parentMap={parentMap}
			/>
		</Box>
	),
});
