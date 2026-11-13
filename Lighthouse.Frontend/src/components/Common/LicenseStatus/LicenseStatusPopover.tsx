import Popover from "@mui/material/Popover";
import { useTheme } from "@mui/material/styles";
import type React from "react";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";
import LicenseStatusContent from "./LicenseStatusContent";

interface LicenseStatusPopoverProps {
	anchorEl: HTMLButtonElement | null;
	onClose: () => void;
	licenseStatus?: ILicenseStatus;
	isLoading: boolean;
	error: Error | null;
	onLicenseImported?: (newLicenseStatus: ILicenseStatus) => void;
}

const LicenseStatusPopover: React.FC<LicenseStatusPopoverProps> = ({
	anchorEl,
	onClose,
	licenseStatus,
	isLoading,
	error,
	onLicenseImported,
}) => {
	const theme = useTheme();

	return (
		<Popover
			open={Boolean(anchorEl)}
			anchorEl={anchorEl}
			onClose={onClose}
			anchorOrigin={{
				vertical: "bottom",
				horizontal: "right",
			}}
			transformOrigin={{
				vertical: "top",
				horizontal: "right",
			}}
			data-testid="license-status-popover"
			sx={{
				"& .MuiPopover-paper": {
					backgroundColor: theme.palette.background.paper,
					border: `1px solid ${theme.palette.divider}`,
					boxShadow: theme.shadows[8],
				},
			}}
		>
			<LicenseStatusContent
				licenseStatus={licenseStatus}
				isLoading={isLoading}
				error={error}
				onLicenseImported={onLicenseImported}
			/>
		</Popover>
	);
};

export default LicenseStatusPopover;
