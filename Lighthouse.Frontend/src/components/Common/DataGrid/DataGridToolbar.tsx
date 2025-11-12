import { Alert, Box } from "@mui/material";
import {
	type GridCsvExportOptions,
	GridToolbarContainer,
	GridToolbarDensitySelector,
	GridToolbarExport,
	GridToolbarFilterButton,
	type GridToolbarProps,
} from "@mui/x-data-grid";
import type React from "react";

interface DataGridToolbarProps extends GridToolbarProps {
	/** Whether user has premium features available */
	canUsePremiumFeatures?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
}

/**
 * Custom toolbar for DataGrid with CSV export functionality
 * Export feature is gated behind premium license
 */
const DataGridToolbar: React.FC<DataGridToolbarProps> = ({
	canUsePremiumFeatures = false,
	exportFileName,
}) => {
	// Generate filename with timestamp
	const generateFileName = () => {
		const timestamp = new Date().toISOString().split("T")[0];
		return exportFileName
			? `${exportFileName}_${timestamp}`
			: `data_export_${timestamp}`;
	};

	// CSV export options - respects current filters, sorting, and visible columns
	const csvOptions: GridCsvExportOptions = {
		fileName: generateFileName(),
		delimiter: ",",
		utf8WithBom: true, // Better support for special characters in Excel
		// Only export visible columns (not hidden ones)
		allColumns: false,
	};

	if (!canUsePremiumFeatures) {
		return (
			<GridToolbarContainer>
				<Box sx={{ p: 2, width: "100%" }}>
					<Alert severity="info" sx={{ mb: 0 }}>
						CSV export is a premium feature. Please obtain a valid license to
						export data.
					</Alert>
				</Box>
			</GridToolbarContainer>
		);
	}

	return (
		<GridToolbarContainer>
			<GridToolbarFilterButton />
			<GridToolbarDensitySelector
				slotProps={{ tooltip: { title: "Change density" } }}
			/>
			<GridToolbarExport
				csvOptions={csvOptions}
				printOptions={{ disableToolbarButton: true }} // Disable print for now
				slotProps={{
					tooltip: { title: "Export visible data as CSV" },
					button: { "data-testid": "export-button" },
				}}
			/>
		</GridToolbarContainer>
	);
};

export default DataGridToolbar;
