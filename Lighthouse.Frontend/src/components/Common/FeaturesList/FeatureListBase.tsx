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
	const [groupFeaturesByParent, setGroupFeaturesByParent] =
		useState<boolean>(false);

	const baseKey = "lighthouse_hide_completed_features";
	const storageKey = `${baseKey}_${contextType}_${contextId}`;
	const groupingBaseKey = "lighthouse_group_features_by_parent";
	const groupingStorageKey = `${groupingBaseKey}_${contextType}_${contextId}`;

	useEffect(() => {
		const storedPreference = localStorage.getItem(storageKey);
		if (storedPreference !== null) {
			setHideCompletedFeatures(storedPreference === "true");
		}

		const storedGroupingPreference = localStorage.getItem(groupingStorageKey);
		if (storedGroupingPreference !== null) {
			setGroupFeaturesByParent(storedGroupingPreference === "true");
		}
	}, [storageKey, groupingStorageKey]);

	const handleToggleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const newValue = event.target.checked;
		setHideCompletedFeatures(newValue);
		localStorage.setItem(storageKey, newValue.toString());
	};

	const handleGroupingToggleChange = (
		event: React.ChangeEvent<HTMLInputElement>,
	) => {
		const newValue = event.target.checked;
		setGroupFeaturesByParent(newValue);
		localStorage.setItem(groupingStorageKey, newValue.toString());
	};

	const displayedFeatures = hideCompletedFeatures
		? features.filter((feature) => feature.stateCategory !== "Done")
		: features;

	return (
		<TableContainer component={Paper}>
			<Box sx={{ display: "flex", justifyContent: "flex-end", p: 2, gap: 2 }}>
				<FormControlLabel
					control={
						<Switch
							checked={groupFeaturesByParent}
							onChange={handleGroupingToggleChange}
							color="primary"
							data-testid="group-features-by-parent-toggle"
						/>
					}
					label="Group Features by Parent"
				/>
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
