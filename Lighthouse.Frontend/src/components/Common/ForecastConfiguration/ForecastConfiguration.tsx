import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import {
	Card,
	CardContent,
	Grid,
	IconButton,
	Stack,
	Tooltip,
	Typography,
} from "@mui/material";
import type React from "react";
import type { Team } from "../../../models/Team/Team";

interface ForecastConfigurationProps {
	team: Team;
}

const ForecastConfiguration: React.FC<ForecastConfigurationProps> = ({
	team,
}) => {
	return (
		<Card
			elevation={1}
			sx={{
				backgroundColor: (theme) => theme.palette.primary.light,
				borderRadius: 1,
				maxHeight: 40,
				minWidth: 250,
				maxWidth: 300,
			}}
		>
			<CardContent
				sx={{ padding: "4px 8px", "&:last-child": { paddingBottom: "4px" } }}
			>
				{" "}
				<Stack
					direction="row"
					sx={{
						width: "100%",
						justifyContent: "space-between",
						alignItems: "center",
					}}
				>
					<Grid>
						<Typography
							variant="caption"
							sx={{
								display: "block",
								fontWeight: 500,
								lineHeight: 1,
								color: "primary.contrastText",
							}}
						>
							Forecast Configuration:
						</Typography>
						<Typography
							variant="body2"
							sx={{
								color: "primary.contrastText",
							}}
						>
							{`${team.throughputStartDate.toLocaleDateString()} - ${team.throughputEndDate.toLocaleDateString()}`}
						</Typography>
					</Grid>
					{team.useFixedDatesForThroughput && (
						<Tooltip title="This team is using a fixed Throughput - consider switching to a rolling history to get more realistic forecasts">
							<IconButton size="small" sx={{ ml: "auto" }}>
								<GppMaybeOutlinedIcon sx={{ color: "warning.main" }} />
							</IconButton>
						</Tooltip>
					)}
				</Stack>
			</CardContent>
		</Card>
	);
};

export default ForecastConfiguration;
