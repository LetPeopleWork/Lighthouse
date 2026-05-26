import {
	Autocomplete,
	Box,
	Button,
	Chip,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useMemo } from "react";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";

interface CumulativeStateTimeItemPickerProps {
	candidates: ICumulativeStateTimeCandidateRow[];
	selectedItemIds: number[];
	onSelectionChange: (itemIds: number[]) => void;
	onOpen: () => void;
	candidatesLoaded?: boolean;
}

const matchesQuery = (
	candidate: ICumulativeStateTimeCandidateRow,
	query: string,
): boolean => {
	const needle = query.trim().toLowerCase();
	if (needle.length === 0) {
		return true;
	}
	return (
		candidate.referenceId.toLowerCase().includes(needle) ||
		candidate.title.toLowerCase().includes(needle)
	);
};

interface ParentExpandGroup {
	parentReferenceId: string;
	childIds: number[];
}

const groupByParentReference = (
	candidates: ICumulativeStateTimeCandidateRow[],
): ParentExpandGroup[] => {
	const grouped = new Map<string, number[]>();
	for (const candidate of candidates) {
		const parentReferenceId = candidate.parentReferenceId;
		if (parentReferenceId === null) {
			continue;
		}
		const existing = grouped.get(parentReferenceId) ?? [];
		grouped.set(parentReferenceId, [...existing, candidate.workItemId]);
	}
	return Array.from(grouped, ([parentReferenceId, childIds]) => ({
		parentReferenceId,
		childIds,
	}));
};

const CumulativeStateTimeItemPicker: React.FC<
	CumulativeStateTimeItemPickerProps
> = ({
	candidates,
	selectedItemIds,
	onSelectionChange,
	onOpen,
	candidatesLoaded = false,
}) => {
	const isEmpty = candidatesLoaded && candidates.length === 0;

	const selectedCandidates = useMemo(
		() =>
			candidates.filter((candidate) =>
				selectedItemIds.includes(candidate.workItemId),
			),
		[candidates, selectedItemIds],
	);

	const handleChange = useCallback(
		(selected: ICumulativeStateTimeCandidateRow[]) => {
			onSelectionChange(selected.map((candidate) => candidate.workItemId));
		},
		[onSelectionChange],
	);

	const expandableParents = useMemo(
		() => groupByParentReference(candidates),
		[candidates],
	);

	const handleExpand = useCallback(
		(childIds: number[]) => {
			const merged = Array.from(new Set([...selectedItemIds, ...childIds]));
			onSelectionChange(merged);
		},
		[onSelectionChange, selectedItemIds],
	);

	if (isEmpty) {
		return (
			<Stack spacing={1} sx={{ minWidth: 280 }}>
				<Autocomplete
					multiple
					disabled
					options={[]}
					value={[]}
					renderInput={(params) => (
						<TextField
							{...params}
							size="small"
							label="Select contributing items"
						/>
					)}
				/>
				<Typography variant="caption" color="text.secondary">
					No contributing items in this window.
				</Typography>
			</Stack>
		);
	}

	return (
		<Stack spacing={1} sx={{ minWidth: 280 }}>
			<Autocomplete
				multiple
				openOnFocus
				options={candidates}
				value={selectedCandidates}
				getOptionLabel={(option) => `${option.referenceId} — ${option.title}`}
				isOptionEqualToValue={(option, selected) =>
					option.workItemId === selected.workItemId
				}
				filterOptions={(options, state) =>
					options.filter((option) => matchesQuery(option, state.inputValue))
				}
				onOpen={onOpen}
				onChange={(_event, selected) => handleChange(selected)}
				renderOption={(props, option) => {
					const { key, ...optionProps } = props;
					return (
						<Box component="li" key={key} {...optionProps}>
							<Stack>
								<Typography variant="body2">{option.title}</Typography>
								<Typography variant="caption" color="text.secondary">
									{option.referenceId}
								</Typography>
							</Stack>
						</Box>
					);
				}}
				renderValue={(value, getItemProps) =>
					value.map((option, index) => {
						const { key, ...itemProps } = getItemProps({ index });
						return (
							<Chip
								key={key}
								{...itemProps}
								size="small"
								label={option.referenceId}
							/>
						);
					})
				}
				renderInput={(params) => (
					<TextField
						{...params}
						size="small"
						label="Select contributing items"
					/>
				)}
			/>
			{expandableParents.map((entry) => (
				<Button
					key={entry.parentReferenceId}
					size="small"
					variant="text"
					onClick={() => handleExpand(entry.childIds)}
				>
					{`Select all ${entry.childIds.length} children of ${entry.parentReferenceId}`}
				</Button>
			))}
		</Stack>
	);
};

export default CumulativeStateTimeItemPicker;
