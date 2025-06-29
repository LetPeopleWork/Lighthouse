import {
	Box,
	FormControlLabel,
	Link,
	Paper,
	Switch,
	Table,
	TableBody,
	TableContainer,
	TableHead,
	useTheme,
} from "@mui/material";
import type React from "react";
import { Fragment, useContext, useEffect, useState } from "react";
import type { Feature, IFeature } from "../../../models/Feature";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
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
	const { featureService } = useContext(ApiServiceContext);
	const [hideCompletedFeatures, setHideCompletedFeatures] =
		useState<boolean>(false);
	const [groupFeaturesByParent, setGroupFeaturesByParent] =
		useState<boolean>(false);
	const [parentFeatures, setParentFeatures] = useState<Record<string, Feature>>(
		{},
	);
	const [isLoadingParents, setIsLoadingParents] = useState<boolean>(false);

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

	// Helper function to render parent header
	const renderParentHeader = (parentId: string): React.ReactNode => {
		if (parentId === "none") {
			return "No Parent";
		}

		if (isLoadingParents) {
			return `Loading parent feature ${parentId}...`;
		}

		const parentFeature = parentFeatures[parentId];
		if (parentFeature) {
			return (
				<Link
					href={parentFeature.url || "#"}
					target="_blank"
					rel="noopener noreferrer"
					color="inherit"
					underline="hover"
					data-testid={`parent-feature-link-${parentId}`}
					sx={{ display: "flex", alignItems: "center" }}
				>
					{`${parentFeature.referenceId}: ${parentFeature.name}`}
				</Link>
			);
		}

		return `Parent ID: ${parentId}`;
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
		let sortedKeys = Object.keys(groups);

		// Sort keys based on parent features order (from API) and put "none" at the bottom
		sortedKeys = sortedKeys.sort((a, b) => {
			// Place "none" group at the bottom
			if (a === "none") return 1;
			if (b === "none") return -1;

			// Use order from API if we have both parent features
			if (parentFeatures[a] && parentFeatures[b]) {
				// By default, keep the order from the API
				return 0;
			}

			// If we don't have both parents, fall back to alphanumeric sort
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
								{renderParentHeader(parentId)}
							</td>
						</tr>
						{groups[parentId].map((feature) => renderTableRow(feature))}
					</Fragment>
				))}
			</>
		);
	};

	// Fetch parent features when needed
	useEffect(() => {
		if (groupFeaturesByParent) {
			// Extract unique parent IDs that are not "none"
			const uniqueParentIds = Array.from(
				new Set(
					features
						.map((feature) => feature.parentWorkItemReference)
						.filter((id): id is string => !!id),
				),
			);

			if (uniqueParentIds.length > 0) {
				const fetchParentFeatures = async () => {
					setIsLoadingParents(true);
					try {
						const parentFeaturesList =
							await featureService.getParentFeatures(uniqueParentIds);
						// Convert array to a record object with referenceId as the key
						const parentsMap = parentFeaturesList.reduce<
							Record<string, Feature>
						>((acc, feature) => {
							acc[feature.referenceId] = feature;
							return acc;
						}, {});
						setParentFeatures(parentsMap);
					} catch (error) {
						console.error("Error fetching parent features:", error);
					} finally {
						setIsLoadingParents(false);
					}
				};

				fetchParentFeatures();
			}
		}
	}, [features, groupFeaturesByParent, featureService]);

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
