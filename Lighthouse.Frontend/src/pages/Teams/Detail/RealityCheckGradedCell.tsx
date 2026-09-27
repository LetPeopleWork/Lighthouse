import { TableCell, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors, getContrastText } from "../../../utils/theme/colors";
import { cellComparison, heldGlyph, notCheckedCell } from "./realityCheckCopy";
import { type GradedCheck, readCheck } from "./realityCheckGrading";
import { useFocusTooltip } from "./useFocusTooltip";

// Inset, so a neighbouring cell's fill never paints over the outline of the one in focus.
const focusOutline = (colour: string) => ({
	"&:focus-visible": { outline: `2px solid ${colour}`, outlineOffset: "-3px" },
});

interface WordedCellProps {
	words: string;
	outlineColour: string;
	style?: React.CSSProperties;
	children: React.ReactNode;
}

/**
 * A table cell a keyboard can stop on, whose words show in a tooltip on focus or hover. The tooltip's
 * title is also the cell's accessible name, so what is shown on demand and what a screen reader hears
 * are one and the same.
 */
const WordedCell: React.FC<Readonly<WordedCellProps>> = ({
	words,
	outlineColour,
	style,
	children,
}) => {
	const { tooltip, target } = useFocusTooltip();

	return (
		<Tooltip title={words} {...tooltip}>
			<TableCell {...target} style={style} sx={focusOutline(outlineColour)}>
				{children}
			</TableCell>
		</Tooltip>
	);
};

interface RealityCheckGradedCellProps {
	check: GradedCheck;
}

const RealityCheckGradedCell: React.FC<
	Readonly<RealityCheckGradedCellProps>
> = ({ check }) => {
	const { getTerm } = useTerminology();
	const { forecastValue, actualCompleted, held } = check;
	const { grade, miss, percentOfActual } = readCheck(check);
	const fill = appColors.forecastGrade[grade];
	const text = getContrastText(fill);

	return (
		<WordedCell
			words={cellComparison(
				{ forecastValue, actualCompleted, miss, percentOfActual },
				getTerm,
			)}
			outlineColour={text}
			style={{ backgroundColor: fill, color: text }}
		>
			{heldGlyph(held)} {forecastValue}
		</WordedCell>
	);
};

/** A level the check left out: a dash that still names itself and can still be reached by keyboard. */
export const RealityCheckNotCheckedCell: React.FC = () => {
	const theme = useTheme();
	return (
		<WordedCell
			words={notCheckedCell}
			outlineColour={theme.palette.text.primary}
		>
			—
		</WordedCell>
	);
};

export default RealityCheckGradedCell;
