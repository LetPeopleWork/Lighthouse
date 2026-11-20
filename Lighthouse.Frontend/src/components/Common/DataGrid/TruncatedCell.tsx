import { Box, Tooltip } from "@mui/material";
import type React from "react";
import { useCallback, useEffect, useRef, useState } from "react";

interface TruncatedCellProps {
	/** The whole text of the cell, shown in the tooltip when the cell cuts it. */
	readonly fullText: string;
	/** True while the grid has keyboard focus on this cell. */
	readonly hasFocus?: boolean;
	readonly children: React.ReactNode;
}

/** Keeps text on one line and ends it with an ellipsis where it runs out of room. */
const ONE_LINE_WITH_ELLIPSIS = {
	minWidth: 0,
	overflow: "hidden",
	textOverflow: "ellipsis",
	whiteSpace: "nowrap",
} as const;

/** True when the cell or anything drawn inside it is wider than the room it has. */
const isCut = (cell: HTMLElement | null): boolean =>
	cell !== null &&
	[cell, ...Array.from(cell.querySelectorAll<HTMLElement>("*"))].some(
		(element) => element.scrollWidth > element.clientWidth,
	);

/**
 * A text cell that stays on one line and ends cut text with an ellipsis. The full text stays in
 * the cell, so assistive technology reads it whole, and a tooltip shows it on hover or keyboard
 * focus, but only when the text is actually cut.
 */
const TruncatedCell: React.FC<TruncatedCellProps> = ({
	fullText,
	hasFocus = false,
	children,
}) => {
	const cellRef = useRef<HTMLDivElement>(null);
	const [cut, setCut] = useState(false);
	const [hovered, setHovered] = useState(false);
	const [focusedWithin, setFocusedWithin] = useState(false);

	const measure = useCallback(() => setCut(isCut(cellRef.current)), []);

	useEffect(() => {
		if (hasFocus) measure();
	}, [hasFocus, measure]);

	const cell = (
		<Box
			ref={cellRef}
			onFocus={() => {
				measure();
				setFocusedWithin(true);
			}}
			onBlur={() => setFocusedWithin(false)}
			sx={{
				flex: "1 1 auto",
				...ONE_LINE_WITH_ELLIPSIS,
				"& *": ONE_LINE_WITH_ELLIPSIS,
			}}
		>
			{children}
		</Box>
	);

	if (fullText === "") return cell;

	return (
		<Tooltip
			title={fullText}
			open={cut && (hovered || focusedWithin || hasFocus)}
			onOpen={() => {
				measure();
				setHovered(true);
			}}
			onClose={() => {
				setHovered(false);
				setFocusedWithin(false);
			}}
			disableFocusListener
		>
			{cell}
		</Tooltip>
	);
};

export default TruncatedCell;
