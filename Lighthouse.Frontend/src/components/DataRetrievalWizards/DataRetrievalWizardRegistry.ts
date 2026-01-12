import type { IDataRetrievalWizard } from "../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { WorkTrackingSystemType } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import CsvUploadWizard from "./CsvUploadWizard";
import JiraBoardWizard from "./JiraBoardWizard";

const dataRetrievalWizards: IDataRetrievalWizard[] = [
	{
		id: "csv.upload",
		name: "Upload CSV File",
		applicableSystemTypes: ["Csv"],
		component: CsvUploadWizard,
	},
	{
		id: "jira.board",
		name: "Select Jira Board",
		applicableSystemTypes: ["Jira"],
		component: JiraBoardWizard,
	},
];

export function getWizardsForSystem(
	systemType: WorkTrackingSystemType,
): IDataRetrievalWizard[] {
	return dataRetrievalWizards.filter((wizard) => {
		if (!wizard.applicableSystemTypes.includes(systemType)) {
			return false;
		}

		return true;
	});
}

export default dataRetrievalWizards;
