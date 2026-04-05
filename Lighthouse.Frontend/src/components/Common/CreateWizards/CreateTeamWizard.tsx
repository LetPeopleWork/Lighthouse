import type React from "react";
import { useCreateWizard } from "../../../hooks/useCreateWizard";
import { getDefaultTeamSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import CreateWizardShell from "./CreateWizardShell";

interface CreateTeamWizardProps {
	getConnections: () => Promise<IWorkTrackingSystemConnection[]>;
	validateTeamSettings: (settings: ITeamSettings) => Promise<boolean>;
	saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
	onCancel: () => void;
}

const CreateTeamWizard: React.FC<CreateTeamWizardProps> = ({
	getConnections,
	validateTeamSettings,
	saveTeamSettings,
	onCancel,
}) => {
	const wizard = useCreateWizard<ITeamSettings>({
		entityType: "team",
		defaultName: "New Team",
		getConnections,
		getSchema: (wts) => getDefaultTeamSchema(wts),
		buildDto: (base, name) => ({
			id: 0,
			name,
			workTrackingSystemConnectionId: base.workTrackingSystemConnectionId,
			dataRetrievalValue: base.dataRetrievalValue,
			workItemTypes: base.workItemTypes,
			toDoStates: base.toDoStates,
			doingStates: base.doingStates,
			doneStates: base.doneStates,
			throughputHistory: 90,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
			featureWIP: 0,
			automaticallyAdjustFeatureWIP: false,
			serviceLevelExpectationProbability: 0,
			serviceLevelExpectationRange: 0,
			systemWIPLimit: 0,
			parentOverrideAdditionalFieldDefinitionId: null,
			blockedStates: [],
			blockedTags: [],
			stateMappings: [],
			doneItemsCutoffDays: 365,
			processBehaviourChartBaselineStartDate: null,
			processBehaviourChartBaselineEndDate: null,
			estimationAdditionalFieldDefinitionId: null,
			estimationUnit: null,
			useNonNumericEstimation: false,
			estimationCategoryValues: [],
		}),
		validateSettings: validateTeamSettings,
		saveSettings: saveTeamSettings,
	});

	return (
		<CreateWizardShell
			{...wizard}
			entityLabel="team"
			nameLabel="Team Name"
			isForTeam={true}
			onSelectConnection={wizard.selectConnection}
			onSetActiveWizard={wizard.setActiveWizard}
			onWizardComplete={wizard.handleWizardComplete}
			onWizardCancel={wizard.handleWizardCancel}
			onSetActiveStep={wizard.setActiveStep}
			selectedConnectionId={wizard.selectedConnection?.id ?? null}
			dataRetrievalLabel={wizard.getDataRetrievalLabel()}
			onDataRetrievalChange={wizard.setDataRetrievalValue}
			showWorkItemTypes={wizard.schema?.isWorkItemTypesRequired !== false}
			onAddWorkItemType={(t) =>
				wizard.setWorkItemTypes((prev) => [...prev, t.trim()])
			}
			onRemoveWorkItemType={(t) =>
				wizard.setWorkItemTypes((prev) => prev.filter((x) => x !== t))
			}
			onAddToDoState={(s) => wizard.setToDoStates((prev) => [...prev, s])}
			onRemoveToDoState={(s) =>
				wizard.setToDoStates((prev) => prev.filter((x) => x !== s))
			}
			onReorderToDoStates={wizard.setToDoStates}
			onAddDoingState={(s) => wizard.setDoingStates((prev) => [...prev, s])}
			onRemoveDoingState={(s) =>
				wizard.setDoingStates((prev) => prev.filter((x) => x !== s))
			}
			onReorderDoingStates={wizard.setDoingStates}
			onAddDoneState={(s) => wizard.setDoneStates((prev) => [...prev, s])}
			onRemoveDoneState={(s) =>
				wizard.setDoneStates((prev) => prev.filter((x) => x !== s))
			}
			onReorderDoneStates={wizard.setDoneStates}
			onCancel={onCancel}
			onBack={wizard.handleBack}
			onNext={wizard.handleNext}
			onCreate={wizard.handleCreate}
			onNameChange={wizard.setName}
		/>
	);
};

export default CreateTeamWizard;
