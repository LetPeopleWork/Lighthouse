import {
	Box,
	Dialog,
	DialogContent,
	DialogTitle,
	Step,
	StepLabel,
	Stepper,
} from "@mui/material";
import type React from "react";
import { useContext, useId, useState } from "react";
import type { IPortfolioSettings } from "../../../../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiServiceContext } from "../../../../services/Api/ApiServiceContext";
import type { ImportResults } from "./ImportResults";
import type {
	ImportChanges,
	ImportMappings,
	ImportOptions,
} from "./Steps/ImportSettingsStep";
import ImportSettingsStep from "./Steps/ImportSettingsStep";
import ImportStep from "./Steps/ImportStep";
import ImportSummaryStep from "./Steps/ImportSummaryStep";
import WorkTrackingSystemConfigurationStep from "./Steps/WorkTrackingSystemConfigurationStep";

interface ImportConfigurationDialogProps {
	open: boolean;
	onClose: () => void;
}

const ImportConfigurationDialog: React.FC<ImportConfigurationDialogProps> = ({
	open,
	onClose,
}) => {
	const [activeStep, setActiveStep] = useState(0);

	const [newWorkTrackingSystems, setNewWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [updatedWorkTrackingSystems, setUpdatedWorkTrackingSystems] = useState<
		IWorkTrackingSystemConnection[]
	>([]);
	const [newTeams, setNewTeams] = useState<ITeamSettings[]>([]);
	const [updatedTeams, setUpdatedTeams] = useState<ITeamSettings[]>([]);
	const [newProjects, setNewProjects] = useState<IPortfolioSettings[]>([]);
	const [updatedProjects, setUpdatedProjects] = useState<IPortfolioSettings[]>(
		[],
	);

	const [importResults, setImportResults] = useState<ImportResults | null>(
		null,
	);

	const [workTrackingSystemsIdMapping, setWorkTrackingSystemsIdMapping] =
		useState<Map<number, number>>(new Map());
	const [teamIdMapping, setTeamIdMapping] = useState<Map<number, number>>(
		new Map(),
	);

	const [clearConfiguration, setClearConfiguration] = useState<boolean>(false);

	const {
		configurationService,
		workTrackingSystemService,
		teamService,
		portfolioService: projectService,
	} = useContext(ApiServiceContext);

	const onCancel = () => {
		setNewWorkTrackingSystems([]);
		setUpdatedWorkTrackingSystems([]);
		setNewTeams([]);
		setUpdatedTeams([]);
		setNewProjects([]);
		setUpdatedProjects([]);
		setImportResults(null);
		setActiveStep(0);
	};

	const handleNext = () => {
		setActiveStep((prevStep) => prevStep + 1);
	};

	const handleSettingsComplete = (
		changes: ImportChanges,
		mappings: ImportMappings,
		options: ImportOptions,
	) => {
		setNewWorkTrackingSystems(changes.newWorkTrackingSystems);
		setUpdatedWorkTrackingSystems(changes.updatedWorkTrackingSystems);
		setNewTeams(changes.newTeams);
		setUpdatedTeams(changes.updatedTeams);
		setNewProjects(changes.newProjects);
		setUpdatedProjects(changes.updatedProjects);
		setClearConfiguration(options.clearConfiguration);

		setWorkTrackingSystemsIdMapping(mappings.workTrackingSystemsIdMapping);
		setTeamIdMapping(mappings.teamIdMapping);

		const secretOptionsCount = changes.newWorkTrackingSystems.reduce(
			(count, system) => {
				return (
					count +
					(system.options?.filter((option) => option.isSecret).length || 0)
				);
			},
			0,
		);

		if (secretOptionsCount === 0) {
			setActiveStep((prevStep) => prevStep + 2);
		} else {
			handleNext();
		}
	};

	const handleWorkTrackingSystemConfigurationComplete = (
		newWorkTrackingSystems: IWorkTrackingSystemConnection[],
	) => {
		setNewWorkTrackingSystems(newWorkTrackingSystems);

		handleNext();
	};

	const handleImportComplete = (importResult: ImportResults) => {
		setImportResults(importResult);
		handleNext();
	};

	const handleCloseDialog = () => {
		onCancel();
		onClose();
	};

	const steps = [
		"File Selection",
		"Secrets Configuration",
		"Import",
		"Summary",
	];

	const importConfigurationDialogTitleId = useId();

	const renderStepContent = () => {
		switch (activeStep) {
			case 0:
				return (
					<ImportSettingsStep
						configurationService={configurationService}
						onNext={handleSettingsComplete}
						onClose={onClose}
					/>
				);
			case 1:
				return (
					<WorkTrackingSystemConfigurationStep
						newWorkTrackingSystems={newWorkTrackingSystems}
						workTrackingSystemService={workTrackingSystemService}
						onNext={handleWorkTrackingSystemConfigurationComplete}
						onCancel={onCancel}
					/>
				);
			case 2:
				return (
					<ImportStep
						newWorkTrackingSystems={newWorkTrackingSystems}
						updatedWorkTrackingSystems={updatedWorkTrackingSystems}
						newTeams={newTeams}
						updatedTeams={updatedTeams}
						newProjects={newProjects}
						updatedProjects={updatedProjects}
						workTrackingSystemsIdMapping={workTrackingSystemsIdMapping}
						teamIdMapping={teamIdMapping}
						clearConfiguration={clearConfiguration}
						workTrackingSystemService={workTrackingSystemService}
						teamService={teamService}
						projectService={projectService}
						configurationService={configurationService}
						onNext={handleImportComplete}
						onCancel={onCancel}
					/>
				);
			case 3:
				return (
					importResults && (
						<ImportSummaryStep
							importResults={importResults}
							teamService={teamService}
							projectService={projectService}
							onClose={handleCloseDialog}
						/>
					)
				);
			default:
				return null;
		}
	};

	return (
		<Dialog
			open={open}
			onClose={handleCloseDialog}
			aria-labelledby={importConfigurationDialogTitleId}
			data-testid="import-configuration-dialog"
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id={importConfigurationDialogTitleId}>
				Import Configuration
			</DialogTitle>
			<DialogContent>
				<Box sx={{ width: "100%", mt: 2, mb: 4 }}>
					<Stepper activeStep={activeStep}>
						{steps.map((label) => (
							<Step key={label}>
								<StepLabel>{label}</StepLabel>
							</Step>
						))}
					</Stepper>
				</Box>

				{renderStepContent()}
			</DialogContent>
		</Dialog>
	);
};

export default ImportConfigurationDialog;
