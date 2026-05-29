import RefreshIcon from "@mui/icons-material/Refresh";
import { Button } from "@mui/material";
import type React from "react";

export interface ReloadDependentDataActionProps {
	visible: boolean;
	label: string;
	onReload: () => void;
}

const ReloadDependentDataAction: React.FC<ReloadDependentDataActionProps> = ({
	visible,
	label,
	onReload,
}) => {
	if (!visible) {
		return null;
	}

	return (
		<Button
			size="small"
			variant="outlined"
			startIcon={<RefreshIcon fontSize="small" />}
			onClick={onReload}
			sx={{ mt: 2 }}
		>
			{label}
		</Button>
	);
};

export default ReloadDependentDataAction;
