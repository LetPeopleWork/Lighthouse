import AllInclusiveIcon from "@mui/icons-material/AllInclusive";
import Badge from "@mui/material/Badge";
import CircularProgress from "@mui/material/CircularProgress";
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
	const { canUpdateAllTeamsAndPortfolios } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const isDisabled =
		!canUpdateAllTeamsAndPortfolios || globalUpdateStatus.hasActiveUpdates;

	const handleClick = async () => {
		if (!isDisabled) {
			await handleUpdateAll();
		}
	};

	const renderIcon = () => {
		if (globalUpdateStatus.hasActiveUpdates) {
			const icon = (
				<CircularProgress
					size={24}
					style={{ color: theme.palette.primary.main }}
				/>
			);

			// Show badge with count even when updates are active
			if (globalUpdateStatus.activeCount > 0) {
				return (
					<Badge
						badgeContent={globalUpdateStatus.activeCount}
						color="primary"
						max={99}
					>
						{icon}
					</Badge>
				);
			}

			return icon;
		}

		if (globalUpdateStatus.activeCount > 0) {
			return (
				<Badge
					badgeContent={globalUpdateStatus.activeCount}
					color="secondary"
					max={99}
				>
					<AllInclusiveIcon style={{ color: theme.palette.primary.main }} />
				</Badge>
			);
		}

		return <AllInclusiveIcon style={{ color: theme.palette.primary.main }} />;
	};

	return (
		<LicenseTooltip
			canUseFeature={canUpdateAllTeamsAndPortfolios}
			defaultTooltip={`Update All ${teamsTerm} and ${portfoliosTerm}`}
			premiumExtraInfo="Please obtain a premium license to update all teams and portfolios."
		>
			<span>
				<IconButton
					size="large"
					color="inherit"
					onClick={handleClick}
					disabled={isDisabled}
					aria-label={`Update All ${teamsTerm} and ${portfoliosTerm}`}
					data-testid="update-all-button"
					className={className}
				>
					{renderIcon()}
				</IconButton>
			</span>
		</LicenseTooltip>
	);
};

export default UpdateAllButton;
