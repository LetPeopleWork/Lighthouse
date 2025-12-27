import UpdateIcon from "@mui/icons-material/Update";
import Badge from "@mui/material/Badge";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import type React from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useUpdateAll } from "../../../hooks/useUpdateAll";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface UpdateAllButtonProps {
	className?: string;
}

const UpdateAllButton: React.FC<UpdateAllButtonProps> = ({ className }) => {
	const theme = useTheme();
	const { handleUpdateAll, globalUpdateStatus } = useUpdateAll();
	const { canUpdateAllTeamsAndPortfolios, updateAllTeamsAndPortfoliosTooltip } =
		useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	const isDisabled =
		!canUpdateAllTeamsAndPortfolios || globalUpdateStatus.hasActiveUpdates;
	const tooltipTitle =
		updateAllTeamsAndPortfoliosTooltip ||
		`Update All ${teamsTerm} and ${portfoliosTerm}`;

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
					<UpdateIcon style={{ color: theme.palette.primary.main }} />
				</Badge>
			);
		}

		return <UpdateIcon style={{ color: theme.palette.primary.main }} />;
	};

	return (
		<Tooltip title={tooltipTitle} arrow>
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
		</Tooltip>
	);
};

export default UpdateAllButton;
