import CloudDoneIcon from "@mui/icons-material/CloudDone";
import CloudOffIcon from "@mui/icons-material/CloudOff";
import Badge from "@mui/material/Badge";
import IconButton from "@mui/material/IconButton";
import { useTheme } from "@mui/material/styles";
import Tooltip from "@mui/material/Tooltip";
import { useContext, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useRbac } from "../../../hooks/useRbac";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { IOAuthHealthDto } from "../../../services/Api/OAuthService";

const buildTooltip = (disconnectedCount: number): string => {
	if (disconnectedCount === 0) {
		return "All OAuth connections healthy";
	}
	const noun = disconnectedCount === 1 ? "connection" : "connections";
	const verb = disconnectedCount === 1 ? "needs" : "need";
	return `${disconnectedCount} OAuth ${noun} ${verb} reconnect`;
};

const OAuthHealthIcon = () => {
	const theme = useTheme();
	const { isSystemAdmin } = useRbac();
	const { oauthService } = useContext(ApiServiceContext);
	const navigate = useNavigate();

	const [health, setHealth] = useState<IOAuthHealthDto | null>(null);

	useEffect(() => {
		if (!isSystemAdmin) {
			return;
		}
		let cancelled = false;
		oauthService
			.getHealth()
			.then((dto) => {
				if (!cancelled) {
					setHealth(dto);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setHealth(null);
				}
			});
		return () => {
			cancelled = true;
		};
	}, [isSystemAdmin, oauthService]);

	if (!isSystemAdmin || health === null || health.totalOAuthConnections === 0) {
		return null;
	}

	const hasIssues = health.disconnectedCount > 0;
	const tooltip = buildTooltip(health.disconnectedCount);
	const iconColor = hasIssues
		? theme.palette.warning.main
		: theme.palette.success.main;
	const Icon = hasIssues ? CloudOffIcon : CloudDoneIcon;
	const targetConnectionId = health.firstDisconnectedConnectionId;

	const handleClick = () => {
		if (targetConnectionId !== null) {
			navigate(`/connections/${targetConnectionId}/edit`);
		}
	};

	return (
		<Tooltip title={tooltip} arrow>
			<IconButton
				size="large"
				color="inherit"
				onClick={handleClick}
				aria-label={tooltip}
				data-testid="oauth-health-icon"
			>
				<Badge
					badgeContent={hasIssues ? health.disconnectedCount : 0}
					color="warning"
					overlap="circular"
				>
					<Icon style={{ color: iconColor }} />
				</Badge>
			</IconButton>
		</Tooltip>
	);
};

export default OAuthHealthIcon;
