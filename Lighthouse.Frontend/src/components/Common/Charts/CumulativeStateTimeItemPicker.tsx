import {
	Autocomplete,
	Box,
	Chip,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useMemo } from "react";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface CumulativeStateTimeItemPickerProps {
	candidates: ICumulativeStateTimeCandidateRow[];
	selectedItemIds: number[];
	onSelectionChange: (itemIds: number[]) => void;
	onOpen: () => void;
	candidatesLoaded?: boolean;
}

const PICKER_WIDTH = 280;

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

const CumulativeStateTimeItemPicker: React.FC<
	CumulativeStateTimeItemPickerProps
> = ({
	candidates,
	selectedItemIds,
	onSelectionChange,
	onOpen,
	candidatesLoaded = false,
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const label = `Select contributing ${workItemsTerm}`;
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

	if (isEmpty) {
		return (
			<Stack spacing={1} sx={{ width: PICKER_WIDTH }}>
				<Autocomplete
					multiple
					disabled
					options={[]}
					value={[]}
					renderInput={(params) => (
						<TextField {...params} size="small" label={label} />
					)}
				/>
				<Typography variant="caption" color="text.secondary">
					No contributing {workItemsTerm.toLowerCase()} in this window.
				</Typography>
			</Stack>
		);
	}

	return (
		<Autocomplete
			multiple
			openOnFocus
			limitTags={1}
			sx={{ width: PICKER_WIDTH }}
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
				<TextField {...params} size="small" label={label} />
			)}
		/>
	);
};

export default CumulativeStateTimeItemPicker;
