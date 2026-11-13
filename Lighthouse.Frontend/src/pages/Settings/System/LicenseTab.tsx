import Box from "@mui/material/Box";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import LicenseStatusContent from "../../../components/Common/LicenseStatus/LicenseStatusContent";
import {
	LICENSE_UNREADABLE_MESSAGE,
	useLicenseStatusQuery,
	useStoreImportedLicense,
} from "../../../hooks/useLicenseRestrictions";

/** The license of this server, read from the query the header icon reads, so an import shows in both. */
const LicenseTab: React.FC = () => {
	const { data: licenseStatus, isLoading, error } = useLicenseStatusQuery();
	const storeImportedLicense = useStoreImportedLicense();

	return (
		<InputGroup title="License">
			<Box sx={{ width: "fit-content" }}>
				<LicenseStatusContent
					licenseStatus={licenseStatus}
					isLoading={isLoading}
					error={error}
					onLicenseImported={storeImportedLicense}
					loadFailedText={LICENSE_UNREADABLE_MESSAGE}
				/>
			</Box>
		</InputGroup>
	);
};

export default LicenseTab;
