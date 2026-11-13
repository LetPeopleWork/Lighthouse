import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { useBehaviourSettings } from "../../../hooks/useBehaviourSettings";
import BehaviourSettingsTable from "./BehaviourSettingsTable";

interface BehaviourSettingsSectionProps {
	title: string;
	settingKey: string;
	children?: React.ReactNode;
}

/** One behaviour setting's row under a heading, with its own loading and failure look. */
const BehaviourSettingsSection: React.FC<BehaviourSettingsSectionProps> = ({
	title,
	settingKey,
	children,
}) => {
	const state = useBehaviourSettings([settingKey]);

	// With no row and nothing else to show there is no group, as the old page showed none for an empty list.
	const hasNoRow = state.status === "ready" && state.settings.length === 0;
	if (hasNoRow && !children) return null;

	return (
		<InputGroup title={title} initiallyExpanded={true}>
			{state.status === "loading" && (
				<Box sx={{ display: "flex", justifyContent: "center", p: 2 }}>
					<CircularProgress />
				</Box>
			)}
			{state.status === "failed" && (
				<Alert severity="error">Could not load the behaviour settings.</Alert>
			)}
			{state.status === "ready" && !hasNoRow && (
				<BehaviourSettingsTable
					settings={state.settings}
					canUsePremiumFeatures={state.canUsePremiumFeatures}
					onToggle={state.toggle}
				/>
			)}
			{children}
		</InputGroup>
	);
};

export default BehaviourSettingsSection;
