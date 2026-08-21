import {
	Box,
	FormControlLabel,
	Paper,
	Switch,
	TableContainer,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useContext, useMemo, useState } from "react";
import { useFeatureOrdering } from "../../../hooks/useFeatureOrdering";
import { useHideCompletedFeatures } from "../../../hooks/useHideCompletedFeatures";
import type { IFeature } from "../../../models/Feature";
import type { FeatureMoveTarget } from "../../../models/FeatureOrdering";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import DataGridBase from "../DataGrid/DataGridBase";
import {
	createActiveWorkColumn,
	createDependsOnColumn,
	createFeatureOrderingActionsColumn,
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
	showPosition = false,
	onOrderChanged,
}) => {
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);

	// The column names whoever owns the order, and the same hook decides whether a row may be moved. The
	// factories stay policy-ignorant - they take what they are given - so this is the one place the two
	// headings and the one verdict are chosen.
	const { positionColumnLabel, resolveMoveGate } = useFeatureOrdering();
	const { featureService } = useContext(ApiServiceContext);

	// "Up" has no predictable meaning in a list a column is sorting, so the grid has to know.
	const [isSortActive, setIsSortActive] = useState(false);

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

	// A scan per rendered row would be quadratic, and this list has to stay usable at five hundred rows.
	const rowIndexById = useMemo(
		() => new Map(filteredFeatures.map((row, index) => [row.id, index])),
		[filteredFeatures],
	);

	// The rows either side of a Feature AS SHOWN. Hidden Done Features and anything the grid filtered out
	// are jumped over rather than landed on, which is why this reads the rendered list and not the raw one.
	const neighboursFor = (feature: IFeature) => {
		const index = rowIndexById.get(feature.id) ?? -1;

		return {
			firstId: filteredFeatures[0]?.id,
			previousId: index > 0 ? filteredFeatures[index - 1].id : undefined,
			nextId: index < 0 ? undefined : filteredFeatures[index + 1]?.id,
		};
	};

	const moveFeature = async (featureId: number, target: FeatureMoveTarget) => {
		await featureService.moveFeature(featureId, target);
		await onOrderChanged?.();
	};

	// The caller supplies the name column first and the surface-specific ones last; the shared columns
	// every feature list carries are inserted around them here.
	const [nameColumn, ...surfaceColumns] = columns;
	const activeWorkColumn = getActiveWorkTeams
		? createActiveWorkColumn(getActiveWorkTeams)
		: null;
	const gridColumns = [
		...(showPosition ? [createPositionColumn(positionColumnLabel)] : []),
		nameColumn,
		createWarningsColumn(),
		...(activeWorkColumn ? [activeWorkColumn] : []),
		...surfaceColumns,
		createDependsOnColumn({
			featureTerm: getTerm(TERMINOLOGY_KEYS.FEATURE),
			portfolioTerm: getTerm(TERMINOLOGY_KEYS.PORTFOLIO),
		}),
		// The two surfaces that show a place are the two that let you change it, so one flag names both
		// and neither caller passes the menu in.
		...(showPosition
			? [
					createFeatureOrderingActionsColumn({
						resolveGate: (feature) =>
							resolveMoveGate(feature, { isSortActive }),
						neighboursFor,
						onMove: moveFeature,
					}),
				]
			: []),
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
				onSortModelChange={(model) => setIsSortActive(model.length > 0)}
			/>
		</TableContainer>
	);
};

export default FeatureListDataGrid;
