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

interface MilestonesDialogProps {
	open: boolean;
	onClose: () => void;
	projectName: string;
	milestones: IMilestone[];
	milestoneLikelihoods?: Record<number, number>;
}

const MilestonesDialog: React.FC<MilestonesDialogProps> = ({
	open,
	onClose,
	projectName,
	milestones,
	milestoneLikelihoods = {},
}) => {
	const theme = useTheme();

	// Sort milestones by date
	const sortedMilestones = [...milestones].sort(
		(a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
	);

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
					<Typography variant="h6">
						{projectName}: Milestones ({milestones.length})
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
				{sortedMilestones.length > 0 ? (
					<Stack spacing={1}>
						{sortedMilestones.map((milestone) => {
							const isPast = isMilestonePast(milestone.date);
							const likelihood = milestoneLikelihoods[milestone.id] || 0;

							return (
								<MilestoneItem
									key={milestone.id}
									elevation={1}
									sx={{
										opacity: isPast ? 0.7 : 1,
									}}
								>
									<Stack spacing={1}>
										<Stack
											direction="row"
											justifyContent="space-between"
											alignItems="center"
										>
											<Typography
												variant="subtitle1"
												sx={{
													fontWeight: "medium",
													color: isPast ? "text.secondary" : "text.primary",
												}}
											>
												{milestone.name}
											</Typography>
											{!isPast && likelihood > 0 && (
												<Chip
													size="small"
													label={`${Math.round(likelihood)}% Likely`}
													color={getLikelihoodColor(likelihood)}
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
											<AccessTimeIcon
												fontSize="small"
												sx={{ mr: 0.5, opacity: 0.7 }}
											/>
											{isPast ? "Was due on: " : "Due on: "}
											<LocalDateTimeDisplay
												utcDate={milestone.date}
												showTime={false}
											/>
										</Typography>
									</Stack>
								</MilestoneItem>
							);
						})}
					</Stack>
				) : (
					<Typography
						variant="body2"
						color="text.secondary"
						sx={{ fontStyle: "italic" }}
					>
						No milestones defined
					</Typography>
				)}
			</DialogContent>
		</Dialog>
	);
};

export default MilestonesDialog;
