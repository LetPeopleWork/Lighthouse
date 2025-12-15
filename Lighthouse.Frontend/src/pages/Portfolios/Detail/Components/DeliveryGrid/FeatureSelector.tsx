import { Box, Checkbox, TextField, Typography } from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { StateCategory } from "../../../../../models/WorkItem";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface FeatureSelectorProps {
	features: IFeature[];
	selectedFeatureIds: number[];
	onSelectionChange: (selectedIds: number[]) => void;
}

export const FeatureSelector: React.FC<FeatureSelectorProps> = ({
	features,
	selectedFeatureIds,
	onSelectionChange,
}) => {
	const { getTerm } = useTerminology();
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const [searchTerm, setSearchTerm] = useState("");

	// Filter features to only show ToDo and Doing
	const eligibleFeatures = useMemo(() => {
		const validStates: StateCategory[] = ["ToDo", "Doing"];
		return features.filter((feature) =>
			validStates.includes(feature.stateCategory),
		);
	}, [features]);

	// Filter features by search term
	const filteredFeatures = useMemo(() => {
		if (!searchTerm.trim()) return eligibleFeatures;

		const lowerSearchTerm = searchTerm.toLowerCase();
		return eligibleFeatures.filter((feature) =>
			feature.name.toLowerCase().includes(lowerSearchTerm),
		);
	}, [eligibleFeatures, searchTerm]);

	// Select all state management
	const allSelected = eligibleFeatures.every((feature) =>
		selectedFeatureIds.includes(feature.id),
	);
	const someSelected =
		selectedFeatureIds.length > 0 &&
		eligibleFeatures.some((feature) => selectedFeatureIds.includes(feature.id));
	const indeterminate = someSelected && !allSelected;

	const handleSelectAll = () => {
		if (allSelected) {
			// Deselect all eligible features
			const remainingSelected = selectedFeatureIds.filter(
				(id) => !eligibleFeatures.some((feature) => feature.id === id),
			);
			onSelectionChange(remainingSelected);
		} else {
			// Select all eligible features
			const allEligibleIds = eligibleFeatures.map((feature) => feature.id);
			const nonEligibleSelected = selectedFeatureIds.filter(
				(id) => !eligibleFeatures.some((feature) => feature.id === id),
			);
			onSelectionChange([...nonEligibleSelected, ...allEligibleIds]);
		}
	};

	const handleFeatureToggle = (featureId: number) => {
		if (selectedFeatureIds.includes(featureId)) {
			onSelectionChange(selectedFeatureIds.filter((id) => id !== featureId));
		} else {
			onSelectionChange([...selectedFeatureIds, featureId]);
		}
	};

	const columns: DataGridColumn<IFeature>[] = [
		{
			field: "select",
			headerName: "",
			width: 50,
			sortable: false,
			filterable: false,
			hideable: false,
			renderCell: (params: { row: IFeature; value: unknown }) => (
				<Checkbox
					checked={selectedFeatureIds.includes(params.row.id)}
					onChange={() => handleFeatureToggle(params.row.id)}
					aria-label={`${params.row.name}`}
				/>
			),
		},
		{
			field: "id",
			headerName: "ID",
			width: 80,
			hideable: false,
		},
		{
			field: "name",
			headerName: "Name",
			flex: 1,
			minWidth: 200,
			hideable: false,
		},
	];

	return (
		<Box>
			<Box sx={{ display: "flex", alignItems: "center", mb: 2, gap: 2 }}>
				<Checkbox
					checked={allSelected}
					indeterminate={indeterminate}
					onChange={handleSelectAll}
					aria-label={`Select all ${featuresTerm.toLowerCase()}`}
				/>
				<Typography variant="body2">
					{allSelected
						? "Deselect All"
						: someSelected
							? "Select All"
							: "Select All"}
				</Typography>

				<TextField
					placeholder={`Search ${featuresTerm.toLowerCase()}...`}
					variant="outlined"
					size="small"
					value={searchTerm}
					onChange={(e) => setSearchTerm(e.target.value)}
					sx={{ ml: "auto", minWidth: 200 }}
				/>
			</Box>

			<DataGridBase
				rows={filteredFeatures}
				columns={columns}
				storageKey="delivery-feature-selector"
				hidePagination={false}
				emptyStateMessage={`No ${featuresTerm.toLowerCase()} available for selection`}
			/>
		</Box>
	);
};
