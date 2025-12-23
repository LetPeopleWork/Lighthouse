import WorkIcon from "@mui/icons-material/Work";
import {
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

type SystemWipQuickSettingProps = {
	wipLimit: number;
	onSave: (wipLimit: number) => Promise<void>;
	disabled?: boolean;
};

const SystemWipQuickSetting: React.FC<SystemWipQuickSettingProps> = ({
	wipLimit,
	onSave,
	disabled = false,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const [open, setOpen] = useState(false);
	const [wipLimitValue, setWipLimitValue] = useState(wipLimit.toString());

	const workItemTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);

	const isUnset = wipLimit < 1;

	useEffect(() => {
		setWipLimitValue(wipLimit.toString());
	}, [wipLimit]);

	const pluralWorkItemTerm = wipLimit === 1 ? workItemTerm : workItemsTerm;
	const tooltipText = isUnset
		? `System ${wipTerm} Limit: Not set`
		: `System ${wipTerm} Limit: ${wipLimit} ${pluralWorkItemTerm}`;

	const handleOpen = () => {
		if (!disabled) {
			setOpen(true);
		}
	};

	const handleSave = async () => {
		const wipNum = Number.parseInt(wipLimitValue, 10);

		if (wipNum !== wipLimit) {
			await onSave(wipNum);
		}

		setOpen(false);
	};

	const handleClose = async (
		_event: object,
		reason?: "backdropClick" | "escapeKeyDown",
	) => {
		if (reason === "escapeKeyDown") {
			setOpen(false);
			setWipLimitValue(wipLimit.toString());
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
					aria-label="System WIP Limit"
					sx={{
						color: isUnset
							? theme.palette.action.disabled
							: theme.palette.primary.main,
						"&:hover": {
							backgroundColor: "action.hover",
						},
					}}
				>
					<WorkIcon />
				</IconButton>
			</Tooltip>

			<Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
				<DialogTitle>System {wipTerm} Limit</DialogTitle>
				<DialogContent>
					<TextField
						label={`${wipTerm} Limit`}
						type="number"
						fullWidth
						value={wipLimitValue}
						onChange={(e) => setWipLimitValue(e.target.value)}
						onKeyDown={handleKeyDown}
						helperText="Set to 0 to disable limit"
						slotProps={{
							htmlInput: {
								min: 0,
								step: 1,
							},
						}}
						sx={{ mt: 2 }}
					/>
				</DialogContent>
			</Dialog>
		</>
	);
};

export default SystemWipQuickSetting;
