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
import { createActiveWorkColumn, createWarningsColumn } from "./columns";
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
				columns={(() => {
					const [nameCol, ...restCols] = columns;
					const warningsCol = createWarningsColumn();
					const activeWorkCol = getActiveWorkTeams
						? createActiveWorkColumn(getActiveWorkTeams)
						: null;
					return [
						nameCol,
						warningsCol,
						...(activeWorkCol ? [activeWorkCol] : []),
						...restCols,
					];
				})()}
				storageKey={storageKey}
				loading={loading}
				emptyStateMessage={emptyStateMessage}
			/>
		</TableContainer>
	);
};

export default FeatureListDataGrid;
