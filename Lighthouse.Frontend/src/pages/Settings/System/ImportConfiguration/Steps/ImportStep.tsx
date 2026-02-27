import { Box, Button, LinearProgress, Typography } from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { IBaseSettings } from "../../../../../models/Common/BaseSettings";
import type { IPortfolioSettings } from "../../../../../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../../../../../models/Team/TeamSettings";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IConfigurationService } from "../../../../../services/Api/ConfigurationService";
import type { IPortfolioService } from "../../../../../services/Api/PortfolioService";
import type { ITeamService } from "../../../../../services/Api/TeamService";
import type { IWorkTrackingSystemService } from "../../../../../services/Api/WorkTrackingSystemService";
import { useTerminology } from "../../../../../services/TerminologyContext";
import type { ImportResults } from "../ImportResults";

interface ImportStepProps {
	newWorkTrackingSystems: IWorkTrackingSystemConnection[];
	updatedWorkTrackingSystems: IWorkTrackingSystemConnection[];
	newTeams: ITeamSettings[];
	updatedTeams: ITeamSettings[];
	newProjects: IPortfolioSettings[];
	updatedProjects: IPortfolioSettings[];
	workTrackingSystemsIdMapping: Map<number, number>;
	teamIdMapping: Map<number, number>;
	clearConfiguration: boolean;
	workTrackingSystemService: IWorkTrackingSystemService;
	teamService: ITeamService;
	projectService: IPortfolioService;
	configurationService: IConfigurationService;
	onNext: (results: ImportResults) => void;
	onCancel: () => void;
}

const ImportStep: React.FC<ImportStepProps> = ({
	newWorkTrackingSystems,
	updatedWorkTrackingSystems,
	newTeams,
	updatedTeams,
	newProjects,
	updatedProjects,
	workTrackingSystemsIdMapping,
	teamIdMapping,
	clearConfiguration,
	workTrackingSystemService,
	teamService,
	projectService,
	configurationService,
	onNext,
	onCancel,
}) => {
	const [isImporting, setIsImporting] = useState(false);

	const { getTerm } = useTerminology();
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);
	const workTrackingSystemTerm = getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM);

	const importResult: ImportResults = {
		workTrackingSystems: [],
		teams: [],
		projects: [],
	};

	const addNewWorkTrackingSystems = async () => {
		for (const system of newWorkTrackingSystems) {
			try {
				const addedConnection =
					await workTrackingSystemService.addNewWorkTrackingSystemConnection(
						system,
					);

				workTrackingSystemsIdMapping.set(
					system.id ?? 0,
					addedConnection.id ?? 0,
				);

				importResult.workTrackingSystems.push({
					entity: system,
					status: "Success",
				});
			} catch (error: unknown) {
				importResult.workTrackingSystems.push({
					entity: system,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to add ${workTrackingSystemTerm}: ${error.message}`
							: `Failed to add ${workTrackingSystemTerm}`,
				});
			}
		}
	};

	const updateExistingWorkTrackingSystems = async () => {
		for (const system of updatedWorkTrackingSystems) {
			try {
				const updatedWorkTrackingSystem =
					await workTrackingSystemService.updateWorkTrackingSystemConnection(
						system,
					);

				importResult.workTrackingSystems.push({
					entity: updatedWorkTrackingSystem,
					status: "Success",
				});
			} catch (error: unknown) {
				importResult.workTrackingSystems.push({
					entity: system,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to update ${workTrackingSystemTerm}: ${error.message}`
							: `Failed to update ${workTrackingSystemTerm}`,
				});
			}
		}
	};

	const patchWorkTrackingSystemConnectionId = (
		connectionOwner: IBaseSettings,
	) => {
		const mappedId = workTrackingSystemsIdMapping.get(
			connectionOwner.workTrackingSystemConnectionId,
		);

		if (mappedId) {
			connectionOwner.workTrackingSystemConnectionId = mappedId;
		}
	};

	const patchWorkTrackingSystemsOfTeamsAndProjects = () => {
		for (const team of newTeams.concat(updatedTeams)) {
			patchWorkTrackingSystemConnectionId(team);
		}

		for (const project of newProjects.concat(updatedProjects)) {
			patchWorkTrackingSystemConnectionId(project);
		}
	};

	const addNewTeams = async () => {
		for (const team of newTeams) {
			try {
				const validationResult = await teamService.validateTeamSettings(team);
				const addedTeam = await teamService.createTeam(team);
				teamIdMapping.set(team.id ?? 0, addedTeam.id ?? 0);

				importResult.teams.push({
					entity: addedTeam,
					status: validationResult ? "Success" : "Validation Failed",
				});
			} catch (error: unknown) {
				importResult.teams.push({
					entity: team,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to add ${teamTerm}: ${error.message}`
							: `Failed to add ${teamTerm}`,
				});
			}
		}
	};

	const updateExistingTeams = async () => {
		for (const team of updatedTeams) {
			try {
				const validationResult = await teamService.validateTeamSettings(team);

				const updatedTeam = await teamService.updateTeam(team);

				importResult.teams.push({
					entity: updatedTeam,
					status: validationResult ? "Success" : "Validation Failed",
				});
			} catch (error: unknown) {
				importResult.teams.push({
					entity: team,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to update ${teamTerm}: ${error.message}`
							: `Failed to update ${teamTerm}`,
				});
			}
		}
	};

	const patchTeamsOfProjects = () => {
		for (const project of newProjects.concat(updatedProjects)) {
			if (project.owningTeam) {
				const mappedTeamId = teamIdMapping.get(project.owningTeam.id);
				if (mappedTeamId) {
					project.owningTeam.id = mappedTeamId;
				}
			}
		}
	};

	const addNewProjects = async () => {
		for (const project of newProjects) {
			try {
				const validationResult =
					await projectService.validatePortfolioSettings(project);

				const addedProject = await projectService.createPortfolio(project);
				importResult.projects.push({
					entity: addedProject,
					status: validationResult ? "Success" : "Validation Failed",
				});
			} catch (error: unknown) {
				importResult.projects.push({
					entity: project,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to add project: ${error.message}`
							: "Failed to add project",
				});
			}
		}
	};

	const updateExistingProjects = async () => {
		for (const project of updatedProjects) {
			try {
				const validationResult =
					await projectService.validatePortfolioSettings(project);

				const updatedProject = await projectService.updatePortfolio(project);
				importResult.projects.push({
					entity: updatedProject,
					status: validationResult ? "Success" : "Validation Failed",
				});
			} catch (error: unknown) {
				importResult.projects.push({
					entity: project,
					status: "Error",
					errorMessage:
						error instanceof Error
							? `Failed to update project: ${error.message}`
							: "Failed to update project",
				});
			}
		}
	};

	const handleImport = async () => {
		setIsImporting(true);

		if (clearConfiguration) {
			await configurationService.clearConfiguration();
		}

		await addNewWorkTrackingSystems();
		await updateExistingWorkTrackingSystems();
		patchWorkTrackingSystemsOfTeamsAndProjects();

		await addNewTeams();
		await updateExistingTeams();
		patchTeamsOfProjects();

		await addNewProjects();
		await updateExistingProjects();

		setIsImporting(false);
		onNext(importResult);
	};

	return (
		<Box sx={{ width: "100%" }}>
			<Typography variant="h6" gutterBottom>
				Import Configuration
			</Typography>

			<Box sx={{ mb: 3 }}>
				<Typography variant="subtitle1" color="error" fontWeight="bold">
					Warning: Import cannot be undone
				</Typography>
			</Box>

			{isImporting ? (
				<Box sx={{ width: "100%", mt: 4, mb: 4 }}>
					<LinearProgress />
					<Typography variant="body2" sx={{ mt: 1, textAlign: "center" }}>
						Importing configuration...
					</Typography>
				</Box>
			) : (
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						alignItems: "center",
						mt: 3,
					}}
				>
					<Box>
						<Button
							variant="outlined"
							color="secondary"
							onClick={onCancel}
							disabled={isImporting}
						>
							Cancel
						</Button>
					</Box>
					<Box>
						<Button
							variant="contained"
							color="primary"
							onClick={handleImport}
							disabled={isImporting}
						>
							Import
						</Button>
					</Box>
				</Box>
			)}
		</Box>
	);
};

export default ImportStep;
