import CalendarMonthIcon from "@mui/icons-material/CalendarMonth";
import DoneIcon from "@mui/icons-material/Done";
import EditIcon from "@mui/icons-material/Edit";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import {
	Box,
	ButtonBase,
	Popover,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import { format } from "date-fns";
import React from "react";
import DateRangeSelector from "../../../components/Common/DateRangeSelector/DateRangeSelector";

export interface DashboardHeaderProps {
	startDate: Date;
	endDate: Date;
	onStartDateChange: (date: Date | null) => void;
	onEndDateChange: (date: Date | null) => void;
	// Identifier used to scope edit/hidden widget state (required)
	dashboardId: string;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({
	startDate,
	endDate,
	onStartDateChange,
	onEndDateChange,
	dashboardId,
}) => {
	const theme = useTheme();
	const isNarrow = useMediaQuery(theme.breakpoints.down("sm"));
	const [anchorEl, setAnchorEl] = React.useState<HTMLElement | null>(null);
	const [isEditing, setIsEditing] = React.useState<boolean>(() => {
		try {
			const key = `lighthouse:dashboard:${dashboardId}:edit`;
			return localStorage.getItem(key) === "1";
		} catch {
			return false;
		}
	});

	const open = Boolean(anchorEl);

	const handleOpen = (e: React.MouseEvent<HTMLElement>) =>
		setAnchorEl(e.currentTarget);
	const handleClose = () => setAnchorEl(null);

	const editStorageKey = `lighthouse:dashboard:${dashboardId}:edit`;

	const toggleEdit = () => {
		const next = !isEditing;
		setIsEditing(next);
		try {
			localStorage.setItem(editStorageKey, next ? "1" : "0");
		} catch {
			// ignore storage errors
		}
		// notify other components in this window
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
				detail: { dashboardId: dashboardId, isEditing: next },
			}),
		);
	};

	const resetLayout = () => {
		try {
			const keys = [
				`lighthouse:dashboard:${dashboardId}:layout`,
				`lighthouse:dashboard:${dashboardId}:hidden`,
				`lighthouse:dashboard:${dashboardId}:edit`,
			];
			keys.forEach((k) => localStorage.removeItem(k));
		} catch {
			// ignore storage errors
		}

		// notify other components in this window to reset their state
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:reset-layout", {
				detail: { dashboardId: dashboardId },
			}),
		);

		// also ensure edit mode is turned off everywhere
		window.dispatchEvent(
			new CustomEvent("lighthouse:dashboard:edit-mode-changed", {
				detail: { dashboardId: dashboardId, isEditing: false },
			}),
		);

		setIsEditing(false);
	};

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

			{/* Right side: edit toggle */}
			<Stack direction="row" alignItems="center" spacing={1}>
				<Tooltip title={isEditing ? "Leave Edit" : "Enter Edit Mode"}>
					<IconButton
						size="small"
						color={isEditing ? "primary" : "default"}
						onClick={toggleEdit}
						aria-pressed={isEditing}
						data-testid="dashboard-edit-toggle"
					>
						{isEditing ? (
							<DoneIcon fontSize="small" />
						) : (
							<EditIcon fontSize="small" />
						)}
					</IconButton>
				</Tooltip>

				<Tooltip title="Reset layout">
					<IconButton
						size="small"
						onClick={resetLayout}
						data-testid="dashboard-reset-layout"
					>
						<RestartAltIcon fontSize="small" />
					</IconButton>
				</Tooltip>
			</Stack>

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
