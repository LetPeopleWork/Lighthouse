import { TextField } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IPortfolioSettings } from "../../../../models/Project/PortfolioSettings";
import { TERMINOLOGY_KEYS } from "../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../services/TerminologyContext";
import InputGroup from "../../InputGroup/InputGroup";

interface UnparentedItemsComponentProps {
	projectSettings: IPortfolioSettings | null;
	onProjectSettingsChange: (
		key: keyof IPortfolioSettings,
		value: string | number | boolean | string[],
	) => void;
}

const UnparentedItemsComponent: React.FC<UnparentedItemsComponentProps> = ({
	projectSettings,
	onProjectSettingsChange,
}) => {
	const { getTerm } = useTerminology();
	const workItemsText = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const queryText = getTerm(TERMINOLOGY_KEYS.QUERY);

	return (
		<InputGroup title={`Unparented ${workItemsText}`} initiallyExpanded={false}>
			<Grid size={{ xs: 12 }}>
				<TextField
					label={`Unparented ${workItemsText} ${queryText}`}
					fullWidth
					multiline
					rows={4}
					margin="normal"
					value={projectSettings?.unparentedItemsQuery ?? ""}
					onChange={(e) =>
						onProjectSettingsChange("unparentedItemsQuery", e.target.value)
					}
				/>
			</Grid>
		</InputGroup>
	);
};

export default UnparentedItemsComponent;
