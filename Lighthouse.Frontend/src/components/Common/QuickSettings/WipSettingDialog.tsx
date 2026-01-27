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
import { useEffect, useRef, useState } from "react";

interface WipSettingIconButtonProps {
	tooltipText: string;
	isUnset: boolean;
	disabled?: boolean;
	onClick: () => void;
}

export const WipSettingIconButton: React.FC<WipSettingIconButtonProps> = ({
	tooltipText,
	isUnset,
	disabled = false,
	onClick,
}) => {
	const theme = useTheme();

	return (
		<Tooltip title={tooltipText} arrow>
			<span>
				<IconButton
					size="small"
					onClick={onClick}
					disabled={disabled}
					aria-label={tooltipText}
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
	);
};

interface WipSettingDialogProps {
	open: boolean;
	onClose: (_event?: unknown, reason?: string) => void;
	title: string;
	children: React.ReactNode;
	onKeyDown: (event: React.KeyboardEvent) => void;
}

export const WipSettingDialog: React.FC<WipSettingDialogProps> = ({
	open,
	onClose,
	title,
	children,
	onKeyDown,
}) => {
	return (
		<Dialog
			open={open}
			onClose={onClose}
			onKeyDown={onKeyDown}
			maxWidth="sm"
			fullWidth
		>
			<DialogTitle>{title}</DialogTitle>
			<DialogContent>{children}</DialogContent>
		</Dialog>
	);
};

interface SingleWipTextFieldProps {
	label: string;
	value: number;
	onChange: (value: number) => void;
	helperText?: string;
}

export const SingleWipTextField: React.FC<SingleWipTextFieldProps> = ({
	label,
	value,
	onChange,
	helperText = "Set to 0 to disable limit",
}) => {
	return (
		<Box sx={{ mt: 2 }}>
			<TextField
				label={label}
				type="number"
				fullWidth
				value={value}
				onChange={(e) => onChange(Number.parseInt(e.target.value, 10) || 0)}
				helperText={helperText}
				slotProps={{ htmlInput: { min: 0, step: 1 } }}
			/>
		</Box>
	);
};

interface UseWipDialogStateProps<T> {
	initialValue: T;
	onOpen?: () => void;
}

export function useWipDialogState<T>({
	initialValue,
	onOpen,
}: UseWipDialogStateProps<T>) {
	const [open, setOpen] = useState(false);
	const [value, setValue] = useState(initialValue);

	const initialValueRef = useRef(initialValue);
	const onOpenRef = useRef(onOpen);

	useEffect(() => {
		initialValueRef.current = initialValue;
		onOpenRef.current = onOpen;
	});

	useEffect(() => {
		if (open) {
			setValue(initialValueRef.current);
			onOpenRef.current?.();
		}
	}, [open]);

	const handleOpen = (disabled: boolean) => {
		if (!disabled) {
			setOpen(true);
		}
	};

	const handleClose = () => {
		setOpen(false);
	};

	return {
		open,
		value,
		setValue,
		handleOpen,
		handleClose,
	};
}

interface UseWipSaveHandlersProps<T> {
	currentValue: T;
	initialValue: T;
	onSave: (value: T) => Promise<void> | void;
	onClose: () => void;
	isDirty?: (current: T, initial: T) => boolean;
}

export function useWipSaveHandlers<T>({
	currentValue,
	initialValue,
	onSave,
	onClose,
	isDirty = (current, initial) => current !== initial,
}: UseWipSaveHandlersProps<T>) {
	const checkIsDirty = () => isDirty(currentValue, initialValue);

	const handleSave = async () => {
		if (!checkIsDirty()) {
			onClose();
			return;
		}
		await onSave(currentValue);
		onClose();
	};

	const handleKeyDown = (event: React.KeyboardEvent) => {
		if (event.key === "Enter") {
			event.preventDefault();
			void handleSave();
		} else if (event.key === "Escape") {
			event.preventDefault();
			onClose();
		}
	};

	const handleDialogClose = (_event?: unknown, reason?: string) => {
		if (reason === "backdropClick") {
			if (checkIsDirty()) {
				void handleSave();
			} else {
				onClose();
			}
		} else {
			onClose();
		}
	};

	return {
		handleSave,
		handleKeyDown,
		handleDialogClose,
	};
}
