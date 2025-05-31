import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from "@mui/material";
import type React from "react";

interface ImportConfigurationDialogProps {
	open: boolean;
	onClose: () => void;
}

const ImportConfigurationDialog: React.FC<ImportConfigurationDialogProps> = ({
	open,
	onClose,
}) => {
	return (
		<Dialog
			open={open}
			onClose={onClose}
			aria-labelledby="import-configuration-title"
			data-testid="import-configuration-dialog"
		>
			<DialogTitle id="import-configuration-title">
				Import Configuration
			</DialogTitle>
			<DialogContent>
				<DialogContentText>
					Import configuration settings from a file.
				</DialogContentText>
				{/* Content will be added in the future */}
			</DialogContent>
			<DialogActions>
				<Button onClick={onClose} color="primary">
					Cancel
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default ImportConfigurationDialog;
