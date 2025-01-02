import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import { Button, IconButton, List, ListItem, TextField } from "@mui/material";
import Grid from "@mui/material/Grid2";
import debounce from "lodash.debounce";
import type React from "react";
import { useCallback, useState } from "react";
import { type IMilestone, Milestone } from "../../../models/Project/Milestone";
import InputGroup from "../InputGroup/InputGroup";

interface MilestonesComponentProps {
	milestones: IMilestone[];
	initiallyExpanded?: boolean;
	onAddMilestone: (milestone: IMilestone) => void;
	onRemoveMilestone: (name: string) => void;
	onUpdateMilestone: (
		name: string,
		updatedMilestone: Partial<IMilestone>,
	) => void;
}

const MilestonesComponent: React.FC<MilestonesComponentProps> = ({
	milestones,
	initiallyExpanded = true,
	onAddMilestone,
	onRemoveMilestone,
	onUpdateMilestone,
}) => {
	const [newMilestoneName, setNewMilestoneName] = useState<string>("");
	const [newMilestoneDate, setNewMilestoneDate] = useState<string>("");

	const debouncedUpdateMilestoneName = useCallback(
		debounce((name: string, updatedMilestone: Partial<IMilestone>) => {
			onUpdateMilestone(name, updatedMilestone);
		}, 700),
		[],
	);

	const handleAddMilestone = () => {
		if (newMilestoneName.trim() && newMilestoneDate) {
			const newMilestone = new Milestone(
				0,
				newMilestoneName.trim(),
				new Date(newMilestoneDate),
			);
			onAddMilestone(newMilestone);
			setNewMilestoneName("");
			setNewMilestoneDate("");
		}
	};

	const handleMilestoneNameChange = (name: string, newName: string) => {
		debouncedUpdateMilestoneName(name, { name: newName });
	};

	const handleMilestoneDateChange = (name: string, newDate: string) => {
		onUpdateMilestone(name, { date: new Date(newDate) });
	};

	return (
		<InputGroup title={"Milestones"} initiallyExpanded={initiallyExpanded}>
			<Grid container spacing={2}>
				<Grid size={{ xs: 6 }}>
					<List>
						{milestones.map((milestone) => (
							<ListItem key={milestone.name}>
								<Grid container spacing={2} alignItems="center">
									<Grid size={{ xs: 4 }}>
										<TextField
											fullWidth
											label="Milestone Name"
											defaultValue={milestone.name}
											onChange={(e) =>
												handleMilestoneNameChange(
													milestone.name,
													e.target.value,
												)
											}
										/>
									</Grid>
									<Grid size={{ xs: 4 }}>
										<TextField
											fullWidth
											label="Milestone Date"
											type="date"
											slotProps={{ inputLabel: { shrink: true } }}
											defaultValue={milestone.date.toISOString().slice(0, 10)} // Convert date to yyyy-MM-dd format
											onChange={(e) =>
												handleMilestoneDateChange(
													milestone.name,
													e.target.value,
												)
											}
										/>
									</Grid>
									<Grid size={{ xs: 4 }}>
										<IconButton
											aria-label="delete"
											onClick={() => onRemoveMilestone(milestone.name)}
										>
											<DeleteIcon />
										</IconButton>
									</Grid>
								</Grid>
							</ListItem>
						))}
					</List>
				</Grid>
				<Grid size={{ xs: 6 }}>
					<TextField
						fullWidth
						label="New Milestone Name"
						margin="normal"
						value={newMilestoneName}
						onChange={(e) => setNewMilestoneName(e.target.value)}
					/>
					<TextField
						fullWidth
						label="New Milestone Date"
						type="date"
						margin="normal"
						slotProps={{ inputLabel: { shrink: true } }}
						value={newMilestoneDate}
						onChange={(e) => setNewMilestoneDate(e.target.value)}
					/>
					<Button
						variant="contained"
						color="primary"
						startIcon={<AddIcon />}
						onClick={handleAddMilestone}
						fullWidth
						sx={{ marginTop: 2 }}
					>
						Add Milestone
					</Button>
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default MilestonesComponent;
