import MoreVertIcon from "@mui/icons-material/MoreVert";
import {
	ClickAwayListener,
	IconButton,
	MenuItem,
	MenuList,
	Paper,
	Popper,
	Tooltip,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useRef, useState } from "react";

interface RowActionsMenuProps {
	rowName: string;
	onDetails: () => void;
	onEdit?: () => void;
	onClone?: () => void;
	onDelete?: () => void;
}

// The menu is drawn outside the grid, but React still passes its events up to the grid cell, which
// would then focus the cell and scroll the table to its end.
const keepFromTheGrid = (event: React.SyntheticEvent) =>
	event.stopPropagation();

// The browser would scroll the sticky button to where it sits before it sticks, so focus it by hand.
const focusWithoutScrolling = (event: React.MouseEvent<HTMLElement>) => {
	keepFromTheGrid(event);
	event.preventDefault();
	event.currentTarget.focus({ preventScroll: true });
};

const ARROW_KEYS = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"]);

// Enter and Space open the menu through the button; the arrow keys stay the grid's, so the
// keyboard can keep moving between cells.
const keepFromTheGridUnlessArrow = (event: React.KeyboardEvent) => {
	if (!ARROW_KEYS.has(event.key)) {
		keepFromTheGrid(event);
	}
};

const RowActionsMenu: React.FC<RowActionsMenuProps> = ({
	rowName,
	onDetails,
	onEdit,
	onClone,
	onDelete,
}) => {
	const theme = useTheme();
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);
	const button = useRef<HTMLButtonElement>(null);
	const close = () => setAnchor(null);

	const items: { label: string; run: (() => void) | undefined }[] = [
		{ label: "Details", run: onDetails },
		{ label: "Edit", run: onEdit },
		{ label: "Clone", run: onClone },
		{ label: "Delete", run: onDelete },
	];

	return (
		<>
			<Tooltip title="Actions">
				<IconButton
					size="small"
					ref={button}
					aria-label={`Actions for ${rowName}`}
					aria-haspopup="menu"
					aria-expanded={anchor !== null}
					onClick={(event) => {
						keepFromTheGrid(event);
						setAnchor(event.currentTarget);
					}}
					onMouseDown={focusWithoutScrolling}
					onFocus={keepFromTheGrid}
					onKeyDown={keepFromTheGridUnlessArrow}
				>
					<MoreVertIcon fontSize="small" />
				</IconButton>
			</Tooltip>
			<Popper
				open={anchor !== null}
				anchorEl={anchor}
				placement="bottom-end"
				sx={{ zIndex: theme.zIndex.modal }}
			>
				<Paper
					elevation={8}
					onFocus={keepFromTheGrid}
					onClick={keepFromTheGrid}
					onMouseDown={focusWithoutScrolling}
					onKeyDown={keepFromTheGrid}
				>
					<ClickAwayListener onClickAway={close}>
						<MenuList
							autoFocusItem
							onKeyDown={(event) => {
								if (event.key === "Escape" || event.key === "Tab") {
									if (event.key === "Escape") {
										event.preventDefault();
										button.current?.focus({ preventScroll: true });
									}
									close();
								}
							}}
						>
							{items.map(({ label, run }) =>
								run ? (
									<MenuItem
										key={label}
										onClick={() => {
											close();
											run();
										}}
									>
										{label}
									</MenuItem>
								) : null,
							)}
						</MenuList>
					</ClickAwayListener>
				</Paper>
			</Popper>
		</>
	);
};

export default RowActionsMenu;
