import { Box, Button, TextField, Typography } from "@mui/material";
import type React from "react";
import {
	STEP_CHOOSE_CONNECTION,
	STEP_CONFIGURE,
	STEP_LOAD_DATA,
	STEP_NAME_CREATE,
	STEPS,
} from "../../../hooks/useCreateWizard";
import type { LicenseCheckStatus } from "../../../hooks/useLicenseRestrictions";
import type { IBoardInformation } from "../../../models/Boards/BoardInformation";
import type { IDataRetrievalWizard } from "../../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import StatesList from "../StatesList/StatesList";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WizardStepperFrame from "./WizardStepperFrame";

export interface CreateWizardShellProps {
	activeStep: number;
	loading: boolean;
	connections: IWorkTrackingSystemConnection[];
	onSelectConnection: (c: IWorkTrackingSystemConnection) => void;
	entityLabel: string; // "team" | "portfolio"
	// load data step
	availableWizards: IDataRetrievalWizard[];
	activeWizard: IDataRetrievalWizard | null;
	onSetActiveWizard: (w: IDataRetrievalWizard | null) => void;
	onWizardComplete: (info: IBoardInformation) => void;
	onWizardCancel: () => void;
	onSetActiveStep: (step: number) => void;
	selectedConnectionId: number | null;
	// configure step
	showDataRetrievalField: boolean;
	dataRetrievalLabel: string;
	dataRetrievalPlaceholder?: string;
	dataRetrievalHelpText?: string;
	dataRetrievalValue: string;
	onDataRetrievalChange: (v: string) => void;
	showWorkItemTypes: boolean;
	workItemTypes: string[];
	onAddWorkItemType: (t: string) => void;
	onRemoveWorkItemType: (t: string) => void;
	isForTeam: boolean;
	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];
	onAddToDoState: (s: string) => void;
	onRemoveToDoState: (s: string) => void;
	onReorderToDoStates: (s: string[]) => void;
	onAddDoingState: (s: string) => void;
	onRemoveDoingState: (s: string) => void;
	onReorderDoingStates: (s: string[]) => void;
	onAddDoneState: (s: string) => void;
	onRemoveDoneState: (s: string) => void;
	onReorderDoneStates: (s: string[]) => void;
	validationError: string | null;
	validationTechnicalDetails?: string | null;
	configInputsValid: boolean;
	validating: boolean;
	// name & create step
	nameLabel: string;
	name: string;
	onNameChange: (v: string) => void;
	saving: boolean;
	// nav
	onCancel: () => void;
	onBack: () => void;
	onNext: () => void;
	onCreate: () => void;
	/** What the license check allows for the thing being created; leave out when creating it is not limited. */
	createLicenseGate?: {
		status: LicenseCheckStatus;
		allowed: boolean;
		premiumExtraInfo?: string;
	};
}

const CreateWizardShell: React.FC<CreateWizardShellProps> = ({
	activeStep,
	loading,
	connections,
	onSelectConnection,
	entityLabel,
	availableWizards,
	activeWizard,
	onSetActiveWizard,
	onWizardComplete,
	onWizardCancel,
	onSetActiveStep,
	selectedConnectionId,
	showDataRetrievalField,
	dataRetrievalLabel,
	dataRetrievalPlaceholder,
	dataRetrievalHelpText,
	dataRetrievalValue,
	onDataRetrievalChange,
	showWorkItemTypes,
	workItemTypes,
	onAddWorkItemType,
	onRemoveWorkItemType,
	isForTeam,
	toDoStates,
	doingStates,
	doneStates,
	onAddToDoState,
	onRemoveToDoState,
	onReorderToDoStates,
	onAddDoingState,
	onRemoveDoingState,
	onReorderDoingStates,
	onAddDoneState,
	onRemoveDoneState,
	onReorderDoneStates,
	validationError,
	validationTechnicalDetails,
	configInputsValid,
	validating,
	nameLabel,
	name,
	onNameChange,
	saving,
	onCancel,
	onBack,
	onNext,
	onCreate,
	createLicenseGate,
}) => {
	const renderWizardButtons = () =>
		availableWizards.map((wizard) => (
			<Button
				key={wizard.id}
				variant="contained"
				onClick={() => onSetActiveWizard(wizard)}
				sx={{ mr: 1 }}
			>
				{wizard.name}
			</Button>
		));

	const renderActiveWizardDialog = () =>
		activeWizard &&
		selectedConnectionId != null && (
			<activeWizard.component
				open={true}
				workTrackingSystemConnectionId={selectedConnectionId}
				onComplete={onWizardComplete}
				onCancel={onWizardCancel}
				boardType={activeWizard.id === "linear.team" ? "Team" : "Board"}
			/>
		);

	const configureError =
		activeStep === STEP_CONFIGURE && validationError
			? { message: validationError, details: validationTechnicalDetails }
			: null;

	return (
		<WizardStepperFrame
			steps={STEPS}
			activeStep={activeStep}
			busy={loading}
			onCancel={onCancel}
			onBack={activeStep > STEP_CHOOSE_CONNECTION ? onBack : undefined}
			next={
				activeStep === STEP_CONFIGURE
					? {
							label: "Next",
							onClick: onNext,
							disabled: !configInputsValid || validating,
						}
					: undefined
			}
			primaryAction={
				activeStep === STEP_NAME_CREATE
					? {
							label: "Create",
							onClick: onCreate,
							disabled:
								name.trim() === "" ||
								saving ||
								(createLicenseGate !== undefined && !createLicenseGate.allowed),
							licenseGate: createLicenseGate,
						}
					: undefined
			}
			error={configureError}
		>
			{activeStep === STEP_CHOOSE_CONNECTION && (
				<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
					<Typography variant="body1">
						Select the connection to use for this {entityLabel}:
					</Typography>
					{connections.map((connection) => (
						<Button
							key={connection.id}
							variant="outlined"
							onClick={() => onSelectConnection(connection)}
							sx={{ justifyContent: "flex-start", textTransform: "none" }}
						>
							{connection.name}
						</Button>
					))}
				</Box>
			)}

			{activeStep === STEP_LOAD_DATA && (
				<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
					<Typography variant="body1">
						Use a wizard to quickly load your {entityLabel} data, or configure
						manually:
					</Typography>
					<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
						{renderWizardButtons()}
					</Box>
					<Button
						variant="outlined"
						onClick={() => onSetActiveStep(STEP_CONFIGURE)}
						sx={{ alignSelf: "flex-start" }}
					>
						Configure Manually
					</Button>
					{renderActiveWizardDialog()}
				</Box>
			)}

			{activeStep === STEP_CONFIGURE && (
				<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
					{availableWizards.length > 0 && (
						<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap", mt: 1 }}>
							{renderWizardButtons()}
						</Box>
					)}
					{showDataRetrievalField && (
						<TextField
							label={dataRetrievalLabel}
							placeholder={dataRetrievalPlaceholder}
							helperText={dataRetrievalHelpText}
							multiline
							rows={4}
							fullWidth
							value={dataRetrievalValue}
							onChange={(e) => onDataRetrievalChange(e.target.value)}
						/>
					)}
					{showWorkItemTypes && (
						<WorkItemTypesComponent
							workItemTypes={workItemTypes}
							onAddWorkItemType={onAddWorkItemType}
							onRemoveWorkItemType={onRemoveWorkItemType}
							isForTeam={isForTeam}
						/>
					)}
					<StatesList
						toDoStates={toDoStates}
						onAddToDoState={onAddToDoState}
						onRemoveToDoState={onRemoveToDoState}
						onReorderToDoStates={onReorderToDoStates}
						doingStates={doingStates}
						onAddDoingState={onAddDoingState}
						onRemoveDoingState={onRemoveDoingState}
						onReorderDoingStates={onReorderDoingStates}
						doneStates={doneStates}
						onAddDoneState={onAddDoneState}
						onRemoveDoneState={onRemoveDoneState}
						onReorderDoneStates={onReorderDoneStates}
						isForTeam={isForTeam}
					/>
					{renderActiveWizardDialog()}
				</Box>
			)}

			{activeStep === STEP_NAME_CREATE && (
				<Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
					<TextField
						label={nameLabel}
						fullWidth
						value={name}
						onChange={(e) => onNameChange(e.target.value)}
					/>
				</Box>
			)}
		</WizardStepperFrame>
	);
};

export default CreateWizardShell;
