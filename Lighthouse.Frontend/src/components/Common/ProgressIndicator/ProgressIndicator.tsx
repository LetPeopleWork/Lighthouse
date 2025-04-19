import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import {
	Box,
	IconButton,
	LinearProgress,
	Tooltip,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import type { IProgressable } from "../../../models/IProgressable";

interface ProgressIndicatorProps {
	title: React.ReactNode;
	progressableItem: IProgressable;
	showDetails?: boolean;
	height?: number;
}

const ProgressIndicator: React.FC<ProgressIndicatorProps> = ({
	title,
	progressableItem,
	showDetails = true,
	height = 20,
}) => {
	const theme = useTheme();
	const [animatedValue, setAnimatedValue] = useState(0);

	const completedItems =
		progressableItem.totalWork - progressableItem.remainingWork;
	const completionPercentage = Number.parseFloat(
		((100 / progressableItem.totalWork) * completedItems).toFixed(2),
	);

	// Determine appropriate color based on completion percentage
	const getColorForPercentage = () => {
		// Use theme's primary color consistently instead of changing colors based on completion percentage
		return theme.palette.primary.main;
	};

	// Animate progress on component mount
	useEffect(() => {
		const timer = setTimeout(() => {
			setAnimatedValue(completionPercentage);
		}, 100);

		return () => clearTimeout(timer);
	}, [completionPercentage]);

	return (
		<Box
			sx={{
				width: "100%",
				mb: title ? 1 : 0,
			}}
		>
			{title && (
				<Typography
					variant="body2"
					color="text.secondary"
					sx={{ mb: 0.5, display: "flex", alignItems: "center" }}
				>
					{title}
				</Typography>
			)}

			<Box sx={{ position: "relative", display: "flex", alignItems: "center" }}>
				<LinearProgress
					variant="determinate"
					value={Number.isNaN(animatedValue) ? 0 : animatedValue}
					sx={{
						width: "100%",
						height: `${height}px`,
						borderRadius: height / 3,
						bgcolor:
							theme.palette.mode === "dark"
								? "rgba(255,255,255,0.1)"
								: "rgba(0,0,0,0.05)",
						"& .MuiLinearProgress-bar": {
							backgroundColor: getColorForPercentage(),
							transition: "transform 1s ease-out",
						},
					}}
				/>

				{showDetails && (
					<Typography
						variant="caption"
						sx={{
							position: "absolute",
							left: "50%",
							transform: "translateX(-50%)",
							color:
								theme.palette.mode === "dark"
									? "#fff"
									: completionPercentage > 50
										? "#fff"
										: theme.palette.text.primary,
							fontWeight: "bold",
							whiteSpace: "nowrap",
							display: "flex",
							alignItems: "center",
							fontSize: `${Math.min(height * 0.6, 12)}px`,
							textShadow:
								theme.palette.mode === "dark" || completionPercentage > 50
									? "0px 0px 2px rgba(0,0,0,0.7)"
									: "none",
						}}
					>
						{progressableItem.totalWork > 0 ? (
							`${Number.isNaN(completionPercentage) ? 0 : completionPercentage}% (${completedItems}/${progressableItem.totalWork})`
						) : (
							<Box display="flex" alignItems="center">
								Could not determine work
								<Tooltip
									title="The remaining and total work could not be determined. This can happen if the work was added to a team in your work tracking system, but you have not defined this team yet in Lighthouse."
									arrow
								>
									<IconButton
										size="small"
										sx={{
											marginLeft: "4px",
											padding: "0px",
											color:
												theme.palette.mode === "dark"
													? "#fff"
													: theme.palette.text.primary,
										}}
									>
										<InfoOutlinedIcon fontSize="small" />
									</IconButton>
								</Tooltip>
							</Box>
						)}
					</Typography>
				)}
			</Box>
		</Box>
	);
};

export default ProgressIndicator;
