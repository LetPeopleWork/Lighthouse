import { Tooltip } from "@mui/material";
import type React from "react";
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import { appColors } from "../../../utils/theme/colors";
import { LICENSE_CHECK_FAILED_MESSAGE } from "./licenseCheckCopy";

interface LicenseTooltipProps {
	canUseFeature: boolean;
	defaultTooltip: string;
	premiumExtraInfo?: string;
	checkStatus?: LicenseCheckStatus;
	children: React.ReactElement;
}

export const LicenseTooltip: React.FC<LicenseTooltipProps> = ({
	canUseFeature,
	defaultTooltip,
	premiumExtraInfo,
	checkStatus = "ready",
	children,
}) => {
	if (checkStatus !== "ready") {
		return (
			<Tooltip
				title={checkStatus === "failed" ? LICENSE_CHECK_FAILED_MESSAGE : ""}
				arrow
			>
				{children}
			</Tooltip>
		);
	}

	const tooltipTitle = canUseFeature ? (
		defaultTooltip
	) : (
		<span>
			This feature requires a{" "}
			<a
				href="https://letpeople.work/lighthouse#lighthouse-license"
				target="_blank"
				rel="noopener noreferrer"
				style={{
					color: appColors.primary.light,
					textDecoration: "underline",
				}}
			>
				premium license.
			</a>
			{premiumExtraInfo && <> {premiumExtraInfo}</>}
		</span>
	);

	return (
		<Tooltip title={tooltipTitle} arrow>
			{children}
		</Tooltip>
	);
};
