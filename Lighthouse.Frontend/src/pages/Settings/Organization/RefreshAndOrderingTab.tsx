import Box from "@mui/material/Box";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import RefreshSettingUpdater from "../Refresh/RefreshSettingUpdater";
import BehaviourSettingsSection from "../System/BehaviourSettingsSection";

const RefreshAndOrderingTab: React.FC = () => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	return (
		<Box sx={{ mb: 4 }}>
			<BehaviourSettingsSection
				title="Behaviour Settings"
				settingKey="FeatureOrdering"
			/>
			<InputGroup title={`${teamTerm} Refresh`}>
				<RefreshSettingUpdater title={teamTerm} settingName="Team" />
			</InputGroup>
			<InputGroup title={`${featureTerm} Refresh`}>
				<RefreshSettingUpdater title={featureTerm} settingName="Feature" />
			</InputGroup>
		</Box>
	);
};

export default RefreshAndOrderingTab;
