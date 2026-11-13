import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import Typography from "@mui/material/Typography";
import type React from "react";
import { USAGE_DATA_DOCS_URL } from "../../../models/UsageData/UsageData";
import BehaviourSettingsSection from "./BehaviourSettingsSection";

const UsageDataTab: React.FC = () => (
	<Box sx={{ mb: 4 }}>
		<BehaviourSettingsSection title="Usage Data" settingKey="UsageData">
			<Typography variant="body2" sx={{ mt: 2 }}>
				<Link href={USAGE_DATA_DOCS_URL} target="_blank" rel="noopener">
					What Lighthouse sends
				</Link>
			</Typography>
		</BehaviourSettingsSection>
	</Box>
);

export default UsageDataTab;
