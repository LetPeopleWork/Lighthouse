import WorkIcon from "@mui/icons-material/Work";
import {
	Box,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	TextField,
	Tooltip,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import StyledLink from "../StyledLink/StyledLink";

type PortfolioFeatureWipQuickSettingProps = {
	teams: ITeamSettings[];
	onSave: (teamId: number, featureWip: number) => Promise<void>;
	disabled?: boolean;
};

const PortfolioFeatureWipQuickSetting: React.FC<
	PortfolioFeatureWipQuickSettingProps
> = ({ teams: initialTeams, onSave, disabled = false }) => {
	const theme = useTheme();
	const [open, setOpen] = useState(false);
	const [teamWips, setTeamWips] = useState<Map<number, number>>(new Map());

	useEffect(() => {
		if (open) {
			const wips = new Map<number, number>();
			for (const team of initialTeams) {
				wips.set(team.id, team.featureWIP);
			}
			setTeamWips(wips);
		}
	}, [open, initialTeams]);

	const getTooltipText = (): string => {
		const allUnset = initialTeams.every((t) => t.featureWIP <= 0);
		if (allUnset) {
			return "Feature WIP: Not set";
		}
		return `Feature WIP: ${initialTeams.length} teams`;
	};

	const isUnset = initialTeams.every((t) => t.featureWIP <= 0);

	const handleOpen = () => {
		if (!disabled) {
			setOpen(true);
		}
	};

	const handleClose = () => {
		setOpen(false);
	};

	const getModifiedTeams = (): Array<{ id: number; wip: number }> => {
		const modified: Array<{ id: number; wip: number }> = [];
		for (const team of initialTeams) {
			const currentWip = teamWips.get(team.id);
			if (currentWip !== undefined && currentWip !== team.featureWIP) {
				modified.push({ id: team.id, wip: currentWip });
			}
		}
		return modified;
	};

	const handleSave = async () => {
		const modifiedTeams = getModifiedTeams();
		if (modifiedTeams.length === 0) {
			handleClose();
			return;
		}

		for (const team of modifiedTeams) {
			await onSave(team.id, team.wip);
		}
		handleClose();
	};

	const handleKeyDown = (event: React.KeyboardEvent) => {
		if (event.key === "Enter") {
			event.preventDefault();
			handleSave();
		} else if (event.key === "Escape") {
			event.preventDefault();
			handleClose();
		}
	};

	const handleDialogClose = (_event: unknown, reason: string) => {
		if (reason === "backdropClick") {
			const modifiedTeams = getModifiedTeams();
			if (modifiedTeams.length > 0) {
				handleSave();
			} else {
				handleClose();
			}
		} else {
			handleClose();
		}
	};

	const handleTeamWipChange = (teamId: number, newWip: number) => {
		setTeamWips((prev) => {
			const updated = new Map(prev);
			updated.set(teamId, newWip);
			return updated;
		});
	};

	return (
		<>
			<Tooltip title={getTooltipText()} arrow>
				<span>
					<IconButton
						size="small"
						onClick={handleOpen}
						disabled={disabled}
						aria-label={getTooltipText()}
						sx={{
							color: isUnset
								? theme.palette.action.disabled
								: theme.palette.primary.main,
							"&:hover": {
								backgroundColor: "action.hover",
							},
						}}
					>
						<WorkIcon />
					</IconButton>
				</span>
			</Tooltip>

			<Dialog
				open={open}
				onClose={handleDialogClose}
				onKeyDown={handleKeyDown}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>Feature WIP per Team</DialogTitle>
				<DialogContent>
					<Box
						sx={{
							display: "flex",
							flexDirection: "column",
							gap: 2,
							mt: 2,
						}}
					>
						{initialTeams.map((team) => (
							<TextField
								key={team.id}
								label={
									<StyledLink to={`/teams/${team.id}`}>{team.name}</StyledLink>
								}
								type="number"
								fullWidth
								value={teamWips.get(team.id) ?? team.featureWIP}
								onChange={(e) =>
									handleTeamWipChange(
										team.id,
										Number.parseInt(e.target.value, 10) || 0,
									)
								}
								inputProps={{ min: 0, step: 1 }}
							/>
						))}
					</Box>
				</DialogContent>
			</Dialog>
		</>
	);
};

export default PortfolioFeatureWipQuickSetting;
