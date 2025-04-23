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
import { useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";

export interface FeatureListBaseProps {
	features: IFeature[];
	renderTableHeader: () => React.ReactNode;
	renderTableRow: (feature: IFeature) => React.ReactNode;
	contextId: number;
	contextType: "project" | "team";
}

const FeatureListBase: React.FC<FeatureListBaseProps> = ({
	features,
	renderTableHeader,
	renderTableRow,
	contextId,
	contextType,
}) => {
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(false);

	const baseKey = "lighthouse_hide_completed_features";
	const storageKey = `${baseKey}_${contextType}_${contextId}`;

	useEffect(() => {
		const storedPreference = localStorage.getItem(storageKey);
		if (storedPreference !== null) {
			setHideCompletedFeatures(storedPreference === "true");
		}
	}, [storageKey]);

	const handleToggleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const newValue = event.target.checked;
		setHideCompletedFeatures(newValue);
		localStorage.setItem(storageKey, newValue.toString());
	};

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
