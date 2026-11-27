import ClearIcon from "@mui/icons-material/Clear";
import SearchIcon from "@mui/icons-material/Search";
import { IconButton, InputAdornment, TextField } from "@mui/material";
import type React from "react";
import type { GridSearchBoxProps } from "./types";

const GridSearchBox: React.FC<GridSearchBoxProps> = ({
	placeholder,
	text,
	onTextChange,
}) => (
	<TextField
		size="small"
		value={text}
		onChange={(event) => onTextChange(event.target.value)}
		placeholder={placeholder}
		slotProps={{
			htmlInput: { "aria-label": placeholder },
			input: {
				startAdornment: (
					<InputAdornment position="start">
						<SearchIcon fontSize="small" />
					</InputAdornment>
				),
				endAdornment:
					text === "" ? undefined : (
						<InputAdornment position="end">
							<IconButton
								size="small"
								aria-label="Clear search"
								onClick={() => onTextChange("")}
							>
								<ClearIcon fontSize="small" />
							</IconButton>
						</InputAdornment>
					),
			},
		}}
		sx={{ mr: "auto", minWidth: 240 }}
	/>
);

export default GridSearchBox;
