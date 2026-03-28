import type {
	IDataRetrievalWizard,
	SettingsContext,
} from "../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { WorkTrackingSystemType } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import BoardWizard from "./BoardWizard";
import CsvUploadWizard from "./CsvUploadWizard";

const dataRetrievalWizards: IDataRetrievalWizard[] = [
	{
		id: "csv.upload",
		name: "Upload CSV File",
		applicableSystemTypes: ["Csv"],
		applicableSettingsContexts: ["team", "portfolio"],
		component: CsvUploadWizard,
	},
	{
		id: "jira.board",
		name: "Select Jira Board",
		applicableSystemTypes: ["Jira"],
		applicableSettingsContexts: ["team", "portfolio"],
		component: BoardWizard,
	},
	{
		id: "ado.board",
		name: "Select Azure DevOps Board",
		applicableSystemTypes: ["AzureDevOps"],
		applicableSettingsContexts: ["team", "portfolio"],
		component: BoardWizard,
	},
	{
		id: "linear.team",
		name: "Select Linear Team",
		applicableSystemTypes: ["Linear"],
		applicableSettingsContexts: ["team"],
		component: BoardWizard,
	},
];

export function getWizardsForSystem(
	systemType: WorkTrackingSystemType,
	settingsContext?: SettingsContext,
): IDataRetrievalWizard[] {
	return dataRetrievalWizards.filter((wizard) => {
		if (!wizard.applicableSystemTypes.includes(systemType)) {
			return false;
		}

		if (
			settingsContext &&
			!wizard.applicableSettingsContexts.includes(settingsContext)
		) {
			return false;
		}

		return true;
	});
}

export default dataRetrievalWizards;
