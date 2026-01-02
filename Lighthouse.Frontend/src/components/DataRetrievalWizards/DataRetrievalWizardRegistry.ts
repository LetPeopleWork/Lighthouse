import type { IDataRetrievalWizard } from "../../models/DataRetrievalWizard/DataRetrievalWizard";
import type { WorkTrackingSystemType } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import CsvUploadWizard from "./CsvUploadWizard";

const dataRetrievalWizards: IDataRetrievalWizard[] = [
	{
		id: "csv.upload",
		name: "Upload CSV File",
		applicableSystemTypes: ["Csv"],
		component: CsvUploadWizard,
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
