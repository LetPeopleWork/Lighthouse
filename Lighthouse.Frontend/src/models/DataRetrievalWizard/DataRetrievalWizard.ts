import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";

export interface IDataRetrievalWizard {
	id: string;

	name: string;

	applicableSystemTypes: WorkTrackingSystemType[];

	component: React.ComponentType<DataRetrievalWizardProps>;
}

export interface DataRetrievalWizardProps {
	open: boolean;

	workTrackingSystemConnectionId: number;

	onComplete: (value: string) => void;

	onCancel: () => void;
}
