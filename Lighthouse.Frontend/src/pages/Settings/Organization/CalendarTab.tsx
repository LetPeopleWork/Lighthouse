import Box from "@mui/material/Box";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { useLicenseStatusQuery } from "../../../hooks/useLicenseRestrictions";
import BlackoutSettings from "../System/BlackoutSettings";

const whatIsKnownAbout = (
	license: ReturnType<typeof useLicenseStatusQuery>,
): "loading" | "failed" | undefined => {
	if (license.isError || license.data === null) return "failed";
	if (license.data === undefined) return "loading";
	return undefined;
};

const CalendarTab: React.FC = () => {
	const license = useLicenseStatusQuery();
	const licenseUnknown = whatIsKnownAbout(license);

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
