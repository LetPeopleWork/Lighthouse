import { Box } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import type { IEntityReference } from "../../../models/EntityReference";
import type { IFeature } from "../../../models/Feature";
import type { DataGridColumn } from "../DataGrid/types";
import ForecastInfoList from "../Forecasts/ForecastInfoList";
import ParentWorkItemCell from "../ParentWorkItemCell/ParentWorkItemCell";
import ActiveWorkIndicator from "./ActiveWorkIndicator";
import WarningsIndicator from "./WarningsIndicator";

export const createForecastsColumn = (
	headerName = "Forecasts",
): DataGridColumn<IFeature & GridValidRowModel> => ({
	field: "forecasts",
	headerName,
	width: 200,
	sortable: false,
	renderCell: ({ row }) => (
		<ForecastInfoList title={""} forecasts={row.forecasts} />
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
