import Box from "@mui/material/Box";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import {
	unreadLicenseOf,
	useLicenseStatusQuery,
} from "../../../hooks/useLicenseRestrictions";
import BlackoutSettings from "../System/BlackoutSettings";

const CalendarTab: React.FC = () => {
	const license = useLicenseStatusQuery();
	const licenseUnknown = unreadLicenseOf(license);

	return (
		<Box sx={{ mb: 4 }}>
			<InputGroup
				title="Blackout Periods & Recurring Rules"
				initiallyExpanded={true}
			>
				<BlackoutSettings
					isPremium={license.data?.canUsePremiumFeatures ?? false}
					licenseUnknown={licenseUnknown}
				/>
			</InputGroup>
		</Box>
	);
};

export default CalendarTab;
