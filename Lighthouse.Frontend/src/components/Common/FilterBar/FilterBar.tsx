import CancelIcon from "@mui/icons-material/Cancel";
import SearchIcon from "@mui/icons-material/Search";
import {
	Box,
	IconButton,
	InputAdornment,
	Paper,
	TextField,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import { useEffect, useState } from "react";

interface FilterBarProps {
	filterText: string;
	onFilterTextChange: (newFilterText: string) => void;
	"data-testid"?: string;
}

const FilterBar: React.FC<FilterBarProps> = ({
	filterText,
	onFilterTextChange,
	"data-testid": dataTestId,
}) => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const [isFocused, setIsFocused] = useState(false);
	const [localFilterText, setLocalFilterText] = useState(filterText);

	// Sync with external filterText value
	useEffect(() => {
		setLocalFilterText(filterText);
	}, [filterText]);

	// Handle keyboard shortcut for focusing search box
	useEffect(() => {
		const handleKeyDown = (e: KeyboardEvent) => {
			// Focus on search when Ctrl+F or Cmd+F (Mac) is pressed
			if ((e.ctrlKey || e.metaKey) && e.key === "f") {
				e.preventDefault();
				document.getElementById("search-filter-bar")?.focus();
			}

			// Clear search with Escape key if focused
			if (
				e.key === "Escape" &&
				document.activeElement?.id === "search-filter-bar"
			) {
				handleClearFilter();
			}
		};

		window.addEventListener("keydown", handleKeyDown);
		return () => window.removeEventListener("keydown", handleKeyDown);
	}, []);

	const handleFilterChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const newFilterText = e.target.value;
		setLocalFilterText(newFilterText);
		onFilterTextChange(newFilterText);
	};

	const handleClearFilter = () => {
		setLocalFilterText("");
		onFilterTextChange("");
	};

	return (
		<Paper
			elevation={isFocused ? 4 : 1}
			sx={{
				p: "2px 4px",
				display: "flex",
				alignItems: "center",
				borderRadius: 2,
				mb: 3,
				transition: "all 0.2s ease-in-out",
				border: `1px solid ${isFocused ? theme.palette.primary.main : "transparent"}`,
				"&:hover": {
					boxShadow: isFocused ? theme.shadows[4] : theme.shadows[2],
				},
				maxWidth: isMobile ? "100%" : "600px",
				mx: "auto",
			}}
		>
			<InputAdornment position="start" sx={{ pl: 1 }}>
				<SearchIcon color={isFocused ? "primary" : "action"} />
			</InputAdornment>

			<Box sx={{ flexGrow: 1 }}>
				<TextField
					id="search-filter-bar"
					data-testid={dataTestId}
					placeholder={
						isMobile ? "Search" : "Search by project or team name (Ctrl+F)"
					}
					variant="standard"
					value={localFilterText}
					onChange={handleFilterChange}
					fullWidth
					onFocus={() => setIsFocused(true)}
					onBlur={() => setIsFocused(false)}
					InputProps={{
						disableUnderline: true,
						sx: {
							fontSize: "1rem",
							px: 1,
							"& input": {
								py: 1.5,
								transition: "all 0.2s",
							},
						},
					}}
				/>
			</Box>

			{localFilterText && (
				<IconButton
					size="small"
					aria-label="clear search"
					onClick={handleClearFilter}
					sx={{
						opacity: 0.7,
						transition: "all 0.2s",
						"&:hover": {
							opacity: 1,
							color: theme.palette.error.main,
						},
						mr: 0.5,
					}}
				>
					<CancelIcon fontSize="small" />
				</IconButton>
			)}
		</Paper>
	);
};

export default FilterBar;
