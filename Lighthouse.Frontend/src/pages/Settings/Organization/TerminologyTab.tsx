import Box from "@mui/material/Box";
import type React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import { TerminologyConfiguration } from "../../../components/TerminologyConfiguration";

const TerminologyTab: React.FC = () => (
	<Box sx={{ mb: 4 }}>
		<InputGroup title="Terminology Configuration" initiallyExpanded={true}>
			<TerminologyConfiguration />
		</InputGroup>
	</Box>
);

export default TerminologyTab;
