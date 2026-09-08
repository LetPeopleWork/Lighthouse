import AnalyticsOutlinedIcon from "@mui/icons-material/AnalyticsOutlined";
import CalendarMonthIcon from "@mui/icons-material/CalendarMonth";
import {
	Box,
	ButtonBase,
	IconButton,
	Popover,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import { format } from "date-fns";
import React from "react";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";
import { isValidDate } from "../../../utils/date/isValidDate";
import CategorySelector from "./CategorySelector";
import type { CategoryKey } from "./categoryMetadata";
import DateWindowStepper from "./DateWindowStepper";
import type { DateWindowPreset } from "./dateWindow";

export interface DashboardHeaderProps {
	startDate: Date;
	endDate: Date;
	onStartDateChange: (date: Date | null) => void;
	onEndDateChange: (date: Date | null) => void;
	presets?: readonly DateWindowPreset[];
	selectedPresetDays?: number | null;
	onSelectPreset?: (days: number) => void;
	/** What the label reads. Defaults to the committed window when no commit is outstanding. */
	pendingStartDate?: Date;
	pendingEndDate?: Date;
	isCommitPending?: boolean;
	stepDays?: number;
	canStepForward?: boolean;
	onStepWindow?: (direction: -1 | 1) => void;
	selectedCategory: CategoryKey;
	onSelectCategory: (key: CategoryKey) => void;
	showTips: boolean;
	onToggleTips: () => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({
	startDate,
	endDate,
	onStartDateChange,
	onEndDateChange,
	presets,
	selectedPresetDays,
	onSelectPreset,
	pendingStartDate,
	pendingEndDate,
	isCommitPending = false,
	stepDays,
	canStepForward = false,
	onStepWindow,
	selectedCategory,
	onSelectCategory,
	showTips,
	onToggleTips,
}) => {
	const theme = useTheme();
	const isNarrow = useMediaQuery(theme.breakpoints.down("sm"));
	const [anchorEl, setAnchorEl] = React.useState<HTMLElement | null>(null);

	const open = Boolean(anchorEl);

	const handleOpen = (e: React.MouseEvent<HTMLElement>) =>
		setAnchorEl(e.currentTarget);
	const handleClose = () => setAnchorEl(null);

	// date-fns throws on a date it cannot format, and a throw here takes the whole
	// application down with it. A missing label is a far smaller loss than that.
	const formatDate = (d: Date) =>
		isValidDate(d) ? format(d, "dd MMM yyyy") : "—";

	return (
		<Box
			sx={{
				width: "100%",
				p: 1,
				mb: 1,
			}}
		>
			<Box
				sx={{
					display: "flex",
					alignItems: "center",
					justifyContent: "space-between",
					mb: 1,
				}}
			>
				<CategorySelector
					selectedCategory={selectedCategory}
					onSelectCategory={onSelectCategory}
				/>
				<Tooltip title={showTips ? "Hide tips" : "Show tips"}>
					<IconButton
						size="small"
						onClick={onToggleTips}
						color={showTips ? "primary" : "default"}
						aria-pressed={showTips}
						data-testid="metrics-tips-toggle"
					>
						<AnalyticsOutlinedIcon fontSize="small" />
					</IconButton>
				</Tooltip>
			</Box>

			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				{!isNarrow && (
					<Typography
						variant="subtitle2"
						color="text.secondary"
						sx={{ mr: 0.5 }}
					>
						Metrics shown for:
					</Typography>
				)}

				{onStepWindow && stepDays !== undefined && (
					// The pair ships as one row component, so its own box is dissolved into this row
					// and the date label placed between the two arrows. An arrow then reads as moving
					// the window it points at rather than as a control that happens to stand beside it.
					<Box
						sx={{
							display: "contents",
							"& > *": { display: "contents" },
							"& > * > :last-of-type": { order: 1 },
						}}
					>
						<DateWindowStepper
							stepDays={stepDays}
							canStepForward={canStepForward}
							onStep={onStepWindow}
						/>
					</Box>
				)}

				<Tooltip title={isNarrow ? "Metrics shown for" : ""}>
					<ButtonBase
						data-testid="dashboard-date-range-toggle"
						data-window-pending={isCommitPending ? "true" : "false"}
						onClick={handleOpen}
						sx={{
							display: "inline-flex",
							alignItems: "center",
							gap: 1,
							px: 1,
							py: 0.25,
							borderRadius: 1,
							transition: "background-color 150ms, opacity 150ms",
							// For up to half a second after a click this label already names the new
							// window while the charts below still show the old one. Unmarked, it would
							// simply be wrong for that moment, so it reads as provisional until the
							// charts catch up.
							opacity: isCommitPending ? 0.55 : 1,
							fontStyle: isCommitPending ? "italic" : "normal",
							"&:hover": { backgroundColor: theme.palette.action.hover },
						}}
					>
						<CalendarMonthIcon color="action" fontSize="small" />
						{!isNarrow && (
							<Typography
								variant="body2"
								color="text.primary"
								sx={{ fontWeight: 500, fontStyle: "inherit" }}
							>
								{` ${formatDate(pendingStartDate ?? startDate)} → ${formatDate(pendingEndDate ?? endDate)}`}
							</Typography>
						)}
					</ButtonBase>
				</Tooltip>
			</Box>

			<Popover
				open={open}
				anchorEl={anchorEl}
				onClose={handleClose}
				anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
				transformOrigin={{ vertical: "top", horizontal: "left" }}
				disableEnforceFocus
				disablePortal
			>
				<DateRangeSelector
					startDate={startDate}
					endDate={endDate}
					onStartDateChange={onStartDateChange}
					onEndDateChange={onEndDateChange}
					presets={presets}
					selectedPresetDays={selectedPresetDays}
					onSelectPreset={onSelectPreset}
				/>
			</Popover>
		</Box>
	);
};

export default DashboardHeader;
