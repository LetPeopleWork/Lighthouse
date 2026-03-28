import type { IBoardInformation } from "../Boards/BoardInformation";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";

export type SettingsContext = "team" | "portfolio";

export interface IDataRetrievalWizard {
	id: string;

	name: string;

	applicableSystemTypes: WorkTrackingSystemType[];

	applicableSettingsContexts: SettingsContext[];

	component: React.ComponentType<DataRetrievalWizardProps>;
}

export interface DataRetrievalWizardProps {
	open: boolean;

	workTrackingSystemConnectionId: number;

	onComplete: (value: IBoardInformation) => void;

	onCancel: () => void;

	dialogTitle?: string;
}
