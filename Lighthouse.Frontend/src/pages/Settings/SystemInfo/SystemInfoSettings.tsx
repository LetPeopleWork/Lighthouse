import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import LogSettings from "../LogSettings/LogSettings";
import SystemInfoDisplay from "./SystemInfoDisplay";

const SystemInfoSettings: React.FC = () => {
	return (
		<>
			<InputGroup title="System Info">
				<SystemInfoDisplay />
			</InputGroup>
			<LogSettings />
		</>
	);
};

export default SystemInfoSettings;
