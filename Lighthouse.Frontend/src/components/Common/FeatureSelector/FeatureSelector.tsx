import { Checkbox, Link } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import type { StateCategory } from "../../../models/WorkItem";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import type { FeatureSelectorProps, FeatureSelectorRow } from "./types";

export const FeatureSelector: React.FC<FeatureSelectorProps> = ({
	features,
	selectedFeatureIds,
	onChange,
	storageKey,
}) => {
	const eligibleFeatures = useMemo(() => {
		const validStates: Set<StateCategory> = new Set(["ToDo", "Doing"]);
		return features.filter((feature) => validStates.has(feature.stateCategory));
	}, [features]);

	// Transform features into rows for DataGrid
	const rows: FeatureSelectorRow[] = useMemo(() => {
		return eligibleFeatures.map((feature) => ({
			id: feature.id,
			name: feature.name,
			selected: selectedFeatureIds.includes(feature.id),
			feature,
		}));
	}, [eligibleFeatures, selectedFeatureIds]);

	const handleAddToSelection = (featureId: number) => {
		const newSelection = [...selectedFeatureIds, featureId];
		onChange(newSelection);
	};

	const handleRemoveFromSelection = (featureId: number) => {
		const newSelection = selectedFeatureIds.filter((id) => id !== featureId);
		onChange(newSelection);
	};

	const columns: DataGridColumn<FeatureSelectorRow>[] = [
		{
			field: "selected",
			headerName: "Selected",
			width: 120,
			hideable: false,
			sortable: false,
			renderCell: ({ row }) => (
				<Checkbox
					checked={row.selected}
					onChange={(event) =>
						event.target.checked
							? handleAddToSelection(row.id)
							: handleRemoveFromSelection(row.id)
					}
					aria-label={`Select ${row.name}`}
				/>
			),
		},
		{
			field: "id",
			headerName: "ID",
			flex: 1,
			hideable: false,
			renderCell: ({ row }) => (
				<Link
					href={row.feature.url ?? ""}
					target="_blank"
					rel="noopener noreferrer"
					sx={{
						textDecoration: "none",
						color: (theme) => theme.palette.primary.main,
						fontWeight: 500,
						"&:hover": {
							textDecoration: "underline",
							opacity: 0.9,
						},
					}}
				>
					{row.id}
				</Link>
			),
		},
		{
			field: "name",
			headerName: "Name",
			flex: 1,
			hideable: false,
			renderCell: ({ row }) => <span>{row.name}</span>,
		},
	];

	return (
		<DataGridBase
			rows={rows}
			columns={columns}
			storageKey={storageKey}
			idField="id"
			emptyStateMessage="No rows to display"
		/>
	);
};
