import {
	Autocomplete,
	Chip,
	CircularProgress,
	Stack,
	TextField,
	Typography,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useRef, useState } from "react";

interface ItemListManagerProps {
	title: string;
	items: string[];
	onAddItem: (item: string) => void;
	onRemoveItem: (item: string) => void;
	suggestions?: string[];
	isLoading?: boolean;
}

const ItemListManager: React.FC<ItemListManagerProps> = ({
	title,
	items,
	onAddItem,
	onRemoveItem,
	suggestions = [],
	isLoading = false,
}) => {
	const [inputValue, setInputValue] = useState<string>("");
	const [highlightedOption, setHighlightedOption] = useState<string | null>(
		null,
	);
	const inputRef = useRef<HTMLInputElement>(null);

	const filteredSuggestions = suggestions.filter(
		(suggestion) => !items.includes(suggestion),
	);

	const findCaseInsensitiveSuggestion = (value: string): string | null => {
		const normalizedValue = value.trim().toLowerCase();
		const match = filteredSuggestions.find(
			(suggestion) => suggestion.toLowerCase() === normalizedValue,
		);
		return match ?? null;
	};

	const addItem = (value: string) => {
		if (value.trim()) {
			// Check if there's a case-insensitive match with a suggestion
			const suggestionMatch = findCaseInsensitiveSuggestion(value);

			// If there's a match, use the original casing from suggestions
			onAddItem(suggestionMatch ?? value.trim());
			setInputValue("");
			setHighlightedOption(null);
		}
	};

	return (
		<Grid container spacing={2}>
			<Grid size={{ xs: 12 }}>
				<Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
					{items
						.filter((item) => item.trim())
						.map((item) => (
							<Chip
								key={item}
								label={item}
								onDelete={() => onRemoveItem(item)}
								color="primary"
								variant="outlined"
							/>
						))}
				</Stack>
			</Grid>
			<Grid size={{ xs: 12 }}>
				{suggestions.length > 0 ? (
					<>
						<Autocomplete
							freeSolo
							options={filteredSuggestions}
							inputValue={inputValue}
							value={null}
							onInputChange={(_, newInputValue) => {
								setInputValue(newInputValue);
							}}
							onChange={(_, selectedOption) => {
								// This only fires when an option is selected from the dropdown with mouse
								if (selectedOption && typeof selectedOption === "string") {
									addItem(selectedOption);
								}
							}}
							onHighlightChange={(_, option) => {
								// Track the currently highlighted option
								setHighlightedOption(option);
							}}
							disableCloseOnSelect={false}
							blurOnSelect={true}
							renderInput={(params) => (
								<TextField
									{...params}
									label={`New ${title}`}
									fullWidth
									margin="normal"
									inputRef={inputRef}
									onKeyDown={(e) => {
										if (e.key === "Enter") {
											e.preventDefault();
											e.stopPropagation();

											// If an option is highlighted in the dropdown, use that
											if (highlightedOption) {
												addItem(highlightedOption);
											} else if (inputValue.trim()) {
												// Otherwise use the input value
												addItem(inputValue);
											}
										}
									}}
									slotProps={{
										input: {
											...params.InputProps,
											endAdornment: (
												<>
													{isLoading ? (
														<CircularProgress color="inherit" size={20} />
													) : null}
													{params.InputProps.endAdornment}
												</>
											),
										},
									}}
								/>
							)}
						/>
						<Typography variant="caption" color="text.secondary">
							Type to select from existing {title.toLowerCase()} or add a new
							one. Press Enter to add.
						</Typography>
					</>
				) : (
					<>
						<TextField
							label={`New ${title}`}
							fullWidth
							margin="normal"
							value={inputValue}
							onChange={(e) => setInputValue(e.target.value)}
							onKeyDown={(e) => {
								if (e.key === "Enter" && inputValue.trim()) {
									e.preventDefault();
									addItem(inputValue);
								}
							}}
							inputRef={inputRef}
							slotProps={{
								input: {
									endAdornment: isLoading ? (
										<CircularProgress color="inherit" size={20} />
									) : null,
								},
							}}
						/>
						<Typography variant="caption" color="text.secondary">
							Type a new {title.toLowerCase()} and press Enter to add.
						</Typography>
					</>
				)}
			</Grid>
		</Grid>
	);
};

export default ItemListManager;
