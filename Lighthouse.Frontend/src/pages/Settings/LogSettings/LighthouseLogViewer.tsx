import { LazyLog } from "@melloware/react-logviewer";
import type React from "react";

interface LogViewerProps {
	data: string;
}

const LighthouseLogViewer: React.FC<LogViewerProps> = ({ data }) => {
	return (
		<LazyLog
			caseInsensitive
			enableHotKeys
			enableSearch
			height={"800"}
			extraLines={1}
			selectableLines
			text={data}
		/>
	);
};

export default LighthouseLogViewer;
