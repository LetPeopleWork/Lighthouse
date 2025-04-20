import {
	Box,
	FormControlLabel,
	Paper,
	Switch,
	Table,
	TableBody,
	TableContainer,
	TableHead,
} from "@mui/material";
import type React from "react";
import { useState } from "react";
import type { IFeature } from "../../../models/Feature";

export interface FeatureListBaseProps {
	features: IFeature[];
	renderTableHeader: () => React.ReactNode;
	renderTableRow: (feature: IFeature) => React.ReactNode;
}

/**
 * Base component for feature lists with shared functionality
 * like filtering completed features
 */
const FeatureListBase: React.FC<FeatureListBaseProps> = ({
	features,
	renderTableHeader,
	renderTableRow,
}) => {
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(false);

	const handleToggleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		setHideCompletedFeatures(event.target.checked);
	};

	// Filter features based on hideCompletedFeatures state
	const displayedFeatures = hideCompletedFeatures
		? features.filter((feature) => feature.stateCategory !== "Done")
		: features;

	return (
		<TableContainer component={Paper}>
			<Box sx={{ display: "flex", justifyContent: "flex-end", p: 2 }}>
				<FormControlLabel
					control={
						<Switch
							checked={hideCompletedFeatures}
							onChange={handleToggleChange}
							color="primary"
							data-testid="hide-completed-features-toggle"
						/>
					}
					label="Hide Completed Features"
				/>
			</Box>
			<Table>
				<TableHead>{renderTableHeader()}</TableHead>
				<TableBody>
					{displayedFeatures.map((feature) => renderTableRow(feature))}
				</TableBody>
			</Table>
		</TableContainer>
	);
};

export default FeatureListBase;
