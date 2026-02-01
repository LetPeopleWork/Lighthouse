import CheckBoxIcon from "@mui/icons-material/CheckBox";
import ClearIcon from "@mui/icons-material/Clear";
import { Button, Checkbox, Link } from "@mui/material";
import { useGridApiContext } from "@mui/x-data-grid";
import type React from "react";
import { useMemo } from "react";
import type { StateCategory } from "../../../models/WorkItem";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import type { FeatureGridProps, FeatureGridRow } from "./types";

// Toolbar button components that use GridApiContext
const SelectAllButton: React.FC<{
	selectedFeatureIds: number[];
	onChange: (ids: number[]) => void;
}> = ({ selectedFeatureIds, onChange }) => {
	const apiRef = useGridApiContext();

	const handleSelectAll = () => {
		// Get the sorted and filtered row IDs
		const sortedRowIds = apiRef.current.getSortedRowIds() as number[];
		const filterModel = apiRef.current.state.filter?.filterModel;

		let visibleRowIds = sortedRowIds;

		// If there are active filters, we need to filter the row IDs
		if (filterModel?.items && filterModel.items.length > 0) {
			visibleRowIds = sortedRowIds.filter((id) => {
				// Check if this row passes all filter conditions
				const row = apiRef.current.getRow(id);
				if (!row) return false;

				return filterModel.items.every((filterItem) => {
					if (!filterItem.field || !filterItem.operator) return true;

					const cellValue = apiRef.current.getCellValue(id, filterItem.field);
					const cellValueStr = String(cellValue || "").toLowerCase();
					const filterValue = String(filterItem.value || "").toLowerCase();

					switch (filterItem.operator) {
						case "contains":
							return cellValueStr.includes(filterValue);
						case "equals":
							return cellValueStr === filterValue;
						case "startsWith":
							return cellValueStr.startsWith(filterValue);
						case "endsWith":
							return cellValueStr.endsWith(filterValue);
						case "isEmpty":
							return !cellValueStr;
						case "isNotEmpty":
							return !!cellValueStr;
						default:
							return true;
					}
				});
			});
		}

		const newSelection = [...selectedFeatureIds];

		// Add only new IDs that aren't already selected
		for (const id of visibleRowIds) {
			if (!newSelection.includes(id)) {
				newSelection.push(id);
			}
		}

		onChange(newSelection);
	};

	return (
		<Button
			size="small"
			startIcon={<CheckBoxIcon fontSize="small" />}
			onClick={handleSelectAll}
			sx={{ ml: 1 }}
		>
			Select All
		</Button>
	);
};

const ClearSelectionButton: React.FC<{ onChange: (ids: number[]) => void }> = ({
	onChange,
}) => {
	const handleClear = () => {
		onChange([]);
	};

	return (
		<Button
			size="small"
			startIcon={<ClearIcon fontSize="small" />}
			onClick={handleClear}
			sx={{ ml: 1 }}
		>
			Clear Selection
		</Button>
	);
};

/**
 * FeatureGrid - A reusable grid component for displaying features with selection capability.
 *
 * Supports two modes:
 * - "selectable": User can toggle checkboxes to select/deselect features
 * - "readonly": All features are shown as selected with disabled (dimmed) checkboxes
 */
export const FeatureGrid: React.FC<FeatureGridProps> = ({
	features,
	selectedFeatureIds,
	onChange,
	storageKey,
	mode = "selectable",
}) => {
	const isReadonly = mode === "readonly";

	const eligibleFeatures = useMemo(() => {
		const validStates: Set<StateCategory> = new Set(["ToDo", "Doing", "Done"]);
		const filtered = features.filter((feature) =>
			validStates.has(feature.stateCategory),
		);

		// Sort: active features first, completed features at bottom
		return filtered.sort((a, b) => {
			const aIsDone = a.stateCategory === "Done";
			const bIsDone = b.stateCategory === "Done";

			if (aIsDone === bIsDone) {
				return 0; // Keep original order within same category
			}

			return aIsDone ? 1 : -1; // Done features go to the bottom
		});
	}, [features]);

	// Transform features into rows for DataGrid
	const rows: FeatureGridRow[] = useMemo(() => {
		return eligibleFeatures.map((feature) => ({
			id: feature.id,
			reference: feature.referenceId,
			name: feature.name,
			selected: isReadonly || selectedFeatureIds.includes(feature.id),
			feature,
		}));
	}, [eligibleFeatures, selectedFeatureIds, isReadonly]);

	const handleAddToSelection = (featureId: number) => {
		if (isReadonly || !onChange) return;
		const newSelection = [...selectedFeatureIds, featureId];
		onChange(newSelection);
	};

	const handleRemoveFromSelection = (featureId: number) => {
		if (isReadonly || !onChange) return;
		const newSelection = selectedFeatureIds.filter((id) => id !== featureId);
		onChange(newSelection);
	};

	const columns: DataGridColumn<FeatureGridRow>[] = [
		{
			field: "selected",
			headerName: "Selected",
			width: 120,
			hideable: false,
			sortable: true,
			valueGetter: (_value, row) => row.selected,
			renderCell: ({ row }) => (
				<Checkbox
					checked={row.selected}
					onChange={(event) =>
						event.target.checked
							? handleAddToSelection(row.id)
							: handleRemoveFromSelection(row.id)
					}
					disabled={isReadonly}
					aria-label={`Select ${row.name}`}
					sx={isReadonly ? { opacity: 0.6 } : undefined}
				/>
			),
		},
		{
			field: "id",
			headerName: "Reference",
			flex: 1,
			hideable: false,
			valueGetter: (_value, row) => row.reference,
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
					{row.reference}
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
		{
			field: "state",
			headerName: "State",
			width: 100,
			hideable: true,
			valueGetter: (_value, row) => row.feature.state,
			renderCell: ({ row }) => <span>{row.feature.state}</span>,
		},
	];

	const toolbarActions = isReadonly ? undefined : (
		<>
			<SelectAllButton
				selectedFeatureIds={selectedFeatureIds}
				onChange={onChange ?? (() => {})}
			/>
			<ClearSelectionButton onChange={onChange ?? (() => {})} />
		</>
	);

	return (
		<DataGridBase
			rows={rows}
			columns={columns}
			storageKey={storageKey}
			idField="id"
			emptyStateMessage="No rows to display"
			toolbarActions={toolbarActions}
		/>
	);
};
