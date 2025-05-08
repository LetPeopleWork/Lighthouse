import AccessTimeIcon from "@mui/icons-material/AccessTime";
import CloseIcon from "@mui/icons-material/Close";
import Chip from "@mui/material/Chip";
import Dialog from "@mui/material/Dialog";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import IconButton from "@mui/material/IconButton";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import { styled } from "@mui/system";
import type React from "react";
import { Link } from "react-router-dom";
import type { IFeature } from "../../../models/Feature";
import type { IMilestone } from "../../../models/Project/Milestone";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";

const MilestoneItem = styled(Paper)(({ theme }) => ({
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

interface SingleMilestoneViewProps {
	milestone: IMilestone;
	isPast: boolean;
	getLikelihoodColor: (likelihood: number) => "success" | "warning" | "error";
	features?: IFeature[];
}

const SingleMilestoneView: React.FC<SingleMilestoneViewProps> = ({
	milestone,
	isPast,
	getLikelihoodColor,
	features = [],
}) => {
	const theme = useTheme();

	const milestoneLikelihood =
		features.length > 0
			? Math.min(
					...features.map((feature) =>
						feature.getMilestoneLikelihood(milestone.id),
					),
				)
			: 0;

	return (
		<Stack spacing={2}>
			<MilestoneItem elevation={1}>
				<Stack spacing={1}>
					<Stack
						direction="row"
						justifyContent="space-between"
						alignItems="center"
					>
						<Typography
							variant="h6"
							sx={{
								fontWeight: "medium",
								color: isPast ? "text.secondary" : "text.primary",
							}}
						>
							{milestone.name}
						</Typography>
						{!isPast && milestoneLikelihood > 0 && (
							<Chip
								size="small"
								label={`${Math.round(milestoneLikelihood)}% Likely`}
								color={getLikelihoodColor(milestoneLikelihood)}
								variant="outlined"
							/>
						)}
					</Stack>
					<Typography
						variant="body2"
						color="text.secondary"
						sx={{
							display: "flex",
							alignItems: "center",
							fontStyle: isPast ? "italic" : "normal",
						}}
					>
						<AccessTimeIcon fontSize="small" sx={{ mr: 0.5, opacity: 0.7 }} />
						{isPast ? "Was due on: " : "Due on: "}
						<LocalDateTimeDisplay utcDate={milestone.date} showTime={false} />
					</Typography>
				</Stack>
			</MilestoneItem>

			{features && features.length > 0 ? (
				<Stack spacing={1}>
					{features
						.filter((feature) => feature.stateCategory !== "Done")
						.map((feature) => {
							const likelihood = feature.getMilestoneLikelihood(milestone.id);

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
											label={`${Math.round(likelihood)}%`}
											color={getLikelihoodColor(likelihood)}
											variant="outlined"
										/>
									</Stack>
									<Typography variant="caption" color="text.secondary">
										{feature.getRemainingWorkForFeature()} items remaining
									</Typography>
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
					No features associated with this milestone
				</Typography>
			)}
		</Stack>
	);
};

interface MilestonesDialogProps {
	open: boolean;
	onClose: () => void;
	projectName: string;
	milestones: IMilestone[];
	selectedMilestoneId: number | null;
	features?: IFeature[];
}

const MilestonesDialog: React.FC<MilestonesDialogProps> = ({
	open,
	onClose,
	projectName,
	milestones,
	selectedMilestoneId,
	features = [],
}) => {
	const theme = useTheme();

	// Function to get an appropriate color based on likelihood
	const getLikelihoodColor = (likelihood: number) => {
		if (likelihood >= 85) return "success";
		if (likelihood >= 60) return "warning";
		return "error";
	};

	// Check if a milestone is in the past
	const isMilestonePast = (date: Date) => {
		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const milestoneDate = new Date(date);
		milestoneDate.setHours(0, 0, 0, 0);
		return milestoneDate < today;
	};

	// Find the selected milestone
	const selectedMilestone =
		selectedMilestoneId !== null
			? milestones.find((m) => m.id === selectedMilestoneId)
			: null;

	// Dialog title for the specific milestone
	const dialogTitle = selectedMilestone
		? `${projectName}: ${selectedMilestone.name} Milestone`
		: `${projectName}: Milestones`;

	return (
		<Dialog
			open={open}
			onClose={onClose}
			maxWidth="sm"
			fullWidth
			aria-labelledby="milestones-dialog-title"
		>
			<DialogTitle id="milestones-dialog-title">
				<Stack
					direction="row"
					justifyContent="space-between"
					alignItems="center"
				>
					<Typography variant="h6">{dialogTitle}</Typography>
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
				{selectedMilestone ? (
					// View for the specific milestone
					<SingleMilestoneView
						milestone={selectedMilestone}
						isPast={isMilestonePast(selectedMilestone.date)}
						getLikelihoodColor={getLikelihoodColor}
						features={features}
					/>
				) : (
					<Typography
						variant="body2"
						color="text.secondary"
						sx={{ fontStyle: "italic" }}
					>
						No milestone selected
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default MilestonesDialog;
