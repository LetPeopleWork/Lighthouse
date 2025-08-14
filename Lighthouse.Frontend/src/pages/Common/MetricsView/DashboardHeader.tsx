import CalendarMonthIcon from "@mui/icons-material/CalendarMonth";
import {
	Box,
	ButtonBase,
	Popover,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import { format } from "date-fns";
import React from "react";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";

export interface DashboardHeaderProps {
	startDate: Date;
	endDate: Date;
	onStartDateChange: (date: Date | null) => void;
	onEndDateChange: (date: Date | null) => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({
	startDate,
	endDate,
	onStartDateChange,
	onEndDateChange,
}) => {
	const theme = useTheme();
	const isNarrow = useMediaQuery(theme.breakpoints.down("sm"));
	const [anchorEl, setAnchorEl] = React.useState<HTMLElement | null>(null);

	const open = Boolean(anchorEl);

	const handleOpen = (e: React.MouseEvent<HTMLElement>) =>
		setAnchorEl(e.currentTarget);
	const handleClose = () => setAnchorEl(null);

	const formatDate = (d: Date) => format(d, "dd MMM yyyy");

	return (
		<Box
			sx={{
				display: "flex",
				alignItems: "center",
				justifyContent: "space-between",
				width: "100%",
				p: 1,
				mb: 1,
			}}
		>
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

				<Tooltip title={isNarrow ? "Metrics shown for" : ""}>
					<ButtonBase
						data-testid="dashboard-date-range-toggle"
						onClick={handleOpen}
						sx={{
							display: "inline-flex",
							alignItems: "center",
							gap: 1,
							px: 1,
							py: 0.25,
							borderRadius: 1,
							transition: "background-color 150ms",
							"&:hover": { backgroundColor: theme.palette.action.hover },
						}}
					>
						<CalendarMonthIcon color="action" fontSize="small" />
						{!isNarrow && (
							<Typography
								variant="body2"
								color="text.primary"
								sx={{ fontWeight: 500 }}
							>
								{` ${formatDate(startDate)} → ${formatDate(endDate)}`}
							</Typography>
						)}
					</ButtonBase>
				</Tooltip>
			</Box>

			{/* Right side placeholder for future additions */}
			<Box sx={{ minWidth: 32 }} />

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
					onStartDateChange={(d) => {
						onStartDateChange(d);
					}}
					onEndDateChange={(d) => {
						onEndDateChange(d);
					}}
				/>
			</Popover>
		</Box>
	);
};

export default DashboardHeader;
