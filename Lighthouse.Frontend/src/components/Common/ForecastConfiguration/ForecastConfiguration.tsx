import GppMaybeOutlinedIcon from "@mui/icons-material/GppMaybeOutlined";
import {
	Card,
	CardContent,
	Grid,
	IconButton,
	Stack,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import type { Team } from "../../../models/Team/Team";

interface ForecastConfigurationProps {
	team: Team;
}

const ForecastConfiguration: React.FC<ForecastConfigurationProps> = ({
	team,
}) => {
	const theme = useTheme();
	return (
		<Card
			elevation={0}
			sx={{
				backgroundColor: "transparent",
				borderRadius: 2,
				minWidth: 250,
				maxWidth: 300,
				border: `2px dashed ${theme.palette.primary.main}`,
				boxShadow: "none",
				p: 0,
			}}
		>
			<CardContent
				sx={{ padding: "8px 12px", "&:last-child": { paddingBottom: "8px" } }}
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
						{" "}
						<Typography
							variant="caption"
							sx={{
								display: "block",
								fontWeight: theme.emphasis.high,
								lineHeight: 1,
								color: theme.palette.primary.main,
								mb: 0.5,
							}}
						>
							Forecast Configuration:
						</Typography>
						<Typography
							variant="body2"
							sx={{
								color: theme.palette.text.primary,
								fontWeight: theme.emphasis.medium,
							}}
						>
							{`${team.throughputStartDate.toLocaleDateString()} - ${team.throughputEndDate.toLocaleDateString()}`}
						</Typography>
					</Grid>
					{team.useFixedDatesForThroughput && (
						<Tooltip title="This team is using a fixed Throughput - consider switching to a rolling history to get more realistic forecasts">
							<IconButton
								size="small"
								sx={{
									ml: "auto",
									color: theme.palette.warning.main,
									"&:hover": {
										backgroundColor: theme.effects.hover.background,
									},
								}}
							>
								<GppMaybeOutlinedIcon />
							</IconButton>
						</Tooltip>
					)}
				</Stack>
			</CardContent>
		</Card>
	);
};

export default ForecastConfiguration;
