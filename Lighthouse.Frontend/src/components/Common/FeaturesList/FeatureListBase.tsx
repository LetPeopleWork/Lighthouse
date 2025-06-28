import {
	Box,
	FormControlLabel,
	Paper,
	Switch,
	Table,
	TableBody,
	TableContainer,
	TableHead,
	useTheme,
} from "@mui/material";
import type React from "react";
import { Fragment, useEffect, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import { appColors } from "../../../utils/theme/colors";

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
	const theme = useTheme();
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

	// Filter features based on the "hide completed" setting
	const filteredFeatures = hideCompletedFeatures
		? features.filter((feature) => feature.stateCategory !== "Done")
		: features;

	// Group features by parent work item
	const groupFeatures = (featuresToGroup: IFeature[]) => {
		const groups: Record<string, IFeature[]> = {};

		// Group with parent
		featuresToGroup.forEach((feature) => {
			const parentId = feature.parentWorkItemReference || "none";
			if (!groups[parentId]) {
				groups[parentId] = [];
			}
			groups[parentId].push(feature);
		});

		return groups;
	};

	// Get header background color based on theme mode
	const headerBgColor =
		theme.palette.mode === "dark"
			? appColors.dark.paper
			: appColors.light.background;

	// Get text color based on theme mode
	const headerTextColor =
		theme.palette.mode === "dark"
			? appColors.dark.text.primary
			: appColors.light.text.primary;

	// Determine if we should display grouped or flat list of features
	const displayFeatures = () => {
		if (!groupFeaturesByParent) {
			// Return flat list
			return <>{filteredFeatures.map((feature) => renderTableRow(feature))}</>;
		}

		// Group features by parent
		const groups = groupFeatures(filteredFeatures);
		const sortedKeys = Object.keys(groups).sort((a, b) => {
			// Place "none" group at the bottom
			if (a === "none") return 1;
			if (b === "none") return -1;
			return a.localeCompare(b);
		});

		return (
			<>
				{sortedKeys.map((parentId) => (
					<Fragment key={parentId}>
						<tr>
							<td
								colSpan={100}
								style={{
									backgroundColor: headerBgColor,
									padding: "8px 16px",
									fontWeight: "bold",
									color: headerTextColor,
									borderBottom: `1px solid ${theme.palette.divider}`,
								}}
							>
								{parentId === "none" ? "No Parent" : `Parent ID: ${parentId}`}
							</td>
						</tr>
						{groups[parentId].map((feature) => renderTableRow(feature))}
					</Fragment>
				))}
			</>
		);
	};

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
				<TableBody>{displayFeatures()}</TableBody>
			</Table>
		</TableContainer>
	);
};

export default FeatureListBase;
