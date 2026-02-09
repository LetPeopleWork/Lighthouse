import { Box } from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type { ParentWorkItem } from "../../../hooks/useParentWorkItems";
import type { IFeature } from "../../../models/Feature";
import type { DataGridColumn } from "../DataGrid/types";
import ForecastInfoList from "../Forecasts/ForecastInfoList";
import ParentWorkItemCell from "../ParentWorkItemCell/ParentWorkItemCell";

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
