import { Box, Paper, TableContainer } from "@mui/material";
import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type {
	DataGridColumn,
	DataGridExportTable,
} from "../../../../../components/Common/DataGrid/types";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import type { FeatureMetric } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import { getWorkItemName } from "../../../../../utils/featureName";

type ArchivedRow = FeatureMetric & GridValidRowModel;

interface ArchivedFeatureGridProps {
	rows: FeatureMetric[];
	deliveryId: number;
	featureTerm: string;
	featuresTerm: string;
	exportFileName: string;
	exportTable: (orderedRowIds: GridRowId[]) => DataGridExportTable;
}

/**
 * Which Features a Delivery had on the day it closed, and nothing more.
 *
 * The question a closed Delivery gets asked is which Features were in it. How far along each one was
 * and what its chance of landing looked like are questions about a Delivery still running, and
 * showing them here invites a comparison against today that the numbers cannot support. The export
 * still carries them for anyone who wants the detail.
 *
 * Nothing here is fetched: the name, reference and link were all written down at closure. A value
 * read live under a heading promising the closing day's is the one failure this record exists to
 * prevent.
 */
const ArchivedFeatureGrid: React.FC<ArchivedFeatureGridProps> = ({
	rows,
	deliveryId,
	featureTerm,
	featuresTerm,
	exportFileName,
	exportTable,
}) => {
	const columns: DataGridColumn<ArchivedRow>[] = useMemo(
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				minWidth: 240,
				flex: 1,
				renderCell: ({ row }) => (
					<FeatureName
						name={getWorkItemName(row.name, row.referenceId)}
						url={row.url ?? ""}
					/>
				),
			},
		],
		[featureTerm],
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
