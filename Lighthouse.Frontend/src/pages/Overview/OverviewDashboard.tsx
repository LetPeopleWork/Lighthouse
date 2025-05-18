import { Container } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import FilterBar from "../../components/Common/FilterBar/FilterBar";
import LoadingAnimation from "../../components/Common/LoadingAnimation/LoadingAnimation";
import type { Project } from "../../models/Project/Project";
import { ApiServiceContext } from "../../services/Api/ApiServiceContext";
import ProjectOverview from "./ProjectOverview";

const OverviewDashboard: React.FC = () => {
	const [projects, setProjects] = useState<Project[]>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [hasError, setHasError] = useState(false);

	const location = useLocation();
	const navigate = useNavigate();
	const queryParams = new URLSearchParams(location.search);
	const initialFilterText = queryParams.get("filter") ?? "";
	const [filterText, setFilterText] = useState(initialFilterText);

	const { projectService } = useContext(ApiServiceContext);

	useEffect(() => {
		const fetchData = async () => {
			try {
				const projectData = await projectService.getProjects();
				setProjects(projectData);
				setIsLoading(false);
			} catch (error) {
				console.error("Error fetching project overview data:", error);
				setHasError(true);
			}
		};

		fetchData();
	}, [projectService]);

	// Update URL when filter changes
	const handleFilterChange = (newFilterText: string) => {
		setFilterText(newFilterText);
		const params = new URLSearchParams(location.search);

		if (newFilterText) {
			params.set("filter", newFilterText);
		} else {
			params.delete("filter");
		}

		navigate(
			{
				pathname: location.pathname,
				search: params.toString(),
			},
			{ replace: true },
		);
	};

	return (
		<LoadingAnimation isLoading={isLoading} hasError={hasError}>
			<Container maxWidth={false}>
				<FilterBar
					filterText={filterText}
					onFilterTextChange={handleFilterChange}
				/>
				<ProjectOverview projects={projects} filterText={filterText} />
			</Container>
		</LoadingAnimation>
	);
};

export default OverviewDashboard;
