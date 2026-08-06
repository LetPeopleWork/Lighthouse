import {
	Box,
	FormControlLabel,
	Paper,
	Switch,
	TableContainer,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import { useHideCompletedFeatures } from "../../../hooks/useHideCompletedFeatures";
import type { IFeature } from "../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import DataGridBase from "../DataGrid/DataGridBase";
import {
	createActiveWorkColumn,
	createPositionColumn,
	createWarningsColumn,
} from "./columns";
import type { FeatureListDataGridProps } from "./types";

const FeatureListDataGrid: React.FC<FeatureListDataGridProps> = ({
	features,
	columns,
	storageKey,
	hideCompletedStorageKey,
	loading = false,
	emptyStateMessage,
	getActiveWorkTeams,
}) => {
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	const { hideCompleted, handleToggleChange } = useHideCompletedFeatures(
		hideCompletedStorageKey,
	);

	const filteredFeatures = useMemo(() => {
		return hideCompleted
			? features.filter(
					(feature) =>
						feature.stateCategory !== "Done" ||
						feature.getRemainingWorkForFeature() > 0,
				)
			: features;
	}, [features, hideCompleted]);

	// The caller supplies the name column first and the surface-specific ones last; the shared columns
	// every feature list carries are inserted around them here.
	const [nameColumn, ...surfaceColumns] = columns;
	const activeWorkColumn = getActiveWorkTeams
		? createActiveWorkColumn(getActiveWorkTeams)
		: null;
	const gridColumns = [
		createPositionColumn("#"),
		nameColumn,
		createWarningsColumn(),
		...(activeWorkColumn ? [activeWorkColumn] : []),
		...surfaceColumns,
	];

	return (
		<TableContainer component={Paper}>
			<Box sx={{ display: "flex", justifyContent: "flex-end", p: 2, gap: 2 }}>
				<FormControlLabel
					control={
						<Switch
							checked={hideCompleted}
							onChange={handleToggleChange}
							color="primary"
							data-testid="hide-completed-features-toggle"
						/>
					}
					label={`Hide Completed ${featuresTerm}`}
				/>
			</Box>
			<DataGridBase
				rows={filteredFeatures as (IFeature & GridValidRowModel)[]}
				columns={gridColumns}
				storageKey={storageKey}
				loading={loading}
				emptyStateMessage={emptyStateMessage}
			/>
		</TableContainer>
	);
};

export default FeatureListDataGrid;
