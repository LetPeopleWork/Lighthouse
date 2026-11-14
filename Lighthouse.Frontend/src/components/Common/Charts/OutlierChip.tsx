import ErrorOutlineIcon from "@mui/icons-material/ErrorOutlineOutlined";
import { Chip, Tooltip, useMediaQuery } from "@mui/material";
import type React from "react";
import type { OutlierWording } from "../../../utils/charts/outlierWording";

const PHONE_WIDTH_QUERY = "(max-width:599.95px)";

interface OutlierChipProps {
	readonly wording: OutlierWording;
	readonly count: number;
	/** The sentence naming the lines above the axis, or null when none is. */
	readonly aboveTheAxis: string | null;
	/** True while the widget has no current answer; the chip then names no count and cannot be opened. */
	readonly disabled: boolean;
	/** Called each time the reader asks for the list of outliers. */
	readonly onOpen: () => void;
	/** Lets the owner of the list hand focus back to the chip when the list closes. */
	readonly chipRef: React.Ref<HTMLDivElement>;
}

const OutlierChip: React.FC<OutlierChipProps> = ({
	wording,
	count,
	aboveTheAxis,
	disabled,
	onOpen,
	chipRef,
}) => {
	const isPhone = useMediaQuery(PHONE_WIDTH_QUERY);

	const accessibleName = disabled
		? wording.chipAccessibleNameWhileLoading
		: wording.chipAccessibleName(count, aboveTheAxis);
	const words = disabled
		? wording.chipLabelWhileLoading
		: wording.chipLabel(count);

	return (
		<Tooltip title={disabled ? "" : accessibleName} arrow>
			<span>
				<Chip
					size="small"
					variant="outlined"
					color="warning"
					icon={<ErrorOutlineIcon fontSize="small" />}
					label={isPhone && !disabled ? count : words}
					disabled={disabled}
					onClick={onOpen}
					aria-label={accessibleName}
					ref={chipRef}
					data-testid="outlier-chip"
					sx={{ fontWeight: 600, fontSize: "0.65rem", height: 20 }}
				/>
			</span>
		</Tooltip>
	);
};

export default OutlierChip;
