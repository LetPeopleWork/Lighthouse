import { Chip, type SxProps, useTheme } from "@mui/material";
import type React from "react";
import { hexToRgba } from "../../../utils/theme/colors";

interface LegendChipProps {
	label: string;
	color: string;
	visible?: boolean;
	onToggle?: () => void;
	sx?: SxProps;
}

const LegendChip: React.FC<LegendChipProps> = ({
	label,
	color,
	visible = true,
	onToggle,
	sx,
}) => {
	const theme = useTheme();
	const bg = visible ? color : hexToRgba(color, 0.3);

	// Always use white text when enabled (both light and dark modes)
	// Always use dark text when disabled (both light and dark modes)
	const labelColor = visible
		? theme.palette.common.white
		: theme.palette.text.primary;

	// Add solid border when disabled
	const borderStyle = visible ? "none" : `2px solid ${color}`;

	return (
		<Chip
			clickable
			label={label}
			onClick={onToggle}
			variant="filled"
			sx={{
				backgroundColor: bg,
				color: labelColor,
				cursor: "pointer",
				border: borderStyle,
				"&:hover": {
					backgroundColor: visible ? color : hexToRgba(color, 0.4),
				},
				...sx,
			}}
			aria-pressed={visible}
			aria-label={`${label} visibility toggle`}
		/>
	);
};

export default LegendChip;
