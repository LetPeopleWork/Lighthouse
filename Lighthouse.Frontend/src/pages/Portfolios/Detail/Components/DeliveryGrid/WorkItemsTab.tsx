import { Box, Typography } from "@mui/material";
import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import type {
	DataGridColumn,
	DataGridExportTable,
} from "../../../../../components/Common/DataGrid/types";
import FeatureListDataGrid from "../../../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import type { IFeature } from "../../../../../models/Feature";

interface WorkItemsTabProps {
	isLoadingFeatures: boolean;
	features: IFeature[];
	columns: DataGridColumn<IFeature & GridValidRowModel>[];
	deliveryId: number;
	featuresTerm: string;
	deliveryTerm: string;
	exportFileName: string;
	exportTable: (orderedRowIds: GridRowId[]) => DataGridExportTable;
}

const WorkItemsTab: React.FC<WorkItemsTabProps> = ({
	isLoadingFeatures,
	features,
	columns,
	deliveryId,
	featuresTerm,
	deliveryTerm,
	exportFileName,
	exportTable,
}) => {
	if (isLoadingFeatures) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				{`Loading ${featuresTerm}...`}
			</Typography>
		);
	}

	if (features.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				No {featuresTerm} in this {deliveryTerm}.
			</Typography>
		);
	}

	return (
		<Box sx={{ mx: 2, mb: 2, mt: 2 }}>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`delivery-features-${deliveryId}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_delivery_${deliveryId}`}
				loading={false}
				emptyStateMessage={`No ${featuresTerm} found`}
				enableExport={true}
				exportFileName={exportFileName}
				exportTable={exportTable}
			/>
		</Box>
	);
};

export default WorkItemsTab;
