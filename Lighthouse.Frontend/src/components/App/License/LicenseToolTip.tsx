import { Tooltip } from "@mui/material";
import type React from "react";
import { appColors } from "../../../utils/theme/colors";

interface LicenseTooltipProps {
	canUseFeature: boolean;
	defaultTooltip: string;
	premiumExtraInfo?: string;
	children: React.ReactElement;
}

export const LicenseTooltip: React.FC<LicenseTooltipProps> = ({
	canUseFeature,
	defaultTooltip,
	premiumExtraInfo,
	children,
}) => {
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
