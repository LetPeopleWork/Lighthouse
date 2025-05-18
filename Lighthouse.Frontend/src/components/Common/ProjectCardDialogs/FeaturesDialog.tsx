import CloseIcon from "@mui/icons-material/Close";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import { styled } from "@mui/system";
import type React from "react";
import { Link } from "react-router-dom";
import type { IFeature } from "../../../models/Feature";
import { getStateColor } from "../../../utils/theme/colors";

const FeatureItem = styled("div")(({ theme }) => ({
	padding: theme.spacing(1.5),
	borderRadius: theme.shape.borderRadius,
	marginBottom: theme.spacing(1),
	backgroundColor:
		theme.palette.mode === "light"
			? theme.palette.grey[100]
			: theme.palette.grey[800],
	"&:last-child": {
		marginBottom: 0,
	},
}));

interface FeaturesDialogProps {
	open: boolean;
	onClose: () => void;
	projectName: string;
	features: IFeature[];
}

const FeaturesDialog: React.FC<FeaturesDialogProps> = ({
	open,
	onClose,
	projectName,
	features,
}) => {
	const theme = useTheme();

	return (
		<Dialog
			open={open}
			onClose={onClose}
			maxWidth="sm"
			fullWidth
			aria-labelledby="features-dialog-title"
		>
			<DialogTitle id="features-dialog-title">
				<Stack
					direction="row"
					justifyContent="space-between"
					alignItems="center"
				>
					<Typography variant="h6">
						{projectName}: Features ({features.length})
					</Typography>
					<IconButton
						aria-label="close"
						onClick={onClose}
						edge="end"
						size="small"
						sx={{
							color: theme.palette.grey[500],
						}}
					>
						<CloseIcon />
					</IconButton>
				</Stack>
			</DialogTitle>
			<DialogContent dividers>
				{features.length > 0 ? (
					<Stack spacing={1}>
						{features.map((feature) => {
							// Calculate feature completion percentage more accurately
							const totalWork = feature.getTotalWorkForFeature();
							const remainingWork = feature.getRemainingWorkForFeature();
							const featureCompletion =
								totalWork > 0
									? Math.round(((totalWork - remainingWork) / totalWork) * 100)
									: 0;

							return (
								<FeatureItem key={feature.id}>
									<Stack
										direction="row"
										justifyContent="space-between"
										alignItems="center"
									>
										<Typography
											variant="body2"
											component={Link}
											to={feature.url ?? ""}
											sx={{
												textDecoration: "none",
												color: "inherit",
												fontWeight: "medium",
												"&:hover": {
													textDecoration: "underline",
													color: theme.palette.primary.main,
												},
											}}
										>
											{feature.name}
										</Typography>
										<Chip
											size="small"
											label={feature.state}
											color={getStateColor(feature.stateCategory)}
											variant="outlined"
										/>
									</Stack>
									<LinearProgress
										variant="determinate"
										value={featureCompletion}
										sx={{
											my: 1,
											height: 6,
											borderRadius: 3,
											bgcolor:
												theme.palette.mode === "light"
													? "rgba(0,0,0,0.1)"
													: "rgba(255,255,255,0.1)",
										}}
									/>
									<Stack
										direction="row"
										justifyContent="space-between"
										alignItems="center"
									>
										<Typography variant="caption" color="text.secondary">
											{remainingWork} of {totalWork} work items remaining
										</Typography>
										<Typography
											variant="caption"
											color={
												featureCompletion > 75
													? "success.main"
													: "text.secondary"
											}
											fontWeight={featureCompletion > 75 ? "bold" : "normal"}
										>
											{featureCompletion}% Complete
										</Typography>
									</Stack>
								</FeatureItem>
							);
						})}
					</Stack>
				) : (
					<Typography
						variant="body2"
						color="text.secondary"
						sx={{ fontStyle: "italic" }}
					>
						No features available
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default FeaturesDialog;
