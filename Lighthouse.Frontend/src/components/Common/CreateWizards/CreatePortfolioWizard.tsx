import type React from "react";
import { useCreateWizard } from "../../../hooks/useCreateWizard";
import { getDefaultPortfolioSchema } from "../../../models/Common/DataRetrievalSchemaDefaults";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import CreateWizardShell from "./CreateWizardShell";

interface CreatePortfolioWizardProps {
	getConnections: () => Promise<IWorkTrackingSystemConnection[]>;
	validatePortfolioSettings: (settings: IPortfolioSettings) => Promise<boolean>;
	savePortfolioSettings: (settings: IPortfolioSettings) => Promise<void>;
	onCancel: () => void;
}

const CreatePortfolioWizard: React.FC<CreatePortfolioWizardProps> = ({
	getConnections,
	validatePortfolioSettings,
	savePortfolioSettings,
	onCancel,
}) => {
	const wizard = useCreateWizard<IPortfolioSettings>({
		entityType: "portfolio",
		defaultName: "New Portfolio",
		getConnections,
		getSchema: (wts) => getDefaultPortfolioSchema(wts),
		buildDto: (base, name) => ({
			id: 0,
			name,
			workTrackingSystemConnectionId: base.workTrackingSystemConnectionId,
			dataRetrievalValue: base.dataRetrievalValue,
			workItemTypes: base.workItemTypes,
			toDoStates: base.toDoStates,
			doingStates: base.doingStates,
			doneStates: base.doneStates,
			overrideRealChildCountStates: [],
			usePercentileToCalculateDefaultAmountOfWorkItems: false,
			defaultAmountOfWorkItemsPerFeature: 10,
			defaultWorkItemPercentile: 0,
			percentileHistoryInDays: 0,
			sizeEstimateAdditionalFieldDefinitionId: null,
			featureOwnerAdditionalFieldDefinitionId: null,
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
		validateSettings: validatePortfolioSettings,
		saveSettings: savePortfolioSettings,
	});

	return (
		<CreateWizardShell
			{...wizard}
			entityLabel="portfolio"
			nameLabel="Portfolio Name"
			isForTeam={false}
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

export default CreatePortfolioWizard;
