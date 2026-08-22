import { Box, Paper, TableContainer, Tooltip, Typography } from "@mui/material";
import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type {
	DataGridColumn,
	DataGridExportTable,
} from "../../../../../components/Common/DataGrid/types";
import type { FeatureMetric } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import { CANNOT_FORECAST_SHORT } from "../../../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";

const NOT_RECORDED = "—";

type ArchivedRow = FeatureMetric & GridValidRowModel;

interface ArchivedFeatureGridProps {
	rows: FeatureMetric[];
	deliveryId: number;
	featureTerm: string;
	featuresTerm: string;
	workItemsTerm: string;
	exportFileName: string;
	exportTable: (orderedRowIds: GridRowId[]) => DataGridExportTable;
}

/**
 * The Feature rows a Delivery had on the day it closed.
 *
 * A narrower grid than a live Delivery's on purpose. The record holds a name, a reference, how far
 * along each Feature was, how many Work Items it held and its own chance of landing — and nothing
 * else, so there is no state, no Team, no per-Feature forecast date and no way through to the
 * Feature as it stands today. Every one of those would have to be fetched live, and a number
 * fetched live under a heading promising the closing day's is the one failure this whole record
 * exists to prevent.
 */
const ArchivedFeatureGrid: React.FC<ArchivedFeatureGridProps> = ({
	rows,
	deliveryId,
	featureTerm,
	featuresTerm,
	workItemsTerm,
	exportFileName,
	exportTable,
}) => {
	const columns: DataGridColumn<ArchivedRow>[] = useMemo(
		() => [
			{
				field: "referenceId",
				headerName: "Reference",
				minWidth: 100,
				flex: 0.4,
			},
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				minWidth: 160,
				flex: 1,
			},
			{
				field: "completion",
				headerName: "Completion",
				minWidth: 110,
				flex: 0.4,
				renderCell: ({ row }) => (
					<Typography variant="body2">
						{`${Math.round(row.completion)}%`}
					</Typography>
				),
			},
			{
				field: "likelihood",
				headerName: "Likelihood",
				minWidth: 120,
				flex: 0.4,
				renderCell: ({ row }) => (
					<Typography variant="body2">
						{row.likelihood === null
							? CANNOT_FORECAST_SHORT
							: formatLikelihood(row.likelihood, {
									hasRemainingWork: row.completion < 100,
									precision: "round",
								})}
					</Typography>
				),
			},
			{
				field: "totalItems",
				headerName: workItemsTerm,
				minWidth: 100,
				flex: 0.3,
				renderCell: ({ row }) => (
					<Typography variant="body2">
						{row.totalItems ?? NOT_RECORDED}
					</Typography>
				),
			},
			{
				field: "isUsingDefaultSize",
				headerName: "Size",
				minWidth: 110,
				flex: 0.3,
				sortable: false,
				renderCell: ({ row }) =>
					row.isUsingDefaultSize === true ? (
						<Tooltip
							title={`No child ${workItemsTerm} were found for this ${featureTerm}, so the count is the configured default size rather than a measurement.`}
						>
							<Typography variant="body2" color="text.secondary">
								Estimated
							</Typography>
						</Tooltip>
					) : null,
			},
		],
		[featureTerm, workItemsTerm],
	);

	return (
		<Box sx={{ mx: 2, mb: 2, mt: 2 }}>
			<TableContainer component={Paper}>
				<DataGridBase<ArchivedRow>
					rows={rows}
					columns={columns}
					idField="referenceId"
					storageKey={`archived-delivery-features-${deliveryId}`}
					emptyStateMessage={`No ${featuresTerm} in this record`}
					enableExport={true}
					exportFileName={exportFileName}
					exportTable={exportTable}
				/>
			</TableContainer>
		</Box>
	);
};

export default ArchivedFeatureGrid;
