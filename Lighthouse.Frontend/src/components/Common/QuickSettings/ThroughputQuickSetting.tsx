import DateRangeIcon from "@mui/icons-material/DateRange";
import {
	Box,
	Checkbox,
	Dialog,
	DialogContent,
	DialogTitle,
	FormControlLabel,
	IconButton,
	TextField,
	Tooltip,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";

type ThroughputQuickSettingProps = {
	useFixedDates: boolean;
	throughputHistory: number;
	startDate: Date | null;
	endDate: Date | null;
	onSave: (
		useFixedDates: boolean,
		throughputHistory: number,
		startDate: Date | null,
		endDate: Date | null,
	) => Promise<void>;
	disabled?: boolean;
};

const ThroughputQuickSetting: React.FC<ThroughputQuickSettingProps> = ({
	useFixedDates: initialUseFixedDates,
	throughputHistory: initialThroughputHistory,
	startDate: initialStartDate,
	endDate: initialEndDate,
	onSave,
	disabled = false,
}) => {
	const theme = useTheme();
	const [open, setOpen] = useState(false);
	const [useFixedDates, setUseFixedDates] = useState(initialUseFixedDates);
	const [throughputHistory, setThroughputHistory] = useState(
		initialThroughputHistory,
	);
	const [startDate, setStartDate] = useState(initialStartDate);
	const [endDate, setEndDate] = useState(initialEndDate);
	const [error, setError] = useState<string>("");

	useEffect(() => {
		if (open) {
			setUseFixedDates(initialUseFixedDates);
			setThroughputHistory(initialThroughputHistory);
			setStartDate(initialStartDate);
			setEndDate(initialEndDate);
			setError("");
		}
	}, [
		open,
		initialUseFixedDates,
		initialThroughputHistory,
		initialStartDate,
		initialEndDate,
	]);

	const getTooltipText = (): string => {
		if (!initialUseFixedDates && initialThroughputHistory <= 0) {
			return "Throughput: Not set";
		}

		if (initialUseFixedDates && initialStartDate && initialEndDate) {
			return `Throughput: Fixed dates ${initialStartDate.toISOString().split("T")[0]} to ${initialEndDate.toISOString().split("T")[0]}`;
		}

		return `Throughput: Rolling ${initialThroughputHistory} days`;
	};

	const isUnset = !initialUseFixedDates && initialThroughputHistory <= 0;

	const handleOpen = () => {
		if (!disabled) {
			setOpen(true);
		}
	};

	const handleClose = () => {
		setOpen(false);
	};

	const validate = (): boolean => {
		setError("");

		if (useFixedDates) {
			if (!startDate || !endDate) {
				setError("Start and end dates are required");
				return false;
			}

			const start = new Date(startDate);
			const end = new Date(endDate);
			const today = new Date();
			today.setHours(0, 0, 0, 0);

			if (end > today) {
				setError("End date cannot be in the future");
				return false;
			}

			const daysDiff = Math.floor(
				(end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24),
			);
			if (daysDiff < 10) {
				setError("Start date must be at least 10 days before end date");
				return false;
			}
		} else {
			if (throughputHistory < 0) {
				setError("Throughput history must be at least 1 day (or 0 to unset)");
				return false;
			}
		}

		return true;
	};

	const isDirty = (): boolean => {
		if (useFixedDates !== initialUseFixedDates) return true;
		if (!useFixedDates && throughputHistory !== initialThroughputHistory)
			return true;
		if (useFixedDates) {
			const start1 = startDate?.toISOString().split("T")[0];
			const start2 = initialStartDate?.toISOString().split("T")[0];
			const end1 = endDate?.toISOString().split("T")[0];
			const end2 = initialEndDate?.toISOString().split("T")[0];
			if (start1 !== start2 || end1 !== end2) return true;
		}
		return false;
	};

	const handleSave = async () => {
		if (!validate()) {
			return;
		}

		if (!isDirty()) {
			handleClose();
			return;
		}

		await onSave(
			useFixedDates,
			useFixedDates ? initialThroughputHistory : throughputHistory,
			useFixedDates ? startDate : null,
			useFixedDates ? endDate : null,
		);
		handleClose();
	};

	const handleKeyDown = (event: React.KeyboardEvent) => {
		if (event.key === "Enter") {
			event.preventDefault();
			handleSave();
		} else if (event.key === "Escape") {
			event.preventDefault();
			handleClose();
		}
	};

	const handleDialogClose = (_event: unknown, reason: string) => {
		if (reason === "backdropClick") {
			if (isDirty() && validate()) {
				handleSave();
			} else {
				handleClose();
			}
		} else {
			handleClose();
		}
	};

	return (
		<>
			<Tooltip title={getTooltipText()} arrow>
				<span>
					<IconButton
						size="small"
						onClick={handleOpen}
						disabled={disabled}
						aria-label={getTooltipText()}
						sx={{
							color: isUnset
								? theme.palette.action.disabled
								: theme.palette.primary.main,
							"&:hover": {
								backgroundColor: "action.hover",
							},
						}}
					>
						<DateRangeIcon />
					</IconButton>
				</span>
			</Tooltip>

			<Dialog
				open={open}
				onClose={handleDialogClose}
				onKeyDown={handleKeyDown}
				maxWidth="xs"
				fullWidth
			>
				<DialogTitle>Throughput Configuration</DialogTitle>
				<DialogContent>
					<Box sx={{ mt: 2, display: "flex", flexDirection: "column", gap: 2 }}>
						<FormControlLabel
							control={
								<Checkbox
									checked={useFixedDates}
									onChange={(e) => setUseFixedDates(e.target.checked)}
								/>
							}
							label="Use Fixed Dates"
						/>

						{!useFixedDates ? (
							<TextField
								label="Throughput History (days)"
								type="number"
								fullWidth
								value={throughputHistory}
								onChange={(e) =>
									setThroughputHistory(Number.parseInt(e.target.value, 10) || 0)
								}
								error={!!error}
								helperText={error || "Set to 0 to disable"}
								inputProps={{ min: 0 }}
							/>
						) : (
							<>
								<TextField
									label="Start Date"
									type="date"
									fullWidth
									value={startDate ? startDate.toISOString().split("T")[0] : ""}
									onChange={(e) =>
										setStartDate(
											e.target.value ? new Date(e.target.value) : null,
										)
									}
									error={!!error}
									slotProps={{
										inputLabel: { shrink: true },
									}}
								/>
								<TextField
									label="End Date"
									type="date"
									fullWidth
									value={endDate ? endDate.toISOString().split("T")[0] : ""}
									onChange={(e) =>
										setEndDate(e.target.value ? new Date(e.target.value) : null)
									}
									error={!!error}
									helperText={error}
									slotProps={{
										inputLabel: { shrink: true },
									}}
								/>
							</>
						)}
					</Box>
				</DialogContent>
			</Dialog>
		</>
	);
};

export default ThroughputQuickSetting;
