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
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import { hexToRgba } from "../../../utils/theme/colors";

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

	const { getTerm } = useTerminology();
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const completedItems =
		progressableItem.totalWork - progressableItem.remainingWork;
	const completionPercentage = Number.parseFloat(
		((100 / progressableItem.totalWork) * completedItems).toFixed(2),
	);

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
						bgcolor: theme.palette.action.disabledBackground,
						"& .MuiLinearProgress-bar": {
							backgroundColor: theme.palette.primary.main,
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
								completionPercentage > 50
									? theme.palette.primary.contrastText
									: theme.palette.text.primary,
							fontWeight: "bold",
							whiteSpace: "nowrap",
							display: "flex",
							alignItems: "center",
							fontSize: `${Math.min(height * 0.6, 12)}px`,
							textShadow:
								completionPercentage > 50
									? `0px 0px 2px ${hexToRgba(theme.palette.common.black, 0.7)}`
									: "none",
						}}
					>
						{progressableItem.totalWork > 0 ? (
							`${Number.isNaN(completionPercentage) ? 0 : completionPercentage}% (${completedItems}/${progressableItem.totalWork})`
						) : (
							<Box display="flex" alignItems="center">
								Could not determine work
								<Tooltip
									title={`The remaining and total work could not be determined. This can happen if the work was added to a ${teamTerm} in your ${workTrackingSystemTerm}, but you have not defined this ${teamTerm} yet in Lighthouse.`}
									arrow
								>
									<IconButton
										size="small"
										sx={{
											marginLeft: "4px",
											padding: "0px",
											color: theme.palette.text.primary,
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
