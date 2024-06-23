import { getProjectOverview } from "../../services/apiService";
import FilterBar from "./FilterBar";
import ProjectOverviewTable from "./ProjectOverviewTable";

export default function OverviewDashboard() {    
    const projects = getProjectOverview();

    return (
    <div>
        <FilterBar />
        <ProjectOverviewTable projects={projects} />
    </div>
    );
}