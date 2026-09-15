import AllInclusiveIcon from "@mui/icons-material/AllInclusive";
import IconButton from "@mui/material/IconButton";
import { useTheme } from "@mui/material/styles";
import type React from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useUpdateAll } from "../../../hooks/useUpdateAll";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { LicenseTooltip } from "../License/LicenseToolTip";

interface UpdateAllButtonProps {
	className?: string;
}

const UpdateAllButton: React.FC<UpdateAllButtonProps> = ({ className }) => {
	const theme = useTheme();
	const { handleUpdateAll, globalUpdateStatus } = useUpdateAll();
	const { licenseStatus } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const isDisabled =
		!licenseStatus?.canUsePremiumFeatures ||
		globalUpdateStatus.hasActiveUpdates;

	return (
		<LicenseTooltip
			canUseFeature={licenseStatus?.canUsePremiumFeatures ?? false}
			defaultTooltip={`Update All ${teamsTerm} and ${portfoliosTerm}`}
			premiumExtraInfo="Please obtain a premium license to update all teams and portfolios."
		>
			<span>
				<IconButton
					size="large"
					color="inherit"
					onClick={() => void handleUpdateAll()}
					disabled={isDisabled}
					aria-label={`Update All ${teamsTerm} and ${portfoliosTerm}`}
					data-testid="update-all-button"
					className={className}
				>
					<AllInclusiveIcon style={{ color: theme.palette.primary.main }} />
				</IconButton>
			</span>
		</LicenseTooltip>
	);
};

export default UpdateAllButton;
