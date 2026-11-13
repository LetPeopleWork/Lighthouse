import Box from "@mui/material/Box";
import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import LicenseStatusContent from "../../../components/Common/LicenseStatus/LicenseStatusContent";
import { useLicenseStatusQuery } from "../../../hooks/useLicenseRestrictions";
import type { ILicenseStatus } from "../../../models/ILicenseStatus";

/** The license of this server, read from the query the header icon reads, so an import shows in both. */
const LicenseTab: React.FC = () => {
	const queryClient = useQueryClient();
	const { data: licenseStatus, isLoading, error } = useLicenseStatusQuery();

	const handleLicenseChanged = (newLicenseStatus: ILicenseStatus) => {
		queryClient.setQueryData(["licenseStatus"], newLicenseStatus);
		queryClient
			.invalidateQueries({ queryKey: ["licenseStatus"] })
			.catch((error_) => {
				console.error("Error refreshing the license status:", error_);
			});
	};

	return (
		<InputGroup title="License">
			<Box sx={{ width: "fit-content" }}>
				<LicenseStatusContent
					licenseStatus={licenseStatus}
					isLoading={isLoading}
					error={error}
					onLicenseImported={handleLicenseChanged}
					loadFailedText="Could not load the license."
				/>
			</Box>
		</InputGroup>
	);
};

export default LicenseTab;
