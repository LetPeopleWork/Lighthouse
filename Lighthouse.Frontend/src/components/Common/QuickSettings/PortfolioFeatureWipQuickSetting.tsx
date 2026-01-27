import { Box, TextField } from "@mui/material";
import type React from "react";
import { useCallback } from "react";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import StyledLink from "../StyledLink/StyledLink";
import {
	useWipDialogState,
	useWipSaveHandlers,
	WipSettingDialog,
	WipSettingIconButton,
} from "./WipSettingDialog";

type PortfolioFeatureWipQuickSettingProps = {
	teams: ITeamSettings[];
	onSave: (teamId: number, featureWip: number) => Promise<void>;
	disabled?: boolean;
};

const PortfolioFeatureWipQuickSetting: React.FC<
	PortfolioFeatureWipQuickSettingProps
> = ({ teams: initialTeams, onSave, disabled = false }) => {
	const { getTerm } = useTerminology();

	const getInitialTeamWips = useCallback((): Map<number, number> => {
		const wips = new Map<number, number>();
		for (const team of initialTeams) {
			wips.set(team.id, team.featureWIP);
		}
		return wips;
	}, [initialTeams]);

	const {
		open,
		value: teamWips,
		setValue: setTeamWips,
		handleOpen,
		handleClose,
	} = useWipDialogState({
		initialValue: getInitialTeamWips(),
	});

	const getModifiedTeams = useCallback((): Array<{
		id: number;
		wip: number;
	}> => {
		const modified: Array<{ id: number; wip: number }> = [];
		for (const team of initialTeams) {
			const currentWip = teamWips.get(team.id);
			if (currentWip !== undefined && currentWip !== team.featureWIP) {
				modified.push({ id: team.id, wip: currentWip });
			}
		}
		return modified;
	}, [initialTeams, teamWips]);

	const handleSaveAll = useCallback(async () => {
		const modifiedTeams = getModifiedTeams();
		await Promise.all(modifiedTeams.map((team) => onSave(team.id, team.wip)));
	}, [getModifiedTeams, onSave]);

	const { handleKeyDown, handleDialogClose } = useWipSaveHandlers({
		currentValue: teamWips,
		initialValue: getInitialTeamWips(),
		onSave: handleSaveAll,
		onClose: handleClose,
		isDirty: () => getModifiedTeams().length > 0,
	});

	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const wipTerm = getTerm(TERMINOLOGY_KEYS.WIP);
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	const getTooltipText = (): string => {
		const allUnset = initialTeams.every((t) => t.featureWIP <= 0);
		if (allUnset) {
			return `${featureTerm} ${wipTerm}: Not set`;
		}
		return `${featureTerm} ${wipTerm}: ${initialTeams.length} ${teamsTerm}`;
	};

	const isUnset = initialTeams.every((t) => t.featureWIP <= 0);

	const handleTeamWipChange = (teamId: number, newWip: number) => {
		setTeamWips((prev) => {
			const updated = new Map(prev);
			updated.set(teamId, newWip);
			return updated;
		});
	};

	return (
		<>
			<WipSettingIconButton
				tooltipText={getTooltipText()}
				isUnset={isUnset}
				disabled={disabled}
				onClick={() => handleOpen(disabled)}
			/>

			<WipSettingDialog
				open={open}
				onClose={handleDialogClose}
				onKeyDown={handleKeyDown}
				title={`${featureTerm} ${wipTerm} per ${teamTerm}`}
			>
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
							slotProps={{ htmlInput: { min: 0, step: 1 } }}
						/>
					))}
				</Box>
			</WipSettingDialog>
		</>
	);
};

export default PortfolioFeatureWipQuickSetting;
