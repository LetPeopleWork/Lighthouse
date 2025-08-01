import DateRangeIcon from "@mui/icons-material/DateRange";
import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import { IconButton, Stack, Tooltip, useTheme } from "@mui/material";
import type React from "react";
import type { Team } from "../../../models/Team/Team";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

interface ForecastConfigurationProps {
	team: Team;
}

const ForecastConfiguration: React.FC<ForecastConfigurationProps> = ({
	team,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const throughputTerm = getTerm(TERMINOLOGY_KEYS.THROUGHPUT);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const tooltipText = `Forecast Configuration: ${team.throughputStartDate.toLocaleDateString()} - ${team.throughputEndDate.toLocaleDateString()}`;

	return (
		<Stack direction="row" spacing={0.5} alignItems="center">
			<Tooltip title={tooltipText} arrow>
				<IconButton
					size="small"
					sx={{
						color: theme.palette.primary.main,
						"&:hover": {
							backgroundColor: "action.hover",
						},
					}}
				>
					<DateRangeIcon />
				</IconButton>
			</Tooltip>
			{team.useFixedDatesForThroughput && (
				<Tooltip
					title={`This ${teamTerm} is using a fixed ${throughputTerm} - consider switching to a rolling history to get more realistic forecasts`}
				>
					<IconButton
						size="small"
						sx={{
							color: theme.palette.warning.main,
							"&:hover": {
								backgroundColor: "action.hover",
							},
						}}
					>
						<GppMaybeOutlinedIcon />
					</IconButton>
				</Tooltip>
			)}
		</Stack>
	);
};

export default ForecastConfiguration;
