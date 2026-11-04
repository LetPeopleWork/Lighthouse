import type { IBoardInformation } from "../Boards/BoardInformation";
import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";

export type SettingsContext = "team" | "portfolio";

export interface IDataRetrievalWizard {
	id: string;

	name: string;

	applicableSystemTypes: WorkTrackingSystemType[];

	applicableSettingsContexts: SettingsContext[];

	component: React.ComponentType<DataRetrievalWizardProps>;

	/** Sign-ins this wizard cannot serve, with the reason shown on its disabled button. */
	unservedSignIns?: {
		authenticationMethodKeys: string[];
		reason: string;
	};
}

export function isWizardUsable(
	wizard: IDataRetrievalWizard,
	authenticationMethodKey: string | undefined,
): boolean {
	return !(
		authenticationMethodKey !== undefined &&
		wizard.unservedSignIns?.authenticationMethodKeys.includes(
			authenticationMethodKey,
		)
	);
}

export interface DataRetrievalWizardProps {
	open: boolean;

	workTrackingSystemConnectionId: number;

	onComplete: (value: IBoardInformation) => void;

	onCancel: () => void;

	boardType?: "Board" | "Team";
}
