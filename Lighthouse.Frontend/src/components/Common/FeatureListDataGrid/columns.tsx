import { Box, Tooltip, Typography } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
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

// The value is the backend-supplied global position, never the row index (ADR-135).
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
	valueGetter: (_, row) =>
		(row.stateCategory === "Done" && row.getRemainingWorkForFeature() > 0) ||
		row.isUsingDefaultFeatureSize,
	renderCell: ({ row }) => (
		<WarningsIndicator
			isDoneWithRemainingWork={
				row.stateCategory === "Done" && row.getRemainingWorkForFeature() > 0
			}
			isUsingDefaultFeatureSize={row.isUsingDefaultFeatureSize}
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
