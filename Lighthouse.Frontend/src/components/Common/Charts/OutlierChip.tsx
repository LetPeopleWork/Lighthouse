import ErrorOutlineIcon from "@mui/icons-material/ErrorOutlineOutlined";
import { Chip, Popover, Tooltip, useMediaQuery } from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import type { OutlierWording } from "../../../utils/charts/outlierWording";
import OutlierPopoverContent, {
	type OutlierRow,
} from "./OutlierPopoverContent";

const PHONE_WIDTH_QUERY = "(max-width:599.95px)";

interface OutlierChipProps {
	readonly wording: OutlierWording;
	readonly count: number;
	readonly cut: number;
	readonly median: number;
	readonly rows: readonly OutlierRow[];
	readonly moreCount: number;
	readonly aboveTheAxis: string | null;
	/** True while the widget has no current answer; the chip then names no count and cannot be opened. */
	readonly disabled: boolean;
	/** Called once each time the popover goes from closed to open. */
	readonly onOpened: () => void;
}

const OutlierChip: React.FC<OutlierChipProps> = ({
	wording,
	count,
	cut,
	median,
	rows,
	moreCount,
	aboveTheAxis,
	disabled,
	onOpened,
}) => {
	const [anchor, setAnchor] = useState<HTMLElement | null>(null);
	const isPhone = useMediaQuery(PHONE_WIDTH_QUERY);

	useEffect(() => {
		if (disabled) setAnchor(null);
	}, [disabled]);

	const open = anchor !== null && !disabled;
	const accessibleName = disabled
		? wording.chipAccessibleNameWhileLoading
		: wording.chipAccessibleName(count);
	const words = disabled
		? wording.chipLabelWhileLoading
		: wording.chipLabel(count);

	const close = () => setAnchor(null);

	return (
		<>
			<Tooltip title={disabled ? "" : accessibleName} arrow>
				<span>
					<Chip
						size="small"
						variant="outlined"
						icon={<ErrorOutlineIcon fontSize="small" />}
						label={isPhone && !disabled ? count : words}
						disabled={disabled}
						onClick={(event) => {
							if (!open) onOpened();
							setAnchor(event.currentTarget);
						}}
						aria-label={accessibleName}
						aria-haspopup="dialog"
						aria-expanded={open}
						data-testid="outlier-chip"
						sx={{ fontWeight: 600, fontSize: "0.65rem", height: 20 }}
					/>
				</span>
			</Tooltip>
			{open && (
				<Popover
					open
					anchorEl={anchor}
					onClose={close}
					anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
				>
					<OutlierPopoverContent
						wording={wording}
						count={count}
						cut={cut}
						median={median}
						rows={rows}
						moreCount={moreCount}
						aboveTheAxis={aboveTheAxis}
					/>
				</Popover>
			)}
		</>
	);
};

export default OutlierChip;
