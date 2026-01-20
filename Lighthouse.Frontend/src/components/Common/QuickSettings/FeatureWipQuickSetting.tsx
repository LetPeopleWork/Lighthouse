import AssignmentIcon from "@mui/icons-material/Assignment";
import {
	Box,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	TextField,
	Tooltip,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

type FeatureWipQuickSettingProps = {
	featureWip: number;
	onSave: (featureWip: number) => Promise<void>;
	disabled?: boolean;
};

const FeatureWipQuickSetting: React.FC<FeatureWipQuickSettingProps> = ({
	featureWip: initialFeatureWip,
	onSave,
	disabled = false,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const [open, setOpen] = useState(false);
	const [featureWip, setFeatureWip] = useState(initialFeatureWip);

	useEffect(() => {
		if (open) {
			setFeatureWip(initialFeatureWip);
		}
	}, [open, initialFeatureWip]);

	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);

	const getItemTypeTerm = (count: number): string => {
		if (count === 1) {
			return featureTerm;
		}

		return featuresTerm;
	};

	const getTooltipText = (): string => {
		if (initialFeatureWip <= 0) {
			return `${featureTerm} ${wipTerm}: Not set`;
		}
		return `${featureTerm} ${wipTerm}: ${initialFeatureWip} ${getItemTypeTerm(initialFeatureWip)}`;
	};

	const isUnset = initialFeatureWip <= 0;

	const handleOpen = () => {
		if (!disabled) {
			setOpen(true);
		}
	};

	const handleClose = () => {
		setOpen(false);
	};

	const isDirty = (): boolean => {
		return featureWip !== initialFeatureWip;
	};

	const handleSave = async () => {
		if (!isDirty()) {
			handleClose();
			return;
		}

		await onSave(featureWip);
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
			if (isDirty()) {
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
						<AssignmentIcon />
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
				<DialogTitle>Feature WIP Limit</DialogTitle>
				<DialogContent>
					<Box sx={{ mt: 2 }}>
						<TextField
							label="Feature WIP"
							type="number"
							fullWidth
							value={featureWip}
							onChange={(e) =>
								setFeatureWip(Number.parseInt(e.target.value, 10) || 0)
							}
							helperText="Set to 0 to disable limit"
							slotProps={{ htmlInput: { min: 0, step: 1 } }}
						/>
					</Box>
				</DialogContent>
			</Dialog>
		</>
	);
};

export default FeatureWipQuickSetting;
