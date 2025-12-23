import SpeedIcon from "@mui/icons-material/Speed";
import {
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	TextField,
	Tooltip,
	useTheme,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

type SleQuickSettingProps = {
	probability: number;
	range: number;
	onSave: (probability: number, range: number) => Promise<void>;
	disabled?: boolean;
	itemTypeKey?: string;
};

const SleQuickSetting: React.FC<SleQuickSettingProps> = ({
	probability,
	range,
	onSave,
	disabled = false,
	itemTypeKey = TERMINOLOGY_KEYS.WORK_ITEMS,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const [open, setOpen] = useState(false);
	const [probabilityValue, setProbabilityValue] = useState(
		probability.toString(),
	);
	const [rangeValue, setRangeValue] = useState(range.toString());
	const [probabilityError, setProbabilityError] = useState("");
	const [rangeError, setRangeError] = useState("");

	const workItemsTerm = getTerm(itemTypeKey);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);

	const isUnset = probability <= 0 || range <= 0;

	useEffect(() => {
		setProbabilityValue(probability.toString());
		setRangeValue(range.toString());
	}, [probability, range]);

	const tooltipText = isUnset
		? `${serviceLevelExpectationTerm}: Not set`
		: `${serviceLevelExpectationTerm}: ${Math.round(probability)}% of ${workItemsTerm} within ${range} days or less`;

	const handleOpen = () => {
		if (!disabled) {
			setOpen(true);
			setProbabilityError("");
			setRangeError("");
		}
	};

	const validate = (): boolean => {
		let valid = true;
		const probNum = Number.parseInt(probabilityValue, 10);
		const rangeNum = Number.parseInt(rangeValue, 10);

		// Allow setting both to 0 for "unset"
		const isBothZero = probNum === 0 && rangeNum === 0;

		if (!isBothZero) {
			if (probNum < 50 || probNum > 95) {
				setProbabilityError("Must be between 50 and 95");
				valid = false;
			} else {
				setProbabilityError("");
			}

			if (rangeNum < 1) {
				setRangeError("Must be at least 1");
				valid = false;
			} else {
				setRangeError("");
			}
		} else {
			setProbabilityError("");
			setRangeError("");
		}

		return valid;
	};

	const handleSave = async () => {
		if (!validate()) {
			return;
		}

		const probNum = Number.parseInt(probabilityValue, 10);
		const rangeNum = Number.parseInt(rangeValue, 10);

		if (probNum !== probability || rangeNum !== range) {
			await onSave(probNum, rangeNum);
		}

		setOpen(false);
	};

	const handleClose = async (
		_event: object,
		reason?: "backdropClick" | "escapeKeyDown",
	) => {
		if (reason === "escapeKeyDown") {
			setOpen(false);
			setProbabilityValue(probability.toString());
			setRangeValue(range.toString());
			setProbabilityError("");
			setRangeError("");
			return;
		}

		// Close via backdrop click
		await handleSave();
	};

	const handleKeyDown = async (event: React.KeyboardEvent<HTMLDivElement>) => {
		if (event.key === "Enter") {
			event.preventDefault();
			await handleSave();
		}
	};

	return (
		<>
			<Tooltip title={tooltipText} arrow>
				<IconButton
					size="small"
					onClick={handleOpen}
					disabled={disabled}
					aria-label="Service Level Expectation"
					sx={{
						color: isUnset
							? theme.palette.action.disabled
							: theme.palette.primary.main,
						"&:hover": {
							backgroundColor: "action.hover",
						},
					}}
				>
					<SpeedIcon />
				</IconButton>
			</Tooltip>

			<Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
				<DialogTitle>{serviceLevelExpectationTerm}</DialogTitle>
				<DialogContent>
					<Grid container spacing={2} sx={{ mt: 1 }}>
						<Grid size={{ xs: 12 }}>
							<TextField
								label="Probability (%)"
								type="number"
								fullWidth
								value={probabilityValue}
								onChange={(e) => setProbabilityValue(e.target.value)}
								onKeyDown={handleKeyDown}
								error={!!probabilityError}
								helperText={probabilityError || "Must be between 50 and 95"}
								slotProps={{
									htmlInput: {
										min: 0,
										max: 95,
										step: 1,
									},
								}}
							/>
						</Grid>
						<Grid size={{ xs: 12 }}>
							<TextField
								label="Range (in days)"
								type="number"
								fullWidth
								value={rangeValue}
								onChange={(e) => setRangeValue(e.target.value)}
								onKeyDown={handleKeyDown}
								error={!!rangeError}
								helperText={rangeError || "Must be at least 1"}
								slotProps={{
									htmlInput: {
										min: 0,
										step: 1,
									},
								}}
							/>
						</Grid>
					</Grid>
				</DialogContent>
			</Dialog>
		</>
	);
};

export default SleQuickSetting;
