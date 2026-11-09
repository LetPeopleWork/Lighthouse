import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import {
	getRoleLabel,
	type SystemWideRule,
} from "../../../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";

interface SystemWideRulesNoticeProps {
	readonly unitKind: "Team" | "Portfolio";
	readonly unitId: number;
}

/** Tells a unit's admin which rules for all Teams and Portfolios also reach the unit. Read-only: only a System Admin changes those rules. */
const SystemWideRulesNotice: React.FC<SystemWideRulesNoticeProps> = ({
	unitKind,
	unitId,
}) => {
	const { rbacService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const rules = useQuery<SystemWideRule[]>({
		queryKey: ["systemWideRules", unitKind, unitId],
		queryFn: () =>
			unitKind === "Team"
				? rbacService.getTeamSystemWideRules(unitId)
				: rbacService.getPortfolioSystemWideRules(unitId),
		retry: false,
		staleTime: 0,
	});

	const teams = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfolios = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const unit = getTerm(
		unitKind === "Team" ? TERMINOLOGY_KEYS.TEAM : TERMINOLOGY_KEYS.PORTFOLIO,
	);

	if (rules.isError) {
		return (
			<Alert severity="warning">
				{`Could not check access to all ${teams} and ${portfolios}. More people may reach this ${unit} than listed below. Reload the page to try again.`}
			</Alert>
		);
	}

	if (rules.isPending) {
		return (
			<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
				<CircularProgress size={16} />
				<Typography variant="body2">
					{`Checking access to all ${teams} and ${portfolios}…`}
				</Typography>
			</Box>
		);
	}

	if (rules.data.length === 0) {
		return null;
	}

	return (
		<Alert severity="info">
			<Typography variant="subtitle2">
				{`Also reachable through access to all ${teams} and ${portfolios}`}
			</Typography>
			{rules.data.map((rule) => (
				<Typography
					variant="body2"
					key={`${rule.role}:${rule.holder}`}
				>{`${getRoleLabel(rule.role)}: group ${rule.holder}`}</Typography>
			))}
			<Typography variant="body2">
				Removing someone below does not take away what these rules give.
			</Typography>
			<Typography variant="body2">
				Only a System Admin changes them, in Settings &gt; Access.
			</Typography>
		</Alert>
	);
};

export default SystemWideRulesNotice;
